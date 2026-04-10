using FluentAssertions;
using Inventory.Application.Handlers;
using Inventory.Domain.Events;
using Inventory.Domain.Ports;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Inventory.UnitTests.Handlers;

public class WaitlistNotificationHandlerTests
{
    private readonly Mock<INotificationService> _notificationServiceMock;
    private readonly Mock<IUserService> _userServiceMock;
    private readonly Mock<ILogger<WaitlistNotificationHandler>> _loggerMock;
    private readonly WaitlistNotificationHandler _handler;

    public WaitlistNotificationHandlerTests()
    {
        _notificationServiceMock = new Mock<INotificationService>();
        _userServiceMock = new Mock<IUserService>();
        _loggerMock = new Mock<ILogger<WaitlistNotificationHandler>>();
        _handler = new WaitlistNotificationHandler(
            _notificationServiceMock.Object, 
            _userServiceMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_WithValidUserAndActiveAccount_ShouldSendEmailNotification()
    {
        var userId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        var opportunityId = Guid.NewGuid();
        
        var waitlistEvent = new WaitlistOpportunityGrantedEvent
        {
            OpportunityId = opportunityId,
            WaitlistEntryId = Guid.NewGuid(),
            UserId = userId,
            EventId = eventId,
            SeatId = Guid.NewGuid(),
            Section = "A",
            OpportunityTTL = 600,
            IdempotencyKey = "test-key",
            CreatedAt = DateTime.UtcNow
        };

        var user = new UserInfo
        {
            Id = userId,
            Email = "test@example.com",
            IsActive = true,
            IsEmailVerified = true,
            FullName = "Test User"
        };

        _userServiceMock.Setup(s => s.GetUserByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _notificationServiceMock.Setup(s => s.SendEmailAsync(It.IsAny<EmailMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        await _handler.Handle(waitlistEvent, CancellationToken.None);

        _notificationServiceMock.Verify(
            s => s.SendEmailAsync(It.Is<EmailMessage>(e => e.To == user.Email), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithInactiveUser_ShouldNotSendNotification()
    {
        var userId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        
        var waitlistEvent = new WaitlistOpportunityGrantedEvent
        {
            OpportunityId = Guid.NewGuid(),
            WaitlistEntryId = Guid.NewGuid(),
            UserId = userId,
            EventId = eventId,
            SeatId = Guid.NewGuid(),
            Section = "A",
            OpportunityTTL = 600,
            IdempotencyKey = "test-key",
            CreatedAt = DateTime.UtcNow
        };

        var user = new UserInfo
        {
            Id = userId,
            Email = "test@example.com",
            IsActive = false,
            IsEmailVerified = true,
            FullName = "Test User"
        };

        _userServiceMock.Setup(s => s.GetUserByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        await _handler.Handle(waitlistEvent, CancellationToken.None);

        _notificationServiceMock.Verify(
            s => s.SendEmailAsync(It.IsAny<EmailMessage>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WithUnverifiedEmail_ShouldNotSendNotification()
    {
        var userId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        
        var waitlistEvent = new WaitlistOpportunityGrantedEvent
        {
            OpportunityId = Guid.NewGuid(),
            WaitlistEntryId = Guid.NewGuid(),
            UserId = userId,
            EventId = eventId,
            SeatId = Guid.NewGuid(),
            Section = "A",
            OpportunityTTL = 600,
            IdempotencyKey = "test-key",
            CreatedAt = DateTime.UtcNow
        };

        var user = new UserInfo
        {
            Id = userId,
            Email = "test@example.com",
            IsActive = true,
            IsEmailVerified = false,
            FullName = "Test User"
        };

        _userServiceMock.Setup(s => s.GetUserByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        await _handler.Handle(waitlistEvent, CancellationToken.None);

        _notificationServiceMock.Verify(
            s => s.SendEmailAsync(It.IsAny<EmailMessage>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WithRetryOnFailure_ShouldRetryWithExponentialBackoff()
    {
        var userId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        
        var waitlistEvent = new WaitlistOpportunityGrantedEvent
        {
            OpportunityId = Guid.NewGuid(),
            WaitlistEntryId = Guid.NewGuid(),
            UserId = userId,
            EventId = eventId,
            SeatId = Guid.NewGuid(),
            Section = "A",
            OpportunityTTL = 600,
            IdempotencyKey = "test-key",
            CreatedAt = DateTime.UtcNow
        };

        var user = new UserInfo
        {
            Id = userId,
            Email = "test@example.com",
            IsActive = true,
            IsEmailVerified = true,
            FullName = "Test User"
        };

        var attemptCount = 0;
        _userServiceMock.Setup(s => s.GetUserByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _notificationServiceMock.Setup(s => s.SendEmailAsync(It.IsAny<EmailMessage>(), It.IsAny<CancellationToken>()))
            .Returns(() => 
            {
                attemptCount++;
                if (attemptCount < 3)
                    throw new InvalidOperationException("Simulated failure");
                return Task.FromResult(true);
            });

        await _handler.Handle(waitlistEvent, CancellationToken.None);

        _notificationServiceMock.Verify(
            s => s.SendEmailAsync(It.IsAny<EmailMessage>(), It.IsAny<CancellationToken>()),
            Times.Exactly(3));
    }

    [Fact]
    public async Task Handle_WithNonExistentUser_ShouldNotThrow()
    {
        var userId = Guid.NewGuid();
        
        var waitlistEvent = new WaitlistOpportunityGrantedEvent
        {
            OpportunityId = Guid.NewGuid(),
            WaitlistEntryId = Guid.NewGuid(),
            UserId = userId,
            EventId = Guid.NewGuid(),
            SeatId = Guid.NewGuid(),
            Section = "A",
            OpportunityTTL = 600,
            IdempotencyKey = "test-key",
            CreatedAt = DateTime.UtcNow
        };

        _userServiceMock.Setup(s => s.GetUserByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserInfo?)null);

        var act = () => _handler.Handle(waitlistEvent, CancellationToken.None);

        await act.Should().NotThrowAsync();
        
        _notificationServiceMock.Verify(
            s => s.SendEmailAsync(It.IsAny<EmailMessage>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WithValidEvent_ShouldComposeEmailWithOpportunityDetails()
    {
        var userId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        var opportunityId = Guid.NewGuid();
        var seatId = Guid.NewGuid();
        
        var waitlistEvent = new WaitlistOpportunityGrantedEvent
        {
            OpportunityId = opportunityId,
            WaitlistEntryId = Guid.NewGuid(),
            UserId = userId,
            EventId = eventId,
            SeatId = seatId,
            Section = "A",
            OpportunityTTL = 600,
            IdempotencyKey = "test-key",
            CreatedAt = DateTime.UtcNow
        };

        var user = new UserInfo
        {
            Id = userId,
            Email = "test@example.com",
            IsActive = true,
            IsEmailVerified = true,
            FullName = "Test User"
        };

        _userServiceMock.Setup(s => s.GetUserByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _notificationServiceMock.Setup(s => s.SendEmailAsync(It.IsAny<EmailMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        EmailMessage? capturedMessage = null;
        _notificationServiceMock.Setup(s => s.SendEmailAsync(It.IsAny<EmailMessage>(), It.IsAny<CancellationToken>()))
            .Callback<EmailMessage, CancellationToken>((m, _) => capturedMessage = m)
            .ReturnsAsync(true);

        await _handler.Handle(waitlistEvent, CancellationToken.None);

        capturedMessage.Should().NotBeNull();
        capturedMessage.Subject.Should().Contain("oportunidad");
        capturedMessage.Body.Should().Contain(opportunityId.ToString());
    }
}
