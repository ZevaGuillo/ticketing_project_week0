using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MediatR;
using Notification.Application.UseCases.SendWaitlistNotification;
using Notification.Domain.Events;

namespace Notification.Infrastructure.Messaging.Strategies;

public class WaitlistOpportunityStrategy : INotificationEventStrategy
{
    public string Topic => "waitlist-opportunity";

    public async Task HandleAsync(JsonElement root, IServiceScope scope, CancellationToken ct)
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<WaitlistOpportunityStrategy>>();

        WaitlistOpportunityEvent? evt = null;
        try
        {
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
        var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        var identityBaseUrl = config["IdentityService:BaseUrl"] ?? "http://identity:5001";
        var catalogBaseUrl = config["CatalogService:BaseUrl"] ?? "http://catalog:5001";

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

        var command = new SendWaitlistNotificationCommand(
            OpportunityId: evt.OpportunityId,
            WaitlistEntryId: evt.WaitlistEntryId,
            UserId: evt.UserId,
            RecipientEmail: userInfo.Email,
            EventName: eventName,
            Section: evt.Section,
            OpportunityTTL: evt.OpportunityTTL,
            CreatedAt: evt.CreatedAt);

        var result = await mediator.Send(command, ct);

        if (result.Success)
            logger.LogInformation("Waitlist notification processed for opportunity {OpportunityId}: {Message}", evt.OpportunityId, result.Message);
        else
            logger.LogError("Failed to process waitlist notification for opportunity {OpportunityId}: {Message}", evt.OpportunityId, result.Message);
    }

    private record UserLookupResult(Guid UserId, string Email);
    private record CatalogEventResult(Guid Id, string Name, string? Description, DateTime? Date);
}
