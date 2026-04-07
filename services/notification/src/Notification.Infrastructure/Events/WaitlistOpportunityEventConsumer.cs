using Confluent.Kafka;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Notification.Application.Email;
using Notification.Application.Ports;
using System.Net.Http.Json;
using System.Text.Json;

namespace Notification.Infrastructure.Events;

public class WaitlistOpportunityEventConsumer : BackgroundService
{
    private readonly IConsumer<string, string>? _consumer;
    private readonly KafkaOptions _kafkaOptions;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<WaitlistOpportunityEventConsumer> _logger;
    private readonly string _topicName;

    public WaitlistOpportunityEventConsumer(
        IOptions<KafkaOptions> kafkaOptions,
        IOptions<IdentityServiceOptions> identityOptions,
        IServiceProvider serviceProvider,
        ILogger<WaitlistOpportunityEventConsumer> logger,
        IConsumer<string, string>? consumer = null)
    {
        _kafkaOptions = kafkaOptions.Value;
        _serviceProvider = serviceProvider;
        _logger = logger;

        _topicName = _kafkaOptions.Topics.TryGetValue("WaitlistOpportunity", out var topic)
            ? topic
            : "waitlist-opportunity";

        if (consumer != null)
        {
            _consumer = consumer;
        }
        else
        {
            try
            {
                var config = new ConsumerConfig
                {
                    BootstrapServers = _kafkaOptions.BootstrapServers,
                    GroupId = "notification-service-waitlist",
                    AutoOffsetReset = AutoOffsetReset.Earliest,
                    EnableAutoCommit = true,
                };

                _consumer = new ConsumerBuilder<string, string>(config).Build();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create Kafka consumer for waitlist opportunities.");
            }
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (_consumer == null)
        {
            _logger.LogWarning("WaitlistOpportunityEventConsumer: Kafka consumer is null. Exiting.");
            return;
        }

        _logger.LogInformation("WaitlistOpportunityEventConsumer starting... (Topic: {Topic})", _topicName);

        try
        {
            _consumer.Subscribe(_topicName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to subscribe to Kafka topic {Topic}.", _topicName);
            return;
        }

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var consumeResult = _consumer.Consume(TimeSpan.FromSeconds(1));

                if (consumeResult == null)
                {
                    await Task.Delay(100, stoppingToken);
                    continue;
                }

                try
                {
                    var evt = JsonSerializer.Deserialize<WaitlistOpportunityEvent>(
                        consumeResult.Message.Value,
                        new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

                    if (evt == null)
                    {
                        _logger.LogWarning("Failed to deserialize waitlist-opportunity event.");
                        continue;
                    }

                    if (evt.Status != "OFFERED")
                    {
                        _logger.LogDebug("Skipping waitlist-opportunity event {Id} with status {Status}.", evt.OpportunityId, evt.Status);
                        continue;
                    }

                    await ProcessOpportunityAsync(evt, stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing waitlist-opportunity message.");
                }
            }
        }
        finally
        {
            _consumer.Close();
        }
    }

    private async Task ProcessOpportunityAsync(WaitlistOpportunityEvent evt, CancellationToken ct)
    {
        using var scope = _serviceProvider.CreateScope();
        var httpFactory = scope.ServiceProvider.GetRequiredService<IHttpClientFactory>();
        var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();
        var identityOptions = scope.ServiceProvider.GetRequiredService<IOptions<IdentityServiceOptions>>().Value;
        var config = scope.ServiceProvider.GetRequiredService<Microsoft.Extensions.Configuration.IConfiguration>();

        // Look up user email from Identity service
        var httpClient = httpFactory.CreateClient("identity");
        UserLookupResult? userInfo = null;
        try
        {
            var response = await httpClient.GetAsync(
                $"{identityOptions.BaseUrl}/internal/users/{evt.UserId}", ct);

            if (response.IsSuccessStatusCode)
            {
                userInfo = await response.Content.ReadFromJsonAsync<UserLookupResult>(
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true }, ct);
            }
            else
            {
                _logger.LogWarning("Identity service returned {Status} for user {UserId}.", response.StatusCode, evt.UserId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to look up user {UserId} from Identity service.", evt.UserId);
        }

        if (userInfo == null)
        {
            _logger.LogWarning("Skipping waitlist opportunity email: could not resolve user {UserId}.", evt.UserId);
            return;
        }

        // Look up event name from Catalog service
        var catalogBase = config["CatalogService:BaseUrl"] ?? "http://catalog:5001";
        var eventName = $"Evento {evt.EventId.ToString()[..8].ToUpper()}";
        try
        {
            var catalogClient = httpFactory.CreateClient();
            var catalogResp = await catalogClient.GetAsync($"{catalogBase}/events/{evt.EventId}", ct);
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
            _logger.LogWarning(ex, "Could not resolve event name from Catalog for eventId {EventId}.", evt.EventId);
        }

        var expiresAt = evt.CreatedAt.AddSeconds(evt.OpportunityTTL);
        var purchaseUrl = $"http://localhost:3000/events/{evt.EventId}";
        var subject = "¡Tienes una oportunidad de compra!";
        var body = EmailTemplates.WaitlistOpportunity(
            userInfo.Email,
            evt.Section,
            expiresAt,
            eventName,
            purchaseUrl);

        var sent = await emailService.SendAsync(userInfo.Email, subject, body);
        if (sent)
            _logger.LogInformation(
                "Waitlist opportunity email sent to {Email} for opportunity {Id}.",
                userInfo.Email, evt.OpportunityId);
        else
            _logger.LogWarning(
                "Failed to send waitlist opportunity email to {Email} for opportunity {Id}.",
                userInfo.Email, evt.OpportunityId);
    }

    private record UserLookupResult(Guid UserId, string Email);
    private record CatalogEventResult(Guid Id, string Name, string? Description, DateTime? Date);
}
