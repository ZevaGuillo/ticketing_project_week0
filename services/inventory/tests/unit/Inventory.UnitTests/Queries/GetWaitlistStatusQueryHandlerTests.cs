using FluentAssertions;
using Inventory.Application.UseCases.GetWaitlistStatus;
using Inventory.Domain.Entities;
using Inventory.Domain.Enums;
using Inventory.Domain.Ports;
using Inventory.Infrastructure.Configuration;
using Moq;
using StackExchange.Redis;
using Xunit;

namespace Inventory.UnitTests.Queries;

public class GetWaitlistStatusQueryHandlerTests
{
    private readonly Mock<IWaitlistRepository> _waitlistRepoMock;
    private readonly GetWaitlistStatusQueryHandler _handler;

    public GetWaitlistStatusQueryHandlerTests()
    {
        _waitlistRepoMock = new Mock<IWaitlistRepository>();
        
        var databaseMock = new Mock<IDatabase>();
        databaseMock.Setup(x => x.SortedSetRankAsync(
            It.IsAny<RedisKey>(),
            It.IsAny<RedisValue>(),
            It.IsAny<Order>(),
            It.IsAny<CommandFlags>()))
            .ReturnsAsync((long?)1);
        
        var connectionMock = new Mock<IConnectionMultiplexer>();
        connectionMock.Setup(x => x.GetDatabase(It.IsAny<int>(), It.IsAny<object>()))
            .Returns(databaseMock.Object);
        
        var redisConfig = new WaitlistRedisConfiguration(connectionMock.Object);
        
        _handler = new GetWaitlistStatusQueryHandler(
            _waitlistRepoMock.Object,
            redisConfig);
    }

    [Fact]
    public async Task Handle_ExistingUser_ShouldReturnStatus()
    {
        var userId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        
        var entry = new WaitlistEntry
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            EventId = eventId,
            Section = "A",
            Status = WaitlistStatus.ACTIVE,
            JoinedAt = DateTime.UtcNow.AddDays(-1)
        };
        
        _waitlistRepoMock.Setup(r => r.GetByUserEventSectionAsync(userId, eventId, "A", It.IsAny<CancellationToken>()))
            .ReturnsAsync(entry);

        var query = new GetWaitlistStatusQuery(userId, eventId, "A");

        var result = await _handler.Handle(query, CancellationToken.None);

        result.Should().NotBeNull();
        result!.WaitlistEntryId.Should().Be(entry.Id);
        result.Status.Should().Be(WaitlistStatus.ACTIVE.ToString());
    }

    [Fact]
    public async Task Handle_NewUser_ShouldReturnNull()
    {
        var userId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        
        _waitlistRepoMock.Setup(r => r.GetByUserEventSectionAsync(userId, eventId, "A", It.IsAny<CancellationToken>()))
            .ReturnsAsync((WaitlistEntry?)null);

        var query = new GetWaitlistStatusQuery(userId, eventId, "A");

        var result = await _handler.Handle(query, CancellationToken.None);

        result.Should().BeNull();
    }
}
