using MediatR;
using Microsoft.Extensions.Logging;

namespace Fulfillment.Application.UseCases.ProcessPaymentSucceeded;

public class ProcessPaymentSucceededCommand : IRequest<ProcessPaymentSucceededResponse>
{
    public Guid OrderId { get; set; }
    public string CustomerEmail { get; set; } = string.Empty;
    public Guid EventId { get; set; }
    public string EventName { get; set; } = string.Empty;
    public string SeatNumber { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string Currency { get; set; } = "USD";
}

public class ProcessPaymentSucceededResponse
{
    public Guid TicketId { get; set; }
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
}

public class ProcessPaymentSucceededHandler : IRequestHandler<ProcessPaymentSucceededCommand, ProcessPaymentSucceededResponse>
{
    private readonly ILogger<ProcessPaymentSucceededHandler> _logger;

    public ProcessPaymentSucceededHandler(ILogger<ProcessPaymentSucceededHandler> logger)
    {
        _logger = logger;
    }

    public async Task<ProcessPaymentSucceededResponse> Handle(ProcessPaymentSucceededCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation($"Processing payment for order {request.OrderId}");
        
        // Placeholder - actual implementation in T036
        return await Task.FromResult(new ProcessPaymentSucceededResponse
        {
            Success = true,
            Message = "Ticket generation in progress"
        });
    }
}
