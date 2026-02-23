using Catalog.Domain.Entities;
using FluentAssertions;

namespace Catalog.Domain.UnitTests;

public class EventTests
{
    [Fact]
    public void Event_Creation_ShouldSetPropertiesCorrectly()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var eventName = "Test Concert";
        var eventDescription = "A great test concert";
        var eventDate = DateTime.Now.AddDays(30);
        var basePrice = 50.00m;

        // Act
        var eventEntity = new Event
        {
            Id = eventId,
            Name = eventName,
            Description = eventDescription,
            EventDate = eventDate,
            BasePrice = basePrice
        };

        // Assert
        eventEntity.Id.Should().Be(eventId);
        eventEntity.Name.Should().Be(eventName);
        eventEntity.Description.Should().Be(eventDescription);
        eventEntity.EventDate.Should().Be(eventDate);
        eventEntity.BasePrice.Should().Be(basePrice);
        eventEntity.Seats.Should().NotBeNull().And.BeEmpty();
    }

    [Fact]
    public void Event_DefaultValues_ShouldBeSetCorrectly()
    {
        // Act
        var eventEntity = new Event();

        // Assert
        eventEntity.Id.Should().Be(Guid.Empty);
        eventEntity.Name.Should().Be(string.Empty);
        eventEntity.Description.Should().Be(string.Empty);
        eventEntity.EventDate.Should().Be(DateTime.MinValue);
        eventEntity.BasePrice.Should().Be(0m);
        eventEntity.Seats.Should().NotBeNull().And.BeEmpty();
    }

    [Fact]
    public void Event_AddSeat_ShouldAddToSeatsCollection()
    {
        // Arrange
        var eventEntity = new Event { Id = Guid.NewGuid() };
        var seat = new Seat
        {
            Id = Guid.NewGuid(),
            EventId = eventEntity.Id,
            SectionCode = "A",
            RowNumber = 1,
            SeatNumber = 1,
            Price = 75.00m
        };

        // Act
        eventEntity.Seats.Add(seat);

        // Assert
        eventEntity.Seats.Should().HaveCount(1);
        eventEntity.Seats.First().Should().Be(seat);
    }

    [Theory]
    [InlineData(0.01)]
    [InlineData(50.00)]
    [InlineData(999.99)]
    public void Event_BasePrice_ShouldAcceptValidValues(decimal price)
    {
        // Arrange & Act
        var eventEntity = new Event { BasePrice = price };

        // Assert
        eventEntity.BasePrice.Should().Be(price);
    }

    [Fact]
    public void Event_EventDate_ShouldAcceptFutureDate()
    {
        // Arrange
        var futureDate = DateTime.Now.AddDays(30);
        var eventEntity = new Event();

        // Act
        eventEntity.EventDate = futureDate;

        // Assert
        eventEntity.EventDate.Should().Be(futureDate);
    }
}