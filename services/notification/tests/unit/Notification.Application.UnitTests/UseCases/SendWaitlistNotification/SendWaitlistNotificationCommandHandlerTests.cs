using Microsoft.Extensions.Logging;
using Moq;
using Notification.Application.Ports;
using Notification.Application.UseCases.SendWaitlistNotification;
using Notification.Domain.Entities;

namespace Notification.Application.UnitTests.UseCases.SendWaitlistNotification;

public class SendWaitlistNotificationCommandHandlerTests
{
    private readonly Mock<IEmailNotificationRepository> _repositoryMock;
    private readonly Mock<IEmailService> _emailServiceMock;
    private readonly Mock<ILogger<SendWaitlistNotificationCommandHandler>> _loggerMock;
    private readonly SendWaitlistNotificationCommandHandler _handler;

    public SendWaitlistNotificationCommandHandlerTests()
    {
        _repositoryMock = new Mock<IEmailNotificationRepository>();
        _emailServiceMock = new Mock<IEmailService>();
        _loggerMock = new Mock<ILogger<SendWaitlistNotificationCommandHandler>>();
        _handler = new SendWaitlistNotificationCommandHandler(
            _repositoryMock.Object, 
            _emailServiceMock.Object, 
            _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_WithValidCommand_ShouldSendEmailAndPersistNotification()
    {
        var opportunityId = Guid.NewGuid();
        var email = "user@example.com";
        var command = new SendWaitlistNotificationCommand(
            OpportunityId: opportunityId,
            WaitlistEntryId: Guid.NewGuid(),
            UserId: Guid.NewGuid(),
            RecipientEmail: email,
            EventName: "Concert 2026",
            Section: "VIP",
            OpportunityTTL: 600,
            CreatedAt: DateTime.UtcNow);

        _repositoryMock
            .Setup(r => r.GetByOpportunityIdAsync(opportunityId))
            .ReturnsAsync((EmailNotification?)null);

        _emailServiceMock
            .Setup(e => e.SendAsync(
                It.IsAny<string>(), 
                It.IsAny<string>(), 
                It.IsAny<string>(), 
                It.IsAny<string?>(), 
                It.IsAny<byte[]?>()))
            .ReturnsAsync(true);

        _repositoryMock
            .Setup(r => r.AddAsync(It.IsAny<EmailNotification>()))
            .ReturnsAsync((EmailNotification n) => n);

        _repositoryMock
            .Setup(r => r.SaveChangesAsync())
            .ReturnsAsync(true);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Message.Should().Contain("successfully");
        result.NotificationId.Should().NotBeEmpty();
        
        _emailServiceMock.Verify(
            e => e.SendAsync(email, It.IsAny<string>(), It.IsAny<string>(), null, null),
            Times.Once);
        _repositoryMock.Verify(r => r.AddAsync(It.IsAny<EmailNotification>()), Times.Once);
        _repositoryMock.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task Handle_WithExistingNotification_ShouldReturnIdempotentResult()
    {
        var opportunityId = Guid.NewGuid();
        var existingId = Guid.NewGuid();
        var command = new SendWaitlistNotificationCommand(
            OpportunityId: opportunityId,
            WaitlistEntryId: Guid.NewGuid(),
            UserId: Guid.NewGuid(),
            RecipientEmail: "user@example.com",
            EventName: "Concert",
            Section: "A",
            OpportunityTTL: 600,
            CreatedAt: DateTime.UtcNow);

        var existing = new EmailNotification
        {
            Id = existingId,
            OpportunityId = opportunityId,
            Status = NotificationStatus.Sent
        };

        _repositoryMock
            .Setup(r => r.GetByOpportunityIdAsync(opportunityId))
            .ReturnsAsync(existing);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Success.Should().BeTrue();
        result.NotificationId.Should().Be(existingId);
        result.Message.Should().Contain("already");
        
        _emailServiceMock.Verify(
            e => e.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), null, null), 
            Times.Never);
    }

    [Fact]
    public async Task Handle_WhenEmailFails_ShouldPersistWithFailedStatus()
    {
        var opportunityId = Guid.NewGuid();
        var email = "user@example.com";
        var command = new SendWaitlistNotificationCommand(
            OpportunityId: opportunityId,
            WaitlistEntryId: Guid.NewGuid(),
            UserId: Guid.NewGuid(),
            RecipientEmail: email,
            EventName: "Concert",
            Section: "A",
            OpportunityTTL: 600,
            CreatedAt: DateTime.UtcNow);

        _repositoryMock
            .Setup(r => r.GetByOpportunityIdAsync(opportunityId))
            .ReturnsAsync((EmailNotification?)null);

        _emailServiceMock
            .Setup(e => e.SendAsync(
                It.IsAny<string>(), 
                It.IsAny<string>(), 
                It.IsAny<string>(), 
                It.IsAny<string?>(), 
                It.IsAny<byte[]?>()))
            .ReturnsAsync(false);

        EmailNotification? captured = null;
        _repositoryMock
            .Setup(r => r.AddAsync(It.IsAny<EmailNotification>()))
            .Callback<EmailNotification>(n => captured = n)
            .ReturnsAsync((EmailNotification n) => n);

        _repositoryMock
            .Setup(r => r.SaveChangesAsync())
            .ReturnsAsync(true);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Message.Should().Contain("queued");
        
        captured.Should().NotBeNull();
        captured!.Status.Should().Be(NotificationStatus.Failed);
        captured.FailureReason.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Handle_WithException_ShouldReturnFailureResult()
    {
        var opportunityId = Guid.NewGuid();
        var command = new SendWaitlistNotificationCommand(
            OpportunityId: opportunityId,
            WaitlistEntryId: Guid.NewGuid(),
            UserId: Guid.NewGuid(),
            RecipientEmail: "user@example.com",
            EventName: "Concert",
            Section: "A",
            OpportunityTTL: 600,
            CreatedAt: DateTime.UtcNow);

        _repositoryMock
            .Setup(r => r.GetByOpportunityIdAsync(opportunityId))
            .ThrowsAsync(new InvalidOperationException("DB error"));

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("Error");
    }
}