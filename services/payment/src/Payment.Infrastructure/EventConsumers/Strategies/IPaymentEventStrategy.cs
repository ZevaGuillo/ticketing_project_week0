using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;

namespace Payment.Infrastructure.EventConsumers.Strategies;

public interface IPaymentEventStrategy
{
    string Topic { get; }
    Task HandleAsync(JsonElement root, IServiceScope scope, CancellationToken cancellationToken);
}
