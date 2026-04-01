using FluentAssertions;
using Inventory.Application.Queries;
using Inventory.Domain.Entities;
using Inventory.Domain.Enums;
using Inventory.Domain.Ports;
using Moq;
using Xunit;

namespace Inventory.UnitTests.Queries;

public class GetWaitlistStatusQueryHandlerTests
{
    private readonly Mock<IWaitlistRepository> _waitlistRepoMock;
    private readonly GetWaitlistStatusQueryHandler _handler;

    public GetWaitlistStatusQueryHandlerTests()
    {
        _waitlistRepoMock = new Mock<IWaitlistRepository>();
        
        _handler = new GetWaitlistStatusQueryHandler(
            _waitlistRepoMock.Object,
            null!);
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

        var query = new GetWaitlistStatusQuery
        {
            UserId = userId,
            EventId = eventId,
            Section = "A"
        };

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

        var query = new GetWaitlistStatusQuery
        {
            UserId = userId,
            EventId = eventId,
            Section = "A"
        };

        var result = await _handler.Handle(query, CancellationToken.None);

        result.Should().BeNull();
    }
}
