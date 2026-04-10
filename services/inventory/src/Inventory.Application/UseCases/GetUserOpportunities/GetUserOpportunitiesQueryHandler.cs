
using Inventory.Domain.Ports;
using MediatR;

namespace Inventory.Application.UseCases.GetUserOpportunities;

public class GetUserOpportunitiesHandler : IRequestHandler<GetUserOpportunitiesQuery, List<OpportunityDto>>
{
    private readonly IOpportunityWindowRepository _opportunityWindowRepository;

    public GetUserOpportunitiesHandler(IOpportunityWindowRepository opportunityWindowRepository)
    {
        _opportunityWindowRepository = opportunityWindowRepository;
    }

    public async Task<List<OpportunityDto>> Handle(GetUserOpportunitiesQuery request, CancellationToken cancellationToken)
    {
        var opportunities = await _opportunityWindowRepository.GetActiveByUserAndEventAsync(
            request.UserId, 
            request.EventId, 
            cancellationToken);

        return opportunities.Select(o => new OpportunityDto(
            o.Id,
            o.SeatId,
            o.WaitlistEntry?.Section ?? string.Empty,
            o.Token,
            o.Status.ToString(),
            o.ExpiresAt
        )).ToList();
    }
}