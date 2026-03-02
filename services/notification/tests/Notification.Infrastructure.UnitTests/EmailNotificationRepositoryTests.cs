using Xunit;
using Microsoft.EntityFrameworkCore;
using Notification.Infrastructure.Persistence;
using Notification.Domain.Entities;
using FluentAssertions;
using System;
using System.Threading.Tasks;

namespace Notification.Infrastructure.UnitTests.Persistence;

public class EmailNotificationRepositoryTests
{
    private NotificationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<NotificationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new NotificationDbContext(options);
    }

    [Fact]
    public async Task AddAsync_ShouldAddNotification()
    {
        // Arrange
        using var context = CreateContext();
        var repository = new EmailNotificationRepository(context);
        var notification = new EmailNotification 
        { 
            Id = Guid.NewGuid(), 
            OrderId = Guid.NewGuid(), 
            RecipientEmail = "test@example.com",
            Subject = "Test",
            Body = "Test"
        };

        // Act
        await repository.AddAsync(notification);
        await repository.SaveChangesAsync();

        // Assert
        var result = await context.EmailNotifications.FindAsync(notification.Id);
        result.Should().NotBeNull();
        result!.RecipientEmail.Should().Be("test@example.com");
    }

    [Fact]
    public async Task GetByOrderIdAsync_ShouldReturnNotification()
    {
        // Arrange
        using var context = CreateContext();
        var repository = new EmailNotificationRepository(context);
        var orderId = Guid.NewGuid();
        var notification = new EmailNotification 
        { 
            Id = Guid.NewGuid(), 
            OrderId = orderId, 
            RecipientEmail = "test@example.com",
            Subject = "Test",
            Body = "Test"
        };
        await context.EmailNotifications.AddAsync(notification);
        await context.SaveChangesAsync();

        // Act
        var result = await repository.GetByOrderIdAsync(orderId);

        // Assert
        result.Should().NotBeNull();
        result!.OrderId.Should().Be(orderId);
    }
}
