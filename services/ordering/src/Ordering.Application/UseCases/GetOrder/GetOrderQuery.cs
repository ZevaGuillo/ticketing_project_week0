using MediatR;
using Ordering.Application.DTOs;

namespace Ordering.Application.UseCases.GetOrder;

public record GetOrderQuery(Guid OrderId) : IRequest<OrderDto?>;
