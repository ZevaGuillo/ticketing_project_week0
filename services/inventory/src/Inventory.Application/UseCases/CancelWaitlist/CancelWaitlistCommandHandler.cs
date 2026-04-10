using Inventory.Domain.Enums;
using Inventory.Domain.Ports;
using Inventory.Infrastructure.Configuration;
using MediatR;

namespace Inventory.Application.UseCases.CancelWaitlist;

public class CancelWaitlistCommandHandler : IRequestHandler<CancelWaitlistCommand, CancelWaitlistResult>
{
    private readonly IWaitlistRepository _waitlistRepository;
    private readonly WaitlistRedisConfiguration? _redisConfiguration;

    public CancelWaitlistCommandHandler(
        IWaitlistRepository waitlistRepository,
        WaitlistRedisConfiguration? redisConfiguration)
    {
        _waitlistRepository = waitlistRepository;
        _redisConfiguration = redisConfiguration;
    }

    public async Task<CancelWaitlistResult> Handle(CancelWaitlistCommand request, CancellationToken cancellationToken)
    {
        var entry = await _waitlistRepository.GetByUserEventSectionAsync(
            request.UserId, request.EventId, request.Section, cancellationToken);

        if (entry == null)
        {
            throw new KeyNotFoundException("You are not in the waitlist for this event and section");
        }

        if (entry.Status == WaitlistStatus.CANCELLED)
        {
            throw new InvalidOperationException("This waitlist subscription is already cancelled");
        }

        if (entry.Status == WaitlistStatus.CONSUMED)
        {
            throw new InvalidOperationException("Cannot cancel: you have already purchased the ticket");
        }

        entry.Status = WaitlistStatus.CANCELLED;
        entry.UpdatedAt = DateTime.UtcNow;

        await _waitlistRepository.UpdateAsync(entry, cancellationToken);

        if (_redisConfiguration != null)
        {
            await _redisConfiguration.RemoveFromQueueAsync(
                request.EventId, request.Section, request.UserId);
        }

        return new CancelWaitlistResult
        {
            WaitlistEntryId = entry.Id,
            Status = entry.Status.ToString(),
            CancelledAt = entry.UpdatedAt
        };
    }
}
