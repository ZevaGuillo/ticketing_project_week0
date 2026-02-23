using MediatR;
using Ordering.Application.DTOs;
using Ordering.Application.Ports;
using Ordering.Domain.Entities;

namespace Ordering.Application.UseCases.AddToCart;

public sealed class AddToCartHandler : IRequestHandler<AddToCartCommand, AddToCartResponse>
{
    private readonly IOrderRepository _orderRepository;

    public AddToCartHandler(IOrderRepository orderRepository)
    {
        _orderRepository = orderRepository;
    }

    public async Task<AddToCartResponse> Handle(AddToCartCommand request, CancellationToken cancellationToken)
    {
        try 
        {
            // Find existing draft order or create new one
            var existingOrder = await _orderRepository.GetDraftOrderAsync(
                request.UserId, request.GuestToken, cancellationToken);

            Order order;
            
            if (existingOrder != null)
            {
                // Add item to existing order
                var newItem = new OrderItem
                {
                    Id = Guid.NewGuid(),
                    OrderId = existingOrder.Id,
                    SeatId = request.SeatId,
                    Price = request.Price
                };
                
                existingOrder.Items.Add(newItem);
                existingOrder.TotalAmount = existingOrder.Items.Sum(i => i.Price);
                
                order = await _orderRepository.UpdateAsync(existingOrder, cancellationToken);
            }
            else
            {
                // Create new draft order
                order = new Order
                {
                    Id = Guid.NewGuid(),
                    UserId = request.UserId,
                    GuestToken = request.GuestToken,
                    TotalAmount = request.Price,
                    State = "draft",
                    CreatedAt = DateTime.UtcNow,
                    Items = new List<OrderItem>
                    {
                        new OrderItem
                        {
                            Id = Guid.NewGuid(),
                            SeatId = request.SeatId,
                            Price = request.Price
                        }
                    }
                };
                
                // Set the OrderId on the item
                order.Items.First().OrderId = order.Id;
                
                order = await _orderRepository.CreateAsync(order, cancellationToken);
            }

            var orderDto = new OrderDto(
                order.Id,
                order.UserId,
                order.GuestToken,
                order.TotalAmount,
                order.State,
                order.CreatedAt,
                order.PaidAt,
                order.Items.Select(i => new OrderItemDto(i.Id, i.SeatId, i.Price))
            );

            return new AddToCartResponse(true, null, orderDto);
        }
        catch (Exception ex)
        {
            return new AddToCartResponse(false, $"Failed to add item to cart: {ex.Message}", null);
        }
    }
}