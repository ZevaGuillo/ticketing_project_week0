using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;

namespace Notification.Infrastructure.Messaging.Strategies;

public interface INotificationEventStrategy
{
    string Topic { get; }
    Task HandleAsync(JsonElement root, IServiceScope scope, CancellationToken ct);
}
