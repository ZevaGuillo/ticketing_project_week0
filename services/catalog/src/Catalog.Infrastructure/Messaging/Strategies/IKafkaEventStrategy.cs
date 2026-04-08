using System.Text.Json;
using Catalog.Application.Ports;

namespace Catalog.Infrastructure.Messaging.Strategies;

public interface IKafkaEventStrategy
{
    string Topic { get; }
    Task HandleAsync(JsonElement root, ICatalogRepository repository);
}
