using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Inventory.Infrastructure.Persistence;
using Inventory.Domain.Ports;
using Moq;

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

            var sp = services.BuildServiceProvider();
            using var scope = sp.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();
            db.Database.EnsureCreated();
        });
    }
}
