using FluentAssertions;
using Inventory.Application.Commands;
using Inventory.Domain.Entities;
using Inventory.Domain.Enums;
using Inventory.Domain.Ports;
using Inventory.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Moq;
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
        
        _handler = new JoinWaitlistCommandHandler(
            _context,
            _waitlistRepoMock.Object,
            null!);
    }

    [Fact]
    public async Task Handle_NewUser_ShouldCreateWaitlistEntry()
    {
        var command = new JoinWaitlistCommand
        {
            UserId = Guid.NewGuid(),
            EventId = Guid.NewGuid(),
            Section = "A"
        };
        
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
        var command = new JoinWaitlistCommand
        {
            UserId = userId,
            EventId = eventId,
            Section = "A"
        };
        
        _waitlistRepoMock.Setup(r => r.ExistsAsync(userId, eventId, "A", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var act = () => _handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*already in waitlist*");
    }
}
