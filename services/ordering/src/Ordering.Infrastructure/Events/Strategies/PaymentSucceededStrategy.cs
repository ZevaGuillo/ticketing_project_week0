using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Ordering.Application.Ports;

namespace Ordering.Infrastructure.Events.Strategies;

public class PaymentSucceededStrategy : IOrderingEventStrategy
{
    public string Topic => "payment-succeeded";

    public async Task HandleAsync(JsonElement root, IServiceScope scope, CancellationToken cancellationToken)
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<PaymentSucceededStrategy>>();

        var paymentEvent = root.Deserialize<PaymentSucceededEvent>(new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        if (paymentEvent == null || !Guid.TryParse(paymentEvent.OrderId, out var orderId))
            return;

        var orderRepo = scope.ServiceProvider.GetRequiredService<IOrderRepository>();
        var order = await orderRepo.GetByIdAsync(orderId, cancellationToken);

        if (order != null)
        {
            order.State = "paid";
            order.PaidAt = DateTime.UtcNow;
            await orderRepo.UpdateAsync(order, cancellationToken);
            logger.LogInformation("Order {OrderId} updated to State: PAID", orderId);
        }
    }

    private record PaymentSucceededEvent
    {
        [JsonPropertyName("orderId")]
        public string OrderId { get; init; } = string.Empty;
    }
}
