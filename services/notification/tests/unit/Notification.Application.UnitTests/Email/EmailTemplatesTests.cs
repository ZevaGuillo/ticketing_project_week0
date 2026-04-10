using Notification.Application.Email;

namespace Notification.Application.UnitTests.Email;

public class EmailTemplatesTests
{
    [Fact]
    public void WaitlistOpportunity_ShouldContainUserName()
    {
        var html = EmailTemplates.WaitlistOpportunity(
            "Juan Pérez", "VIP", DateTime.UtcNow.AddMinutes(10),
            "Concierto Rock", "https://example.com/buy");

        html.Should().Contain("Juan Pérez");
    }

    [Fact]
    public void WaitlistOpportunity_ShouldContainSectionName()
    {
        var html = EmailTemplates.WaitlistOpportunity(
            "Ana", "VIP", DateTime.UtcNow.AddMinutes(10),
            "Concierto", "https://example.com/buy");

        html.Should().Contain("VIP");
    }

    [Fact]
    public void WaitlistOpportunity_ShouldContainEventName()
    {
        var html = EmailTemplates.WaitlistOpportunity(
            "Ana", "VIP", DateTime.UtcNow.AddMinutes(10),
            "Concierto Jazz Noche", "https://example.com/buy");

        html.Should().Contain("Concierto Jazz Noche");
    }

    [Fact]
    public void WaitlistOpportunity_ShouldContainPurchaseUrl()
    {
        var url = "https://example.com/events/123";
        var html = EmailTemplates.WaitlistOpportunity(
            "Ana", "A", DateTime.UtcNow.AddMinutes(5),
            "Evento", url);

        html.Should().Contain($"href=\"{url}\"");
    }

    [Fact]
    public void WaitlistOpportunity_ShouldShowMinutesLeft()
    {
        var html = EmailTemplates.WaitlistOpportunity(
            "Ana", "A", DateTime.UtcNow.AddMinutes(8),
            "Evento", "https://example.com");

        html.Should().Contain("min");
    }

    [Fact]
    public void WaitlistOpportunity_WithExpiredTime_ShouldShowAtLeast1Minute()
    {
        var html = EmailTemplates.WaitlistOpportunity(
            "Ana", "A", DateTime.UtcNow.AddMinutes(-5),
            "Evento", "https://example.com");

        html.Should().Contain("1 min");
    }

    [Fact]
    public void WaitlistOpportunity_ShouldReturnValidHtml()
    {
        var html = EmailTemplates.WaitlistOpportunity(
            "Test", "General", DateTime.UtcNow.AddMinutes(10),
            "Evento", "https://example.com");

        html.Should().Contain("<!DOCTYPE html>");
        html.Should().Contain("</html>");
    }

    [Fact]
    public void TicketConfirmation_ShouldContainEventName()
    {
        var html = EmailTemplates.TicketConfirmation(
            "Concierto Rock", "A1-5", 99.99m, "USD", DateTime.UtcNow);

        html.Should().Contain("Concierto Rock");
    }

    [Fact]
    public void TicketConfirmation_ShouldContainSeatNumber()
    {
        var html = EmailTemplates.TicketConfirmation(
            "Evento", "VIP-3-12", 50m, "USD", DateTime.UtcNow);

        html.Should().Contain("VIP-3-12");
    }

    [Fact]
    public void TicketConfirmation_ShouldContainFormattedPrice()
    {
        var html = EmailTemplates.TicketConfirmation(
            "Evento", "A1", 150.50m, "USD", DateTime.UtcNow);

        html.Should().Contain("150");
        html.Should().Contain("50");
        html.Should().Contain("USD");
    }

    [Fact]
    public void TicketConfirmation_ShouldContainIssuedDate()
    {
        var issuedAt = new DateTime(2026, 4, 7, 15, 30, 0, DateTimeKind.Utc);
        var html = EmailTemplates.TicketConfirmation(
            "Evento", "A1", 100m, "USD", issuedAt);

        html.Should().Contain("07/04/2026 15:30");
    }

    [Fact]
    public void TicketConfirmation_ShouldReturnValidHtml()
    {
        var html = EmailTemplates.TicketConfirmation(
            "Evento", "A1", 100m, "USD", DateTime.UtcNow);

        html.Should().Contain("<!DOCTYPE html>");
        html.Should().Contain("</html>");
        html.Should().Contain("¡Tu compra fue exitosa!");
    }
}
