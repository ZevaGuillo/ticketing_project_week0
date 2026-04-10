using MediatR;
using Ordering.Application.DTOs;
using Ordering.Application.Ports;

namespace Ordering.Application.UseCases.GetOrder;

public class GetOrderQueryHandler : IRequestHandler<GetOrderQuery, OrderDto?>
{
    private readonly IOrderRepository _orderRepository;

    public GetOrderQueryHandler(IOrderRepository orderRepository)
    {
        _orderRepository = orderRepository;
    }

    public async Task<OrderDto?> Handle(GetOrderQuery request, CancellationToken cancellationToken)
    {
        var order = await _orderRepository.GetByIdAsync(request.OrderId, cancellationToken);

        if (order == null)
            return null;

        var firstItem = order.Items.FirstOrDefault();
        var seatNumber = firstItem?.SeatLabel ?? (firstItem != null ? $"Seat-{firstItem.SeatId.ToString().Substring(0, 8)}" : "N/A");

        return new OrderDto(
            order.Id,
            order.UserId,
            order.UserEmail,
            order.GuestToken,
            order.TotalAmount,
            order.State,
            order.CreatedAt,
            order.PaidAt,
            order.Items.Select(i => new OrderItemDto(i.Id, i.SeatId, i.Price, i.SeatLabel)),
            order.EventName ?? "Evento",
            seatNumber,
            Guid.Empty
        );
    }
}
