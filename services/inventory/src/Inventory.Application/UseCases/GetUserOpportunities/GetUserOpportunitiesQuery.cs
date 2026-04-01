using Inventory.Domain.Ports;
using MediatR;

namespace Inventory.Application.UseCases.GetUserOpportunities;

public record GetUserOpportunitiesQuery(Guid UserId, Guid EventId) : IRequest<List<OpportunityDto>>;

public record OpportunityDto(
    Guid OpportunityId,
    Guid SeatId,
    string Section,
    string Token,
    string Status,
    DateTime ExpiresAt
);

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