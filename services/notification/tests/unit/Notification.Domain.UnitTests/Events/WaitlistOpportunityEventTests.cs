using Notification.Domain.Events;

namespace Notification.Domain.UnitTests.Events;

public class WaitlistOpportunityEventTests
{
    [Fact]
    public void WaitlistOpportunityEvent_DefaultValues_ShouldBeCorrect()
    {
        // Act
        var evt = new WaitlistOpportunityEvent();

        // Assert
        evt.OpportunityId.Should().Be(Guid.Empty);
        evt.WaitlistEntryId.Should().Be(Guid.Empty);
        evt.UserId.Should().Be(Guid.Empty);
        evt.EventId.Should().Be(Guid.Empty);
        evt.SeatId.Should().Be(Guid.Empty);
        evt.Section.Should().Be(string.Empty);
        evt.OpportunityTTL.Should().Be(600);
        evt.Status.Should().Be("OFFERED");
    }

    [Fact]
    public void WaitlistOpportunityEvent_WithAllProperties_ShouldRetainValues()
    {
        // Arrange
        var opportunityId = Guid.NewGuid();
        var waitlistEntryId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        var seatId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        // Act
        var evt = new WaitlistOpportunityEvent
        {
            OpportunityId = opportunityId,
            WaitlistEntryId = waitlistEntryId,
            UserId = userId,
            EventId = eventId,
            SeatId = seatId,
            Section = "VIP",
            OpportunityTTL = 300,
            CreatedAt = now,
            Status = "ACCEPTED"
        };

        // Assert
        evt.OpportunityId.Should().Be(opportunityId);
        evt.WaitlistEntryId.Should().Be(waitlistEntryId);
        evt.UserId.Should().Be(userId);
        evt.EventId.Should().Be(eventId);
        evt.SeatId.Should().Be(seatId);
        evt.Section.Should().Be("VIP");
        evt.OpportunityTTL.Should().Be(300);
        evt.CreatedAt.Should().Be(now);
        evt.Status.Should().Be("ACCEPTED");
    }
}
