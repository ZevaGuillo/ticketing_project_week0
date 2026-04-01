using Inventory.Domain.Enums;
using Inventory.Domain.Ports;
using Inventory.Infrastructure.Configuration;
using Inventory.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Inventory.Infrastructure.Workers;

public class OpportunityExpiryWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IKafkaProducer _producer;
    private readonly ILogger<OpportunityExpiryWorker> _logger;
    private readonly TimeSpan _pollInterval;

    public OpportunityExpiryWorker(
        IServiceScopeFactory scopeFactory,
        IKafkaProducer producer,
        ILogger<OpportunityExpiryWorker> logger)
        : this(scopeFactory, producer, logger, TimeSpan.FromMinutes(1))
    {
    }

    public OpportunityExpiryWorker(
        IServiceScopeFactory scopeFactory,
        IKafkaProducer producer,
        ILogger<OpportunityExpiryWorker> logger,
        TimeSpan pollInterval)
    {
        _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
        _producer = producer ?? throw new ArgumentNullException(nameof(producer));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _pollInterval = pollInterval;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessExpiredOpportunitiesAsync(stoppingToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing expired opportunities");
            }

            await Task.Delay(_pollInterval, stoppingToken).ConfigureAwait(false);
        }
    }

    public async Task ProcessExpiredOpportunitiesAsync(CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();
        var redisConfig = scope.ServiceProvider.GetService<WaitlistRedisConfiguration>();
        var waitlistRepo = scope.ServiceProvider.GetService<IWaitlistRepository>();
        var opportunityRepo = scope.ServiceProvider.GetService<IOpportunityWindowRepository>();
        var kafkaProducer = scope.ServiceProvider.GetService<IKafkaProducer>();

        var now = DateTime.UtcNow;
        var expiredOpportunities = await db.OpportunityWindows
            .Where(o => (o.Status == OpportunityStatus.OFFERED || o.Status == OpportunityStatus.IN_PROGRESS) 
                        && o.ExpiresAt <= now)
            .Include(o => o.WaitlistEntry)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        if (!expiredOpportunities.Any()) 
        {
            _logger.LogDebug("No expired opportunities found");
            return;
        }

        _logger.LogInformation("Found {Count} expired opportunities to process", expiredOpportunities.Count);

        foreach (var opportunity in expiredOpportunities)
        {
            try
            {
                await ProcessSingleExpiredOpportunityAsync(opportunity, db, redisConfig, waitlistRepo, opportunityRepo, kafkaProducer, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing expired opportunity {OpportunityId}", opportunity.Id);
            }
        }

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    private async Task ProcessSingleExpiredOpportunityAsync(
        Domain.Entities.OpportunityWindow opportunity,
        InventoryDbContext db,
        WaitlistRedisConfiguration? redisConfig,
        IWaitlistRepository? waitlistRepo,
        IOpportunityWindowRepository? opportunityRepo,
        IKafkaProducer? kafkaProducer,
        CancellationToken cancellationToken)
    {
        var waitlistEntry = opportunity.WaitlistEntry;
        if (waitlistEntry == null)
        {
            _logger.LogWarning("WaitlistEntry not found for opportunity {OpportunityId}", opportunity.Id);
            opportunity.Status = OpportunityStatus.EXPIRED;
            return;
        }

        _logger.LogInformation(
            "Opportunity {OpportunityId} expired for user {UserId}, triggering re-selection",
            opportunity.Id, waitlistEntry.UserId);

        opportunity.Status = OpportunityStatus.EXPIRED;
        
        waitlistEntry.Status = WaitlistStatus.ACTIVE;

        if (waitlistRepo != null)
        {
            await waitlistRepo.UpdateAsync(waitlistEntry, cancellationToken);
        }
        
        if (opportunityRepo != null)
        {
            await opportunityRepo.UpdateAsync(opportunity, cancellationToken);
        }
        
        if (redisConfig != null)
        {
            await redisConfig.AddToQueueAsync(
                waitlistEntry.EventId,
                waitlistEntry.Section,
                waitlistEntry.UserId,
                waitlistEntry.JoinedAt);
        }

        await TriggerReSelectionAsync(waitlistEntry, db, kafkaProducer, cancellationToken);

        await VerifyRedisConsistencyAsync(waitlistEntry, db, redisConfig, cancellationToken);
    }

    private async Task TriggerReSelectionAsync(
        Domain.Entities.WaitlistEntry waitlistEntry,
        InventoryDbContext db,
        IKafkaProducer? kafkaProducer,
        CancellationToken cancellationToken)
    {
        try
        {
            var availableSeat = await db.Seats
                .FirstOrDefaultAsync(s => !s.Reserved, cancellationToken);

            if (availableSeat == null)
            {
                _logger.LogInformation(
                    "No available seats for re-selection, skipping re-selection");
                return;
            }

            var selectedUser = await SelectNextUserFromQueueAsync(
                waitlistEntry.EventId, 
                waitlistEntry.Section, 
                db, 
                cancellationToken);

            if (selectedUser == null)
            {
                _logger.LogInformation("No users available for re-selection for event {EventId}", waitlistEntry.EventId);
                return;
            }

            var opportunityId = Guid.NewGuid();
            var token = Guid.NewGuid().ToString("N");
            var expiresAt = DateTime.UtcNow.AddMinutes(10);

            var opportunity = new Domain.Entities.OpportunityWindow
            {
                Id = opportunityId,
                WaitlistEntryId = selectedUser.Id,
                SeatId = availableSeat.Id,
                Token = token,
                Status = OpportunityStatus.OFFERED,
                StartsAt = DateTime.UtcNow,
                ExpiresAt = expiresAt
            };

            selectedUser.Status = WaitlistStatus.OFFERED;
            selectedUser.NotifiedAt = DateTime.UtcNow;

            db.OpportunityWindows.Add(opportunity);

            await db.SaveChangesAsync(cancellationToken);

            if (kafkaProducer != null)
            {
                var domainEvent = new
                {
                    OpportunityId = opportunityId,
                    WaitlistEntryId = selectedUser.Id,
                    UserId = selectedUser.UserId,
                    EventId = waitlistEntry.EventId,
                    SeatId = availableSeat.Id,
                    Section = waitlistEntry.Section,
                    OpportunityTTL = 600,
                    CreatedAt = DateTime.UtcNow
                };

                var json = System.Text.Json.JsonSerializer.Serialize(domainEvent);
                await kafkaProducer.ProduceAsync("waitlist.opportunity-granted", json, selectedUser.UserId.ToString());
            }

            _logger.LogInformation(
                "Re-selection successful: user {UserId} selected for opportunity {OpportunityId}",
                selectedUser.UserId, opportunityId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error triggering re-selection for event {EventId}", waitlistEntry.EventId);
        }
    }

    private async Task<Domain.Entities.WaitlistEntry?> SelectNextUserFromQueueAsync(
        Guid eventId, 
        string section, 
        InventoryDbContext db,
        CancellationToken cancellationToken)
    {
        var activeEntries = await db.WaitlistEntries
            .Where(w => w.EventId == eventId 
                       && w.Section == section 
                       && w.Status == WaitlistStatus.ACTIVE)
            .OrderBy(w => w.JoinedAt)
            .ToListAsync(cancellationToken);

        return activeEntries.FirstOrDefault();
    }

    private async Task VerifyRedisConsistencyAsync(
        Domain.Entities.WaitlistEntry waitlistEntry,
        InventoryDbContext db,
        WaitlistRedisConfiguration? redisConfig,
        CancellationToken cancellationToken)
    {
        if (redisConfig == null)
        {
            return;
        }

        try
        {
            var dbEntry = await db.WaitlistEntries
                .FirstOrDefaultAsync(w => w.Id == waitlistEntry.Id, cancellationToken);

            if (dbEntry == null)
            {
                _logger.LogWarning("WaitlistEntry {EntryId} not found in DB", waitlistEntry.Id);
                return;
            }

            var redisQueuePosition = await redisConfig.GetQueuePositionAsync(
                waitlistEntry.EventId, 
                waitlistEntry.Section, 
                waitlistEntry.UserId);

            if (dbEntry.Status == WaitlistStatus.ACTIVE && redisQueuePosition < 0)
            {
                await redisConfig.AddToQueueAsync(
                    waitlistEntry.EventId,
                    waitlistEntry.Section,
                    waitlistEntry.UserId,
                    waitlistEntry.JoinedAt);
                
                _logger.LogWarning(
                    "Redis-DB inconsistency detected: entry is ACTIVE in DB but not in Redis queue. Added to queue.");
            }
            else if (dbEntry.Status != WaitlistStatus.ACTIVE && redisQueuePosition > 0)
            {
                await redisConfig.RemoveFromQueueAsync(
                    waitlistEntry.EventId,
                    waitlistEntry.Section,
                    waitlistEntry.UserId);
                
                _logger.LogWarning(
                    "Redis-DB inconsistency detected: entry is {Status} in DB but in Redis queue. Removed from queue.",
                    dbEntry.Status);
            }
            else
            {
                _logger.LogDebug(
                    "Redis-DB consistency verified for entry {EntryId}: DB={DbStatus}, Redis position={RedisPosition}",
                    waitlistEntry.Id, dbEntry.Status, redisQueuePosition);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying Redis-DB consistency for entry {EntryId}", waitlistEntry.Id);
        }
    }
}
