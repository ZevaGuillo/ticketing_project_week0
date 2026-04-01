using FluentAssertions;
using Inventory.Domain.Events;
using Inventory.Infrastructure.Messaging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Confluent.Kafka;

namespace Inventory.UnitTests.Messaging;

public class ReservationExpiredEventConsumerTests
{
    private readonly Mock<IServiceScopeFactory> _scopeFactoryMock;
    private readonly Mock<IConsumer<string?, string>> _consumerMock;
    private readonly Mock<IProducer<string?, string>> _dlqProducerMock;
    private readonly Mock<ILogger<ReservationExpiredEventConsumer>> _loggerMock;

    public ReservationExpiredEventConsumerTests()
    {
        _scopeFactoryMock = new Mock<IServiceScopeFactory>();
        _consumerMock = new Mock<IConsumer<string?, string>>();
        _dlqProducerMock = new Mock<IProducer<string?, string>>();
        _loggerMock = new Mock<ILogger<ReservationExpiredEventConsumer>>();
    }

    [Fact]
    public void New_CreatesInstanceSuccessfully()
    {
        var consumer = new ReservationExpiredEventConsumer(
            _scopeFactoryMock.Object,
            _consumerMock.Object,
            _dlqProducerMock.Object,
            _loggerMock.Object);

        consumer.Should().NotBeNull();
    }
}