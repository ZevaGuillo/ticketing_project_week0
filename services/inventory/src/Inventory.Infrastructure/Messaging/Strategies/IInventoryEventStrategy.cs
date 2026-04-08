using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;

namespace Inventory.Infrastructure.Messaging.Strategies;

public interface IInventoryEventStrategy
{
    string Topic { get; }
    Task HandleAsync(JsonElement root, IServiceScope scope, CancellationToken ct);
}
