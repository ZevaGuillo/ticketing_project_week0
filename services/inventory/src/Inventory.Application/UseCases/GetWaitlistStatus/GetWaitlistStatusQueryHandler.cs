using Inventory.Domain.Ports;
using Inventory.Infrastructure.Configuration;
using MediatR;

namespace Inventory.Application.UseCases.GetWaitlistStatus;

public class GetWaitlistStatusQueryHandler : IRequestHandler<GetWaitlistStatusQuery, GetWaitlistStatusResponse?>
{
    private readonly IWaitlistRepository _waitlistRepository;
    private readonly WaitlistRedisConfiguration? _redisConfiguration;

    public GetWaitlistStatusQueryHandler(
        IWaitlistRepository waitlistRepository,
        WaitlistRedisConfiguration? redisConfiguration)
    {
        _waitlistRepository = waitlistRepository;
        _redisConfiguration = redisConfiguration;
    }

    public async Task<GetWaitlistStatusResponse?> Handle(GetWaitlistStatusQuery request, CancellationToken cancellationToken)
    {
        var entry = await _waitlistRepository.GetByUserEventSectionAsync(
            request.UserId, request.EventId, request.Section, cancellationToken);

        if (entry == null)
        {
            return null;
        }

        var position = _redisConfiguration != null
            ? await _redisConfiguration.GetQueuePositionAsync(request.EventId, request.Section, request.UserId)
            : 0;

        return new GetWaitlistStatusResponse
        {
            WaitlistEntryId = entry.Id,
            UserId = entry.UserId,
            EventId = entry.EventId,
            Section = entry.Section,
            QueuePosition = (int)position,
            Status = entry.Status.ToString(),
            JoinedAt = entry.JoinedAt,
            NotifiedAt = entry.NotifiedAt,
            CancelledAt = entry.CancelledAt
        };
    }
}