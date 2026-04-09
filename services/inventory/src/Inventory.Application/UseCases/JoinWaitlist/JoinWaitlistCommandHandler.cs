using Inventory.Domain.Entities;
using Inventory.Domain.Enums;
using Inventory.Domain.Ports;
using Inventory.Infrastructure.Configuration;
using Inventory.Infrastructure.Persistence;
using MediatR;

namespace Inventory.Application.UseCases.JoinWaitlist;

public class JoinWaitlistCommandHandler : IRequestHandler<JoinWaitlistCommand, JoinWaitlistResponse>
{
    private readonly InventoryDbContext _context;
    private readonly IWaitlistRepository _waitlistRepository;
    private readonly WaitlistRedisConfiguration? _redisConfiguration;

    public JoinWaitlistCommandHandler(
        InventoryDbContext context,
        IWaitlistRepository waitlistRepository,
        WaitlistRedisConfiguration? redisConfiguration)
    {
        _context = context;
        _waitlistRepository = waitlistRepository;
        _redisConfiguration = redisConfiguration;
    }

    public async Task<JoinWaitlistResponse> Handle(JoinWaitlistCommand request, CancellationToken cancellationToken)
    {
        // Block only if the user already has an active or offered entry
        var exists = await _waitlistRepository.ExistsAsync(request.UserId, request.EventId, request.Section, cancellationToken);
        if (exists)
        {
            throw new InvalidOperationException("User is already in waitlist for this event and section");
        }

        // If a prior entry exists in a terminal state (EXPIRED/CANCELLED/CONSUMED),
        // reactivate it so we keep history and avoid duplicate rows.
        var existingEntry = await _waitlistRepository.GetByUserEventSectionAsync(
            request.UserId, request.EventId, request.Section, cancellationToken);

        WaitlistEntry entry;
        if (existingEntry != null && (existingEntry.Status == WaitlistStatus.EXPIRED ||
            existingEntry.Status == WaitlistStatus.CANCELLED))
        {
            existingEntry.Status = WaitlistStatus.ACTIVE;
            existingEntry.JoinedAt = DateTime.UtcNow;
            existingEntry.UpdatedAt = DateTime.UtcNow;
            existingEntry.NotifiedAt = null;
            existingEntry.CancelledAt = null;
            await _waitlistRepository.UpdateAsync(existingEntry, cancellationToken);
            entry = existingEntry;
        }
        else
        {
            entry = new WaitlistEntry
            {
                Id = Guid.NewGuid(),
                UserId = request.UserId,
                EventId = request.EventId,
                Section = request.Section,
                Status = WaitlistStatus.ACTIVE,
                JoinedAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            await _waitlistRepository.AddAsync(entry, cancellationToken);
        }

        if (_redisConfiguration != null)
        {
            await _redisConfiguration.AddToQueueAsync(request.EventId, request.Section, request.UserId, entry.JoinedAt);
        }

        var position = _redisConfiguration != null
            ? await _redisConfiguration.GetQueuePositionAsync(request.EventId, request.Section, request.UserId)
            : 1;

        return new JoinWaitlistResponse
        {
            WaitlistEntryId = entry.Id,
            UserId = entry.UserId,
            EventId = entry.EventId,
            Section = entry.Section,
            QueuePosition = (int)position,
            JoinedAt = entry.JoinedAt,
            Status = entry.Status.ToString()
        };
    }
}