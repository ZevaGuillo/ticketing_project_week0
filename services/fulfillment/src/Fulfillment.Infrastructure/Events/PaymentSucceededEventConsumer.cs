using Confluent.Kafka;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Fulfillment.Application.Ports;
using System.Net.Http.Json;
using System.Text.Json;
using Fulfillment.Domain.Entities;

namespace Fulfillment.Infrastructure.Events;

public class PaymentSucceededEventConsumer : BackgroundService
{
    private static readonly JsonSerializerOptions CamelCaseOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
    private readonly IConsumer<string, string> _consumer;
    private readonly KafkaOptions _kafkaOptions;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<PaymentSucceededEventConsumer> _logger;
    private readonly string _topicName;

    public PaymentSucceededEventConsumer(
        IOptions<KafkaOptions> kafkaOptions,
        IServiceProvider serviceProvider,
        ILogger<PaymentSucceededEventConsumer> logger)
    {
        _kafkaOptions = kafkaOptions.Value;
        _serviceProvider = serviceProvider;
        _logger = logger;

        var config = new ConsumerConfig
        {
            BootstrapServers = _kafkaOptions.BootstrapServers,
            GroupId = _kafkaOptions.ConsumerGroupId,
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = true,
        };

        _consumer = new ConsumerBuilder<string, string>(config).Build();
        _topicName = _kafkaOptions.Topics.TryGetValue("PaymentSucceeded", out var topic)
            ? topic
            : "payment-succeeded";
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("PaymentSucceededEventConsumer starting...");
        _consumer.Subscribe(_topicName);

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var consumeResult = _consumer.Consume(TimeSpan.FromSeconds(10));

                if (consumeResult == null)
                    continue;

                try
                {
                    var paymentEvent = JsonSerializer.Deserialize<PaymentSucceededEvent>(
                        consumeResult.Message.Value,
                        CamelCaseOptions);

                    if (paymentEvent == null)
                    {
                        _logger.LogWarning("Failed to deserialize payment-succeeded event");
                        continue;
                    }

                    // Process the event using the application handler
                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var ticketRepository = scope.ServiceProvider.GetRequiredService<ITicketRepository>();
                        var orderingServiceClient = scope.ServiceProvider.GetRequiredService<IOrderingServiceClient>();

                        // Check if ticket already exists (idempotency)
                        var existingTicket = await ticketRepository.GetByOrderIdAsync(paymentEvent.OrderId);
                        if (existingTicket != null)
                        {
                            _logger.LogInformation($"Ticket already exists for order {paymentEvent.OrderId}");
                            continue;
                        }

                        // ENRICHMENT: Fetch missing details from Ordering Service
                        var orderDetails = await orderingServiceClient.GetOrderDetailsAsync(paymentEvent.OrderId);
                        if (orderDetails == null)
                        {
                            _logger.LogWarning($"Could not enrich ticket data for order {paymentEvent.OrderId}. Order not found in Ordering service.");
                            continue;
                        }

                        // Resolve customer email: if the stored value is a UUID (no @), call Identity
                        var customerEmail = orderDetails.CustomerEmail;
                        if (!customerEmail.Contains('@'))
                        {
                            try
                            {
                                var httpFactory = scope.ServiceProvider.GetRequiredService<IHttpClientFactory>();
                                var identityClient = httpFactory.CreateClient("identity");
                                var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();
                                var identityBase = config["IdentityService:BaseUrl"] ?? "http://identity:5001";
                                var resp = await identityClient.GetAsync($"{identityBase}/internal/users/{customerEmail}");
                                if (resp.IsSuccessStatusCode)
                                {
                                    var userInfo = await resp.Content.ReadFromJsonAsync<IdentityUserResult>(
                                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                                    if (userInfo?.Email != null)
                                        customerEmail = userInfo.Email;
                                }
                            }
                            catch (Exception ex)
                            {
                                _logger.LogWarning(ex, "Could not resolve email from Identity for userId {UserId}", customerEmail);
                            }
                        }

                        // Create new ticket entity with enriched data
                        var ticket = new Ticket
                        {
                            Id = Guid.NewGuid(),
                            OrderId = orderDetails.OrderId,
                            CustomerEmail = customerEmail,
                            EventName = orderDetails.EventName,
                            SeatNumber = orderDetails.SeatNumber,
                            Price = orderDetails.Price,
                            Currency = orderDetails.Currency,
                            Status = TicketStatus.Pending,
                            QrCodeData = $"{orderDetails.OrderId}:{orderDetails.SeatNumber}:{orderDetails.EventId}",
                            CreatedAt = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow
                        };

                        // Generate PDF and store it
                        try
                        {
                            var pdfGenerator = scope.ServiceProvider.GetRequiredService<ITicketPdfGenerator>();
                            var storageService = scope.ServiceProvider.GetRequiredService<ITicketStorageService>();
                            var eventPublisher = scope.ServiceProvider.GetRequiredService<IEventPublisher>();

                            // Generate PDF
                            var (pdfStream, filename) = await pdfGenerator.GenerateTicketPdfAsync(ticket);

                            // Store PDF
                            var pdfPath = await storageService.SaveTicketPdfAsync(ticket.Id, pdfStream);
                            ticket.TicketPdfPath = pdfPath;
                            ticket.GeneratedAt = DateTime.UtcNow;
                            ticket.Status = TicketStatus.Generated;

                            // Save ticket to database
                            await ticketRepository.CreateAsync(ticket);

                            // Publish ticket-issued event
                            var ticketEvent = new TicketIssuedEvent
                            {
                                OrderId = ticket.OrderId,
                                TicketId = ticket.Id,
                                CustomerEmail = ticket.CustomerEmail,
                                TicketPdfUrl = $"/tickets/{pdfPath}",
                                EventName = ticket.EventName,
                                SeatNumber = ticket.SeatNumber,
                                Price = ticket.Price,
                                Currency = ticket.Currency,
                                IssuedAt = ticket.GeneratedAt,
                                Timestamp = DateTime.UtcNow
                            };

                            var topicName = _kafkaOptions.Topics.TryGetValue("TicketIssued", out var topic)
                                ? topic
                                : "ticket-issued";

                            await eventPublisher.PublishAsync(topicName, ticket.OrderId.ToString(), ticketEvent);

                            _logger.LogInformation($"Ticket generado y publicado para order {paymentEvent.OrderId}");
                        }
                        catch (Exception ticketEx)
                        {
                            _logger.LogError($"Error generando ticket: {ticketEx.Message}");
                            ticket.Status = TicketStatus.Failed;
                            await ticketRepository.CreateAsync(ticket);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error processing payment-succeeded event: {ex.Message}");
                }
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("PaymentSucceededEventConsumer cancelled");
        }
        finally
        {
            _consumer.Close();
            _consumer.Dispose();
        }
    }
}

internal record IdentityUserResult(Guid UserId, string Email);
