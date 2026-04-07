using Inventory.Infrastructure;
using Inventory.Infrastructure.Configuration;
using Inventory.Infrastructure.Consumers;
using Inventory.Infrastructure.Locking;
using Inventory.Infrastructure.Messaging;
using Inventory.Infrastructure.Persistence;
using Inventory.Domain.Ports;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using Confluent.Kafka;
using Microsoft.Extensions.Hosting;

namespace Inventory.Infrastructure;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Configure Reservation settings
        services.AddSingleton<ReservationSettings>(sp =>
        {
            var options = new ReservationSettings();
            configuration.GetSection("Reservation").Bind(options);
            var envValue = Environment.GetEnvironmentVariable("Reservation__TTLMinutes");
            if (int.TryParse(envValue, out var ttlMinutes) && ttlMinutes > 0)
            {
                options.TTLMinutes = ttlMinutes;
            }
            return options;
        });

        // Configure Waitlist settings
        services.AddSingleton<WaitlistSettings>(sp =>
        {
            var options = new WaitlistSettings();
            configuration.GetSection("Waitlist").Bind(options);
            var envValue = Environment.GetEnvironmentVariable("Waitlist__OpportunityTTLMinutes");
            if (int.TryParse(envValue, out var ttlMinutes) && ttlMinutes > 0)
            {
                options.OpportunityTTLMinutes = ttlMinutes;
            }
            return options;
        });
        
        services.AddDbContext<InventoryDbContext>(options =>
        {
            options.UseNpgsql(configuration.GetConnectionString("Default"), 
                npgsqlOptions => npgsqlOptions.MigrationsHistoryTable("__EFMigrationsHistory", "bc_inventory"));
        });

        // IDbInitializer removed - migrations handled externally

        // Configure Redis connection multiplexer and Redis lock adapter
        var redisConn = configuration.GetConnectionString("Redis") ?? configuration["Redis:Connection"] ?? "localhost:6379";
        
        // Lazy registration for Redis to avoid connection on EF build
        services.AddSingleton<IConnectionMultiplexer>(sp => ConnectionMultiplexer.Connect(redisConn));
        services.AddScoped<IRedisLock, RedisLock>();
        services.AddScoped<WaitlistRedisConfiguration>();
        services.AddScoped<IReservationRepository, ReservationRepository>();
        services.AddScoped<IWaitlistRepository, WaitlistRepository>();
        services.AddScoped<IOpportunityWindowRepository, OpportunityWindowRepository>();

        // Configure Kafka producer
        var kafkaBootstrapServers = configuration.GetConnectionString("Kafka") ?? configuration["Kafka:BootstrapServers"] ?? "localhost:9092";
        var kafkaConfig = new ProducerConfig
        {
            BootstrapServers = kafkaBootstrapServers,
            AllowAutoCreateTopics = true,
            Acks = Acks.All
        };
        
        services.AddSingleton(sp => new ProducerBuilder<string?, string>(kafkaConfig).Build());
        services.AddSingleton<IKafkaProducer, KafkaProducer>();

        // Register inventory event consumer
        services.AddHostedService<Inventory.Infrastructure.Messaging.InventoryEventConsumer>();

        // Register reservation-expired consumer for waitlist
        var waitlistConsumerConfig = new ConsumerConfig
        {
            BootstrapServers = kafkaBootstrapServers,
            GroupId = "waitlist-consumer-group",
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false,
            SessionTimeoutMs = 30000,
            EnablePartitionEof = true
        };
        
        services.AddSingleton<IHostedService, ReservationExpiredEventConsumer>(sp =>
        {
            var scopeFactory = sp.GetRequiredService<IServiceScopeFactory>();
            var consumer = new ConsumerBuilder<string?, string>(waitlistConsumerConfig).Build();
            var dlqProducer = sp.GetRequiredService<IProducer<string?, string>>();
            var logger = sp.GetRequiredService<ILogger<ReservationExpiredEventConsumer>>();
            var waitlistSettings = sp.GetRequiredService<WaitlistSettings>();
            return new ReservationExpiredEventConsumer(scopeFactory, consumer, dlqProducer, logger, waitlistSettings);
        });

        // Register expiry worker as hosted service (optional in tests)
        services.AddSingleton<IHostedService, Inventory.Infrastructure.Workers.ReservationExpiryWorker>(sp =>
        {
            var scopeFactory = sp.GetRequiredService<IServiceScopeFactory>();
            var kafka = sp.GetRequiredService<IKafkaProducer>();
            return new Inventory.Infrastructure.Workers.ReservationExpiryWorker(scopeFactory, kafka);
        });

        // Register opportunity expiry worker for waitlist re-selection
        services.AddSingleton<IHostedService, Inventory.Infrastructure.Workers.OpportunityExpiryWorker>(sp =>
        {
            var scopeFactory = sp.GetRequiredService<IServiceScopeFactory>();
            var kafka = sp.GetRequiredService<IKafkaProducer>();
            var logger = sp.GetRequiredService<ILogger<Inventory.Infrastructure.Workers.OpportunityExpiryWorker>>();
            var waitlistSettings = sp.GetRequiredService<WaitlistSettings>();
            return new Inventory.Infrastructure.Workers.OpportunityExpiryWorker(scopeFactory, kafka, logger, waitlistSettings);
        });

        // Register seats-generated Kafka consumer as hosted service
        var consumerConfig = new ConsumerConfig
        {
            BootstrapServers = kafkaBootstrapServers,
            GroupId = "inventory-seats-consumer",
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false
        };
        
        services.AddSingleton<IHostedService, SeatsGeneratedConsumer>(sp =>
        {
            var scopeFactory = sp.GetRequiredService<IServiceScopeFactory>();
            var consumer = new ConsumerBuilder<string?, string>(consumerConfig).Build();
            return new SeatsGeneratedConsumer(scopeFactory, consumer);
        });

        return services;
    }
}
