namespace Fulfillment.Application.UseCases.ProcessPaymentSucceeded;

public record ProcessPaymentSucceededResponse(
    Guid TicketId,
    bool Success,
    string Message);
