namespace Ordering.Infrastructure.Events;

/// <summary>
/// Configuration options for Kafka.
/// </summary>
public class KafkaOptions
{
    public const string Section = "Kafka";

    public string BootstrapServers { get; set; } = "localhost:9092";
    public string ConsumerGroupId { get; set; } = "ordering-service";
    public bool EnableConsumer { get; set; } = true;
}
