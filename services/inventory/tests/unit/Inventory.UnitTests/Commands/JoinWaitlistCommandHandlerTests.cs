using FluentAssertions;
using Inventory.Application.UseCases.JoinWaitlist;
using Inventory.Domain.Entities;
using Inventory.Domain.Enums;
using Inventory.Domain.Ports;
using Inventory.Infrastructure.Configuration;
using Inventory.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Moq;
using StackExchange.Redis;
using Xunit;

namespace Inventory.UnitTests.Commands;

public class JoinWaitlistCommandHandlerTests
{
    private readonly InventoryDbContext _context;
    private readonly Mock<IWaitlistRepository> _waitlistRepoMock;
    private readonly JoinWaitlistCommandHandler _handler;

    public JoinWaitlistCommandHandlerTests()
    {
        var options = new DbContextOptionsBuilder<InventoryDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        
        _context = new InventoryDbContext(options);
        _waitlistRepoMock = new Mock<IWaitlistRepository>();
        
        var databaseMock = new Mock<IDatabase>();
        databaseMock.Setup(x => x.SortedSetAddAsync(
            It.IsAny<RedisKey>(),
            It.IsAny<RedisValue>(),
            It.IsAny<double>(),
            It.IsAny<When>(),
            It.IsAny<CommandFlags>()))
            .ReturnsAsync(true);
        
        var connectionMock = new Mock<IConnectionMultiplexer>();
        connectionMock.Setup(x => x.GetDatabase(It.IsAny<int>(), It.IsAny<object>()))
            .Returns(databaseMock.Object);
        
        var redisConfig = new WaitlistRedisConfiguration(connectionMock.Object);
        
        _handler = new JoinWaitlistCommandHandler(
            _context,
            _waitlistRepoMock.Object,
            redisConfig);
    }

    [Fact]
    public async Task Handle_NewUser_ShouldCreateWaitlistEntry()
    {
        var command = new JoinWaitlistCommand(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "A"
        );
        
        _waitlistRepoMock.Setup(r => r.ExistsAsync(command.UserId, command.EventId, command.Section, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        
        _waitlistRepoMock.Setup(r => r.AddAsync(It.IsAny<WaitlistEntry>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((WaitlistEntry e, CancellationToken _) => e);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Should().NotBeNull();
        result.WaitlistEntryId.Should().NotBeEmpty();
        result.Status.Should().Be(WaitlistStatus.ACTIVE.ToString());
    }

    [Fact]
    public async Task Handle_ExistingUser_ShouldThrowException()
    {
        var userId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        var command = new JoinWaitlistCommand(
            userId,
            eventId,
            "A"
        );
        
        _waitlistRepoMock.Setup(r => r.ExistsAsync(userId, eventId, "A", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var act = () => _handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*already in waitlist*");
    }
}
