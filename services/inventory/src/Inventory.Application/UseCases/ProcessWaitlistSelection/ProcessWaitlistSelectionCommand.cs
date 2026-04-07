using Inventory.Domain.Entities;
using Inventory.Domain.Events;
using Inventory.Domain.Enums;
using Inventory.Domain.Ports;
using Inventory.Infrastructure.Configuration;
using Inventory.Infrastructure.Persistence;
using MediatR;

namespace Inventory.Application.UseCases.ProcessWaitlistSelection;

public record ProcessWaitlistSelectionCommand(
    Guid ReservationId,
    Guid SeatId,
    Guid EventId,
    string Section,
    DateTime ExpiredAt
) : IRequest<ProcessWaitlistSelectionResult?>;

public class ProcessWaitlistSelectionResult
{
    public Guid WaitlistEntryId { get; set; }
    public Guid UserId { get; set; }
    public Guid OpportunityWindowId { get; set; }
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
}

public class ProcessWaitlistSelectionHandler : IRequestHandler<ProcessWaitlistSelectionCommand, ProcessWaitlistSelectionResult?>
{
    private readonly InventoryDbContext _context;
    private readonly IWaitlistRepository _waitlistRepository;
    private readonly WaitlistRedisConfiguration? _redisConfiguration;
    private readonly IOpportunityWindowRepository _opportunityWindowRepository;
    private readonly IKafkaProducer _kafkaProducer;
    private readonly WaitlistSettings _waitlistSettings;

    public ProcessWaitlistSelectionHandler(
        InventoryDbContext context,
        IWaitlistRepository waitlistRepository,
        WaitlistRedisConfiguration? redisConfiguration,
        IOpportunityWindowRepository opportunityWindowRepository,
        IKafkaProducer kafkaProducer,
        WaitlistSettings waitlistSettings)
    {
        _context = context;
        _waitlistRepository = waitlistRepository;
        _redisConfiguration = redisConfiguration;
        _opportunityWindowRepository = opportunityWindowRepository;
        _kafkaProducer = kafkaProducer;
        _waitlistSettings = waitlistSettings;
    }

    public async Task<ProcessWaitlistSelectionResult?> Handle(ProcessWaitlistSelectionCommand request, CancellationToken cancellationToken)
    {
        var idempotencyKey = $"waitlist:processed:{request.ReservationId}:{request.ExpiredAt:o}";
        
        if (_redisConfiguration != null)
        {
            var alreadyProcessed = await _redisConfiguration.CheckIdempotencyAsync(idempotencyKey);
            if (alreadyProcessed)
            {
                return null;
            }

            var lockValue = $"{request.ReservationId}:{DateTime.UtcNow:Ticks}";
            var lockAcquired = await _redisConfiguration.AcquireLockAsync(
                request.EventId, request.Section, lockValue, TimeSpan.FromSeconds(10));
            
            if (!lockAcquired)
            {
                return null;
            }

            try
            {
                return await ProcessSelectionInternalAsync(request, idempotencyKey, cancellationToken);
            }
            finally
            {
                await _redisConfiguration.ReleaseLockAsync(request.EventId, request.Section, lockValue);
            }
        }

        return await ProcessSelectionInternalAsync(request, idempotencyKey, cancellationToken);
    }

    private async Task<ProcessWaitlistSelectionResult?> ProcessSelectionInternalAsync(
        ProcessWaitlistSelectionCommand request, 
        string idempotencyKey,
        CancellationToken cancellationToken)
    {
        var userId = await GetNextUserFromWaitlistAsync(request.EventId, request.Section, cancellationToken);
        if (userId == null)
        {
            return null;
        }

        var waitlistEntry = await _waitlistRepository.GetByUserEventSectionAsync(
            userId.Value, request.EventId, request.Section, cancellationToken);

        if (waitlistEntry == null)
        {
            return null;
        }

        waitlistEntry.Status = WaitlistStatus.OFFERED;
        waitlistEntry.NotifiedAt = DateTime.UtcNow;
        await _waitlistRepository.UpdateAsync(waitlistEntry, cancellationToken);

        var token = Guid.NewGuid().ToString("N");
        var expiresAt = DateTime.UtcNow.AddMinutes(_waitlistSettings.OpportunityTTLMinutes);

        var opportunityWindow = new OpportunityWindow
        {
            Id = Guid.NewGuid(),
            WaitlistEntryId = waitlistEntry.Id,
            SeatId = request.SeatId,
            Token = token,
            Status = OpportunityStatus.OFFERED,
            StartsAt = DateTime.UtcNow,
            ExpiresAt = expiresAt
        };

        await _opportunityWindowRepository.AddAsync(opportunityWindow, cancellationToken);

        if (_redisConfiguration != null)
        {
            await _redisConfiguration.SetIdempotencyAsync(idempotencyKey, TimeSpan.FromHours(24));
            await _redisConfiguration.RemoveFromQueueAsync(request.EventId, request.Section, userId.Value);
        }

        var waitlistEvent = new WaitlistOpportunityGrantedEvent
        {
            OpportunityId = opportunityWindow.Id,
            WaitlistEntryId = waitlistEntry.Id,
            UserId = waitlistEntry.UserId,
            EventId = request.EventId,
            SeatId = request.SeatId,
            Section = request.Section,
            OpportunityTTL = _waitlistSettings.OpportunityTTLMinutes * 60,
            IdempotencyKey = idempotencyKey,
            CreatedAt = DateTime.UtcNow
        };

        var json = System.Text.Json.JsonSerializer.Serialize(waitlistEvent);
        await _kafkaProducer.ProduceAsync("waitlist-opportunity", json, waitlistEntry.UserId.ToString());

        return new ProcessWaitlistSelectionResult
        {
            WaitlistEntryId = waitlistEntry.Id,
            UserId = waitlistEntry.UserId,
            OpportunityWindowId = opportunityWindow.Id,
            Token = token,
            ExpiresAt = expiresAt
        };
    }

    private async Task<Guid?> GetNextUserFromWaitlistAsync(Guid eventId, string section, CancellationToken ct)
    {
        if (_redisConfiguration != null)
        {
            var result = await _redisConfiguration.AtomicFifoSelectAsync(eventId, section);
            if (result.HasValue)
            {
                return result.Value.UserId;
            }
        }

        var activeEntries = await _waitlistRepository.GetActiveByEventAndSectionAsync(eventId, section, ct);
        return activeEntries.FirstOrDefault()?.UserId;
    }
}