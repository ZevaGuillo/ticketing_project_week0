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