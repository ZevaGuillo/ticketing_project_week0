using FluentAssertions;
using Inventory.Application.UseCases.ProcessWaitlistSelection;
using Inventory.Domain.Entities;
using Inventory.Domain.Enums;
using Inventory.Domain.Ports;
using Inventory.Infrastructure.Configuration;
using Inventory.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace Inventory.UnitTests.Services;

public class ProcessWaitlistSelectionServiceTests
{
    private readonly InventoryDbContext _context;
    private readonly Mock<IWaitlistRepository> _waitlistRepoMock;
    private readonly Mock<IOpportunityWindowRepository> _opportunityWindowRepoMock;
    private readonly Mock<IKafkaProducer> _kafkaProducerMock;
    private readonly ProcessWaitlistSelectionHandler _handler;

    public ProcessWaitlistSelectionServiceTests()
    {
        var options = new DbContextOptionsBuilder<InventoryDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        
        _context = new InventoryDbContext(options);
        _waitlistRepoMock = new Mock<IWaitlistRepository>();
        _opportunityWindowRepoMock = new Mock<IOpportunityWindowRepository>();
        _kafkaProducerMock = new Mock<IKafkaProducer>();
        
        _handler = new ProcessWaitlistSelectionHandler(
            _context,
            _waitlistRepoMock.Object,
            null,
            _opportunityWindowRepoMock.Object,
            _kafkaProducerMock.Object);
    }

    [Fact]
    public async Task Handle_WithAvailableWaitlistUser_ShouldSelectUserAndCreateOpportunity()
    {
        var userId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        var seatId = Guid.NewGuid();
        var reservationId = Guid.NewGuid();
        var section = "A";
        var expiredAt = DateTime.UtcNow;

        var waitlistEntry = new WaitlistEntry
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            EventId = eventId,
            Section = section,
            Status = WaitlistStatus.ACTIVE,
            JoinedAt = DateTime.UtcNow.AddMinutes(-30)
        };

        _waitlistRepoMock.Setup(r => r.GetByUserEventSectionAsync(userId, eventId, section, It.IsAny<CancellationToken>()))
            .ReturnsAsync(waitlistEntry);

        _waitlistRepoMock.Setup(r => r.GetActiveByEventAndSectionAsync(eventId, section, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<WaitlistEntry> { waitlistEntry });

        _waitlistRepoMock.Setup(r => r.UpdateAsync(It.IsAny<WaitlistEntry>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _opportunityWindowRepoMock.Setup(r => r.AddAsync(It.IsAny<OpportunityWindow>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((OpportunityWindow w, CancellationToken _) => w);

        _kafkaProducerMock.Setup(p => p.ProduceAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        var command = new ProcessWaitlistSelectionCommand(
            reservationId,
            seatId,
            eventId,
            section,
            expiredAt);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Should().NotBeNull();
        result.UserId.Should().Be(userId);
        result.WaitlistEntryId.Should().Be(waitlistEntry.Id);
        result.Token.Should().NotBeEmpty();
        result.ExpiresAt.Should().BeCloseTo(DateTime.UtcNow.AddMinutes(10), TimeSpan.FromSeconds(5));

        _waitlistRepoMock.Verify(r => r.UpdateAsync(
            It.Is<WaitlistEntry>(e => e.Status == WaitlistStatus.OFFERED),
            It.IsAny<CancellationToken>()), Times.Once);

        _opportunityWindowRepoMock.Verify(r => r.AddAsync(
            It.Is<OpportunityWindow>(w => w.Status == OpportunityStatus.OFFERED),
            It.IsAny<CancellationToken>()), Times.Once);

        _kafkaProducerMock.Verify(p => p.ProduceAsync(
            "waitlist.opportunity-granted",
            It.IsAny<string>(),
            userId.ToString()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenNoUsersInWaitlist_ShouldReturnNull()
    {
        var eventId = Guid.NewGuid();
        var seatId = Guid.NewGuid();
        var reservationId = Guid.NewGuid();
        var section = "A";
        var expiredAt = DateTime.UtcNow;

        _waitlistRepoMock.Setup(r => r.GetActiveByEventAndSectionAsync(eventId, section, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<WaitlistEntry>());

        var command = new ProcessWaitlistSelectionCommand(
            reservationId,
            seatId,
            eventId,
            section,
            expiredAt);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task Handle_WhenOpportunityCreated_ShouldSet10MinuteTTL()
    {
        var userId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        var seatId = Guid.NewGuid();
        var reservationId = Guid.NewGuid();
        var section = "A";
        var expiredAt = DateTime.UtcNow;

        var waitlistEntry = new WaitlistEntry
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            EventId = eventId,
            Section = section,
            Status = WaitlistStatus.ACTIVE,
            JoinedAt = DateTime.UtcNow.AddMinutes(-30)
        };

        OpportunityWindow? capturedWindow = null;

        _waitlistRepoMock.Setup(r => r.GetByUserEventSectionAsync(userId, eventId, section, It.IsAny<CancellationToken>()))
            .ReturnsAsync(waitlistEntry);

        _waitlistRepoMock.Setup(r => r.GetActiveByEventAndSectionAsync(eventId, section, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<WaitlistEntry> { waitlistEntry });

        _waitlistRepoMock.Setup(r => r.UpdateAsync(It.IsAny<WaitlistEntry>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _opportunityWindowRepoMock.Setup(r => r.AddAsync(It.IsAny<OpportunityWindow>(), It.IsAny<CancellationToken>()))
            .Callback<OpportunityWindow, CancellationToken>((w, _) => capturedWindow = w)
            .ReturnsAsync((OpportunityWindow w, CancellationToken _) => w);

        _kafkaProducerMock.Setup(p => p.ProduceAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        var command = new ProcessWaitlistSelectionCommand(
            reservationId,
            seatId,
            eventId,
            section,
            expiredAt);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Should().NotBeNull();
        capturedWindow.Should().NotBeNull();
        capturedWindow.ExpiresAt.Should().BeCloseTo(capturedWindow.StartsAt.AddMinutes(10), TimeSpan.FromSeconds(5));
    }
}
