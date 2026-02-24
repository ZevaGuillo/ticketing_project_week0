using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Payment.Application.Ports;
using Payment.Infrastructure.Events;
using Payment.Infrastructure.Persistence;

namespace Payment.Infrastructure;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<PaymentDbContext>(options =>
        {
            options.UseNpgsql(configuration.GetConnectionString("Default"), 
                npgsqlOptions => npgsqlOptions.MigrationsHistoryTable("__EFMigrationsHistory", "bc_payment"));
        });
        
        services.AddScoped<IDbInitializer, DbInitializer>();
        
        // Configure Kafka options
        services.Configure<KafkaOptions>(
            configuration.GetSection(KafkaOptions.Section));

        return services;
    }
}