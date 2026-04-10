using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;

namespace Ordering.Infrastructure.Events.Strategies;

public interface IOrderingEventStrategy
{
    string Topic { get; }
    Task HandleAsync(JsonElement root, IServiceScope scope, CancellationToken cancellationToken);
}
