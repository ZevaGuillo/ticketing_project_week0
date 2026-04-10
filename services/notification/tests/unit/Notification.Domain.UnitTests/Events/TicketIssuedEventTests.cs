using Notification.Domain.Events;

namespace Notification.Domain.UnitTests.Events;

public class TicketIssuedEventTests
{
    [Fact]
    public void TicketIssuedEvent_DefaultValues_ShouldBeCorrect()
    {
        // Act
        var evt = new TicketIssuedEvent();

        // Assert
        evt.TicketId.Should().Be(Guid.Empty);
        evt.OrderId.Should().Be(Guid.Empty);
        evt.CustomerEmail.Should().Be(string.Empty);
        evt.EventName.Should().Be(string.Empty);
        evt.SeatNumber.Should().Be(string.Empty);
        evt.Price.Should().Be(0);
        evt.Currency.Should().Be("USD");
        evt.TicketPdfUrl.Should().BeNull();
        evt.QrCodeData.Should().BeNull();
    }

    [Fact]
    public void TicketIssuedEvent_WithAllProperties_ShouldRetainValues()
    {
        // Arrange
        var ticketId = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        // Act
        var evt = new TicketIssuedEvent
        {
            TicketId = ticketId,
            OrderId = orderId,
            CustomerEmail = "customer@example.com",
            EventName = "Rock Concert",
            SeatNumber = "A1",
            Price = 99.99m,
            Currency = "PEN",
            TicketPdfUrl = "https://cdn.example.com/ticket.pdf",
            QrCodeData = "QR_ABC123",
            IssuedAt = now,
            Timestamp = now
        };

        // Assert
        evt.TicketId.Should().Be(ticketId);
        evt.OrderId.Should().Be(orderId);
        evt.CustomerEmail.Should().Be("customer@example.com");
        evt.EventName.Should().Be("Rock Concert");
        evt.SeatNumber.Should().Be("A1");
        evt.Price.Should().Be(99.99m);
        evt.Currency.Should().Be("PEN");
        evt.TicketPdfUrl.Should().Be("https://cdn.example.com/ticket.pdf");
        evt.QrCodeData.Should().Be("QR_ABC123");
        evt.IssuedAt.Should().Be(now);
        evt.Timestamp.Should().Be(now);
    }
}
