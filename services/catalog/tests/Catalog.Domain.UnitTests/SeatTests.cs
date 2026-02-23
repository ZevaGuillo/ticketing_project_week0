using Catalog.Domain.Entities;
using FluentAssertions;

namespace Catalog.Domain.UnitTests;

public class SeatTests
{
    [Fact]
    public void Seat_Creation_ShouldSetPropertiesCorrectly()
    {
        // Arrange
        var seatId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        var sectionCode = "VIP";
        var rowNumber = 5;
        var seatNumber = 12;
        var price = 125.50m;
        var status = "reserved";

        // Act
        var seat = new Seat
        {
            Id = seatId,
            EventId = eventId,
            SectionCode = sectionCode,
            RowNumber = rowNumber,
            SeatNumber = seatNumber,
            Price = price,
            Status = status
        };

        // Assert
        seat.Id.Should().Be(seatId);
        seat.EventId.Should().Be(eventId);
        seat.SectionCode.Should().Be(sectionCode);
        seat.RowNumber.Should().Be(rowNumber);
        seat.SeatNumber.Should().Be(seatNumber);
        seat.Price.Should().Be(price);
        seat.Status.Should().Be(status);
        seat.Event.Should().BeNull();
    }

    [Fact]
    public void Seat_DefaultValues_ShouldBeSetCorrectly()
    {
        // Act
        var seat = new Seat();

        // Assert
        seat.Id.Should().Be(Guid.Empty);
        seat.EventId.Should().Be(Guid.Empty);
        seat.SectionCode.Should().Be(string.Empty);
        seat.RowNumber.Should().Be(0);
        seat.SeatNumber.Should().Be(0);
        seat.Price.Should().Be(0m);
        seat.Status.Should().Be("available");
        seat.Event.Should().BeNull();
    }

    [Theory]
    [InlineData("available")]
    [InlineData("reserved")]
    [InlineData("sold")]
    public void Seat_Status_ShouldAcceptValidStatuses(string status)
    {
        // Arrange & Act
        var seat = new Seat { Status = status };

        // Assert
        seat.Status.Should().Be(status);
    }

    [Theory]
    [InlineData("A", 1, 1)]
    [InlineData("VIP", 10, 25)]
    [InlineData("BALCONY", 5, 12)]
    public void Seat_LocationProperties_ShouldAcceptValidValues(string sectionCode, int rowNumber, int seatNumber)
    {
        // Arrange & Act
        var seat = new Seat
        {
            SectionCode = sectionCode,
            RowNumber = rowNumber,
            SeatNumber = seatNumber
        };

        // Assert
        seat.SectionCode.Should().Be(sectionCode);
        seat.RowNumber.Should().Be(rowNumber);
        seat.SeatNumber.Should().Be(seatNumber);
    }

    [Theory]
    [InlineData(0.01)]
    [InlineData(50.00)]
    [InlineData(999.99)]
    [InlineData(1500.25)]
    public void Seat_Price_ShouldAcceptValidPrices(decimal price)
    {
        // Arrange & Act
        var seat = new Seat { Price = price };

        // Assert
        seat.Price.Should().Be(price);
    }

    [Fact]
    public void Seat_Event_NavigationProperty_ShouldBeSettable()
    {
        // Arrange
        var eventEntity = new Event
        {
            Id = Guid.NewGuid(),
            Name = "Test Event",
            EventDate = DateTime.Now.AddDays(30)
        };

        var seat = new Seat
        {
            Id = Guid.NewGuid(),
            EventId = eventEntity.Id
        };

        // Act
        seat.Event = eventEntity;

        // Assert
        seat.Event.Should().Be(eventEntity);
        seat.EventId.Should().Be(eventEntity.Id);
    }

    [Fact]
    public void Seat_WithEvent_ShouldHaveMatchingEventId()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var eventEntity = new Event { Id = eventId };

        // Act
        var seat = new Seat
        {
            EventId = eventId,
            Event = eventEntity
        };

        // Assert
        seat.EventId.Should().Be(eventEntity.Id);
        seat.Event.Should().Be(eventEntity);
    }

    [Fact]
    public void Seat_StatusTransitions_ShouldWorkCorrectly()
    {
        // Arrange
        var seat = new Seat();

        // Act & Assert - Initial state
        seat.Status.Should().Be("available");

        // Act & Assert - Reserve seat
        seat.Status = "reserved";
        seat.Status.Should().Be("reserved");

        // Act & Assert - Sell seat
        seat.Status = "sold";
        seat.Status.Should().Be("sold");

        // Act & Assert - Back to available (e.g., reservation expired)
        seat.Status = "available";
        seat.Status.Should().Be("available");
    }
}