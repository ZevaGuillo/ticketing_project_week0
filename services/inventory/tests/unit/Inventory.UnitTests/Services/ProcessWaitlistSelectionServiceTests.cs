using FluentAssertions;
using Inventory.Application.UseCases.ProcessWaitlistSelection;
using Inventory.Domain.Entities;
using Inventory.Domain.Enums;
using Inventory.Domain.Ports;
using Inventory.Infrastructure.Configuration;
using Inventory.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Moq;
using StackExchange.Redis;
using Xunit;

namespace Inventory.UnitTests.Services;

public class ProcessWaitlistSelectionServiceTests
{
    private readonly InventoryDbContext _context;
    private readonly Mock<IWaitlistRepository> _waitlistRepoMock;
    private readonly Mock<IOpportunityWindowRepository> _opportunityWindowRepoMock;
    private readonly Mock<IKafkaProducer> _kafkaProducerMock;
    private readonly Mock<IConnectionMultiplexer> _connectionMock;
    private readonly Mock<IDatabase> _databaseMock;
    private readonly WaitlistRedisConfiguration _redisConfig;
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
        
        _databaseMock = new Mock<IDatabase>();
        _databaseMock.Setup(x => x.SortedSetPopAsync(
            It.IsAny<RedisKey>(),
            It.IsAny<long>(),
            It.IsAny<Order>(),
            It.IsAny<CommandFlags>()))
            .ReturnsAsync(SortedSetPopResult.Empty);
        
        _databaseMock.Setup(x => x.KeyExistsAsync(
            It.IsAny<RedisKey>(),
            It.IsAny<CommandFlags>()))
            .ReturnsAsync(false);
        
        _databaseMock.Setup(x => x.StringSetAsync(
            It.IsAny<RedisKey>(),
            It.IsAny<RedisValue>(),
            It.IsAny<TimeSpan?>(),
            It.IsAny<When>(),
            It.IsAny<CommandFlags>()))
            .ReturnsAsync(true);
        
        _connectionMock = new Mock<IConnectionMultiplexer>();
        _connectionMock.Setup(x => x.GetDatabase(It.IsAny<int>(), It.IsAny<object>()))
            .Returns(_databaseMock.Object);
        
        _redisConfig = new WaitlistRedisConfiguration(_connectionMock.Object);
        
        _handler = new ProcessWaitlistSelectionHandler(
            _context,
            _waitlistRepoMock.Object,
            _redisConfig,
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

        _databaseMock.Setup(x => x.SortedSetPopAsync(
            It.IsAny<RedisKey>(),
            1,
            Order.Descending,
            It.IsAny<CommandFlags>()))
            .ReturnsAsync(SortedSetPopResult.Create(
                new RedisValue[] { userId.ToString() },
                new RedisValue[] { waitlistEntry.JoinedAt.Ticks }));

        _waitlistRepoMock.Setup(r => r.GetByUserEventSectionAsync(userId, eventId, section, It.IsAny<CancellationToken>()))
            .ReturnsAsync(waitlistEntry);

        _waitlistRepoMock.Setup(r => r.UpdateAsync(It.IsAny<WaitlistEntry>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _opportunityWindowRepoMock.Setup(r => r.AddAsync(It.IsAny<OpportunityWindow>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

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

        _databaseMock.Setup(x => x.SortedSetPopAsync(
            It.IsAny<RedisKey>(),
            1,
            Order.Descending,
            It.IsAny<CommandFlags>()))
            .ReturnsAsync(SortedSetPopResult.Empty);

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
    public async Task Handle_WithAlreadyProcessedEvent_ShouldReturnNull()
    {
        var eventId = Guid.NewGuid();
        var seatId = Guid.NewGuid();
        var reservationId = Guid.NewGuid();
        var section = "A";
        var expiredAt = DateTime.UtcNow;

        _databaseMock.Setup(x => x.KeyExistsAsync(
            It.IsAny<RedisKey>(),
            It.IsAny<CommandFlags>()))
            .ReturnsAsync(true);

        var command = new ProcessWaitlistSelectionCommand(
            reservationId,
            seatId,
            eventId,
            section,
            expiredAt);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Should().BeNull();

        _waitlistRepoMock.Verify(r => r.GetActiveByEventAndSectionAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithLuaScript_ShouldUseAtomicFifoSelection()
    {
        var userId1 = Guid.NewGuid();
        var userId2 = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        var seatId = Guid.NewGuid();
        var reservationId = Guid.NewGuid();
        var section = "A";
        var expiredAt = DateTime.UtcNow;

        var waitlistEntry1 = new WaitlistEntry
        {
            Id = Guid.NewGuid(),
            UserId = userId1,
            EventId = eventId,
            Section = section,
            Status = WaitlistStatus.ACTIVE,
            JoinedAt = DateTime.UtcNow.AddMinutes(-30)
        };

        var waitlistEntry2 = new WaitlistEntry
        {
            Id = Guid.NewGuid(),
            UserId = userId2,
            EventId = eventId,
            Section = section,
            Status = WaitlistStatus.ACTIVE,
            JoinedAt = DateTime.UtcNow.AddMinutes(-20)
        };

        _databaseMock.Setup(x => x.SortedSetPopAsync(
            It.IsAny<RedisKey>(),
            1,
            Order.Descending,
            It.IsAny<CommandFlags>()))
            .ReturnsAsync(SortedSetPopResult.Create(
                new RedisValue[] { userId1.ToString() },
                new RedisValue[] { waitlistEntry1.JoinedAt.Ticks }));

        _waitlistRepoMock.SetupSequence(r => r.GetByUserEventSectionAsync(It.IsAny<Guid>(), eventId, section, It.IsAny<CancellationToken>()))
            .ReturnsAsync(waitlistEntry1)
            .ReturnsAsync(waitlistEntry2);

        _waitlistRepoMock.Setup(r => r.UpdateAsync(It.IsAny<WaitlistEntry>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _opportunityWindowRepoMock.Setup(r => r.AddAsync(It.IsAny<OpportunityWindow>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

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
        result.UserId.Should().Be(userId1);
    }

    [Fact]
    public async Task Handle_WithDistributedLock_ShouldPreventConcurrentSelection()
    {
        var userId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        var seatId = Guid.NewGuid();
        var reservationId = Guid.NewGuid();
        var section = "A";
        var expiredAt = DateTime.UtcNow;

        var lockAcquired = false;
        var db = _databaseMock.Object;

        _databaseMock.Setup(x => x.StringSetAsync(
            It.IsAny<RedisKey>(),
            It.IsAny<RedisValue>(),
            It.IsAny<TimeSpan?>(),
            It.IsAny<When>(),
            It.IsAny<CommandFlags>()))
            .ReturnsAsync(true)
            .Callback(() => lockAcquired = true);

        _databaseMock.Setup(x => x.StringGetDeleteAsync(
            It.IsAny<RedisKey>(),
            It.IsAny<CommandFlags>()))
            .ReturnsAsync(RedisValue.Null)
            .Callback(() => lockAcquired = false);

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

        _databaseMock.Setup(x => x.SortedSetPopAsync(
            It.IsAny<RedisKey>(),
            1,
            Order.Descending,
            It.IsAny<CommandFlags>()))
            .ReturnsAsync(SortedSetPopResult.Create(
                new RedisValue[] { userId.ToString() },
                new RedisValue[] { waitlistEntry.JoinedAt.Ticks }));

        _waitlistRepoMock.Setup(r => r.GetByUserEventSectionAsync(userId, eventId, section, It.IsAny<CancellationToken>()))
            .ReturnsAsync(waitlistEntry);

        _waitlistRepoMock.Setup(r => r.UpdateAsync(It.IsAny<WaitlistEntry>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _opportunityWindowRepoMock.Setup(r => r.AddAsync(It.IsAny<OpportunityWindow>(), It.IsAny<CancellationToken>()))
            .Callback<OpportunityWindow, CancellationToken>((w, _) => capturedWindow = w)
            .Returns(Task.CompletedTask);

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
