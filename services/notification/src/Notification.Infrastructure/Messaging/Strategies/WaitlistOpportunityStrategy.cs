using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Notification.Application.Email;
using Notification.Application.Ports;
using Notification.Domain.Events;

namespace Notification.Infrastructure.Messaging.Strategies;

public class WaitlistOpportunityStrategy : INotificationEventStrategy
{
    public string Topic => "waitlist-opportunity";

    public async Task HandleAsync(JsonElement root, IServiceScope scope, CancellationToken ct)
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<WaitlistOpportunityStrategy>>();

        // Try both PascalCase (actual Kafka events) and camelCase (model annotations) 
        WaitlistOpportunityEvent? evt = null;
        try
        {
            // First try with PropertyNameCaseInsensitive to handle both cases
            evt = JsonSerializer.Deserialize<WaitlistOpportunityEvent>(
                root.GetRawText(),
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch (JsonException ex)
        {
            logger.LogError(ex, "Failed to deserialize waitlist-opportunity event: {EventData}", root.GetRawText());
        }

        if (evt == null)
        {
            logger.LogWarning("Failed to deserialize waitlist-opportunity event");
            return;
        }

        if (evt.Status != "OFFERED")
        {
            logger.LogDebug("Skipping waitlist-opportunity event {Id} with status {Status}", evt.OpportunityId, evt.Status);
            return;
        }

        var httpFactory = scope.ServiceProvider.GetRequiredService<IHttpClientFactory>();
        var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();
        var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();

        var identityBaseUrl = config["IdentityService:BaseUrl"] ?? "http://identity:5001";
        var catalogBaseUrl = config["CatalogService:BaseUrl"] ?? "http://catalog:5001";

        // Resolve user email from Identity service
        var httpClient = httpFactory.CreateClient("identity");
        UserLookupResult? userInfo = null;
        try
        {
            var response = await httpClient.GetAsync($"{identityBaseUrl}/internal/users/{evt.UserId}", ct);
            if (response.IsSuccessStatusCode)
                userInfo = await response.Content.ReadFromJsonAsync<UserLookupResult>(
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true }, ct);
            else
                logger.LogWarning("Identity service returned {Status} for user {UserId}", response.StatusCode, evt.UserId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to look up user {UserId} from Identity service", evt.UserId);
        }

        if (userInfo == null)
        {
            logger.LogWarning("Skipping waitlist opportunity email: could not resolve user {UserId}", evt.UserId);
            return;
        }

        // Resolve event name from Catalog service
        var eventName = $"Evento {evt.EventId.ToString()[..8].ToUpper()}";
        try
        {
            var catalogClient = httpFactory.CreateClient();
            var catalogResp = await catalogClient.GetAsync($"{catalogBaseUrl}/events/{evt.EventId}", ct);
            if (catalogResp.IsSuccessStatusCode)
            {
                var eventData = await catalogResp.Content.ReadFromJsonAsync<CatalogEventResult>(
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true }, ct);
                if (!string.IsNullOrEmpty(eventData?.Name))
                    eventName = eventData.Name;
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Could not resolve event name from Catalog for eventId {EventId}", evt.EventId);
        }

        var expiresAt = evt.CreatedAt.AddSeconds(evt.OpportunityTTL);
        var purchaseUrl = $"http://localhost:3000/events/{evt.EventId}";
        var subject = "¡Tienes una oportunidad de compra!";
        var body = EmailTemplates.WaitlistOpportunity(userInfo.Email, evt.Section, expiresAt, eventName, purchaseUrl);

        var sent = await emailService.SendAsync(userInfo.Email, subject, body);
        if (sent)
            logger.LogInformation("Waitlist opportunity email sent to {Email} for opportunity {Id}", userInfo.Email, evt.OpportunityId);
        else
            logger.LogWarning("Failed to send waitlist opportunity email to {Email} for opportunity {Id}", userInfo.Email, evt.OpportunityId);
    }

    private record UserLookupResult(Guid UserId, string Email);
    private record CatalogEventResult(Guid Id, string Name, string? Description, DateTime? Date);
}
