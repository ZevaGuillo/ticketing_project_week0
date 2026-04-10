using FluentAssertions;
using Inventory.Infrastructure.Messaging.Strategies;

namespace Inventory.UnitTests.Messaging;

public class ReservationExpiredEventConsumerTests
{
    [Fact]
    public void ReservationExpiredStrategy_HasCorrectTopic()
    {
        var strategy = new ReservationExpiredStrategy();
        strategy.Topic.Should().Be("reservation-expired");
    }
}