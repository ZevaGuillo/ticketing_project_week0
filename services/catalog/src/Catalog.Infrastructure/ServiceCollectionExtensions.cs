using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Confluent.Kafka;
using Catalog.Application.Ports;
using Catalog.Application.UseCases.GetEventSeatmap;
using Catalog.Infrastructure.Persistence;
using Catalog.Infrastructure.Messaging;
using Catalog.Infrastructure.Messaging.Strategies;

namespace Catalog.Infrastructure;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Add Controllers
        services.AddControllers();

        // Add MediatR
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(GetEventSeatmapHandler).Assembly));

        // Add Database Context
        services.AddDbContext<CatalogDbContext>(options =>
        {
            options.UseNpgsql(configuration.GetConnectionString("Default"), 
                npgsqlOptions => npgsqlOptions.MigrationsHistoryTable("__EFMigrationsHistory", "bc_catalog"));
        });
        
        // Add Repository
        services.AddScoped<ICatalogRepository, CatalogRepository>();

        // Configure Kafka producer
        var kafkaBootstrapServers = configuration.GetConnectionString("Kafka") ?? configuration["Kafka:BootstrapServers"] ?? "localhost:9092";
        var kafkaConfig = new ProducerConfig
        {
            BootstrapServers = kafkaBootstrapServers,
            AllowAutoCreateTopics = true,
            Acks = Acks.All
        };
        
        // Use factory registration to avoid building producer during EF migrations
        services.AddSingleton(sp => new ProducerBuilder<string?, string>(kafkaConfig).Build());
        services.AddSingleton<IKafkaProducer, KafkaProducer>();

        // Kafka event strategies (Strategy pattern)
        services.AddScoped<IKafkaEventStrategy, WaitlistOpportunityStrategy>();
        services.AddScoped<IKafkaEventStrategy, ReservationCreatedStrategy>();
        services.AddScoped<IKafkaEventStrategy, ReservationExpiredStrategy>();
        services.AddScoped<IKafkaEventStrategy, PaymentSucceededStrategy>();
        services.AddScoped<IKafkaEventStrategy, SeatReleasedStrategy>();
        services.AddScoped<IKafkaEventStrategy, TicketIssuedStrategy>();

        // Kafka consumer
        services.AddHostedService<CatalogEventConsumer>();

        return services;
    }

    public static WebApplication UseInfrastructure(this WebApplication app)
    {

        // Configure the HTTP request pipeline
        if (app.Environment.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }

        app.UseCors("FrontendPolicy");
        app.UseRouting();
        
        // Add Authentication & Authorization middleware
        app.UseAuthentication();
        app.UseAuthorization();
        
        app.MapControllers();

        return app;
    }
}