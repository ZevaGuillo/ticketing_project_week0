using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Inventory.Domain.Ports;
using Inventory.Infrastructure.Persistence;
using Inventory.Infrastructure.Locking;
using StackExchange.Redis;

namespace Inventory.Infrastructure;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<InventoryDbContext>(options =>
        {
            options.UseNpgsql(configuration.GetConnectionString("Default"));
        });

        services.AddScoped<IDbInitializer, DbInitializer>();

        // Configure Redis connection multiplexer and Redis lock adapter
        var redisConn = configuration.GetConnectionString("Redis") ?? configuration["Redis:Connection"] ?? "localhost:6379";
        var multiplexer = ConnectionMultiplexer.Connect(redisConn);
        services.AddSingleton<IConnectionMultiplexer>(multiplexer);
        services.AddScoped<IRedisLock, RedisLock>();

        return services;
    }
}
