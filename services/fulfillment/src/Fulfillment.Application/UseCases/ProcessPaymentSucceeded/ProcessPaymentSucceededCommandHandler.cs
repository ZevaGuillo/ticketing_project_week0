using Fulfillment.Application.Ports;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Fulfillment.Application.UseCases.ProcessPaymentSucceeded;

public class ProcessPaymentSucceededCommandHandler : IRequestHandler<ProcessPaymentSucceededCommand, ProcessPaymentSucceededResponse>
{
    private readonly ILogger<ProcessPaymentSucceededCommandHandler> _logger;
    private readonly IOrderingServiceClient _orderingService;
    private readonly ITicketRepository _ticketRepository;

    public ProcessPaymentSucceededCommandHandler(
        ILogger<ProcessPaymentSucceededCommandHandler> logger,
        IOrderingServiceClient orderingService,
        ITicketRepository ticketRepository)
    {
        _logger = logger;
        _orderingService = orderingService;
        _ticketRepository = ticketRepository;
    }

    public async Task<ProcessPaymentSucceededResponse> Handle(ProcessPaymentSucceededCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Processing payment for order {OrderId}", request.OrderId);

        var ticketId = Guid.NewGuid();

        return new ProcessPaymentSucceededResponse(
            TicketId: ticketId,
            Success: true,
            Message: "Ticket generated successfully");
    }
}
