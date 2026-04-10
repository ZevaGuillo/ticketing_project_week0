using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Inventory.Infrastructure.Persistence;
using Inventory.Infrastructure.Configuration;
using Inventory.Domain.Ports;
using Moq;
using StackExchange.Redis;

namespace Inventory.IntegrationTests;

public class InventoryApiFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<InventoryDbContext>));

            if (descriptor != null)
            {
                services.Remove(descriptor);
            }

            services.AddDbContext<InventoryDbContext>(options =>
            {
                options.UseInMemoryDatabase("InventoryTestDb");
            });

            var redisLockMock = new Mock<IRedisLock>();
            redisLockMock.Setup(x => x.AcquireLockAsync(It.IsAny<string>(), It.IsAny<TimeSpan>()))
                .ReturnsAsync("test-lock-token");
            redisLockMock.Setup(x => x.ReleaseLockAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(true);

            services.AddSingleton(redisLockMock.Object);

            var kafkaProducerMock = new Mock<IKafkaProducer>();
            kafkaProducerMock.Setup(x => x.ProduceAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            services.AddSingleton(kafkaProducerMock.Object);

            var reservationRepoMock = new Mock<IReservationRepository>();
            reservationRepoMock.Setup(x => x.AddAsync(It.IsAny<Inventory.Domain.Entities.Reservation>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Inventory.Domain.Entities.Reservation r, CancellationToken _) => r);
            reservationRepoMock.Setup(x => x.UpdateAsync(It.IsAny<Inventory.Domain.Entities.Reservation>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            services.AddSingleton(reservationRepoMock.Object);

            var connectionMultiplexerMock = new Mock<IConnectionMultiplexer>();
            var databaseMock = new Mock<IDatabase>();
            
            databaseMock.Setup(x => x.SortedSetAddAsync(
                It.IsAny<RedisKey>(),
                It.IsAny<RedisValue>(),
                It.IsAny<double>(),
                It.IsAny<When>(),
                It.IsAny<CommandFlags>()))
                .ReturnsAsync(true);
            
            databaseMock.Setup(x => x.SortedSetRankAsync(
                It.IsAny<RedisKey>(),
                It.IsAny<RedisValue>(),
                It.IsAny<Order>(),
                It.IsAny<CommandFlags>()))
                .ReturnsAsync((long?)1);
            
            connectionMultiplexerMock.Setup(x => x.GetDatabase(It.IsAny<int>(), It.IsAny<object>())).Returns(databaseMock.Object);
            
            services.AddSingleton(connectionMultiplexerMock.Object);
            services.AddScoped(sp => new WaitlistRedisConfiguration(connectionMultiplexerMock.Object));

            var sp = services.BuildServiceProvider();
            using var scope = sp.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();
            db.Database.EnsureCreated();
        });
    }
}