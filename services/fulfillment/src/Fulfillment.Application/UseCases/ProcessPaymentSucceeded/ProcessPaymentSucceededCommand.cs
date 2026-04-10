using MediatR;

namespace Fulfillment.Application.UseCases.ProcessPaymentSucceeded;

public record ProcessPaymentSucceededCommand(
    Guid OrderId,
    string CustomerEmail,
    Guid EventId,
    string EventName,
    string SeatNumber,
    decimal Price,
    string Currency = "USD") : IRequest<ProcessPaymentSucceededResponse>;
