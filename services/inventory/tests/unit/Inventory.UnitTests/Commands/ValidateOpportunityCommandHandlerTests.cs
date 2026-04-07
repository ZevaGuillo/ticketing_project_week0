using FluentAssertions;
using Inventory.Application.UseCases.CreateReservation;
using Inventory.Domain.Entities;
using Inventory.Domain.Enums;
using Inventory.Domain.Ports;
using Inventory.Infrastructure.Configuration;
using Inventory.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace Inventory.UnitTests.Commands;

public class ValidateOpportunityCommandHandlerTests
{
    private readonly InventoryDbContext _context;
    private readonly Mock<IOpportunityWindowRepository> _opportunityWindowRepoMock;
    private readonly Mock<IReservationRepository> _reservationRepoMock;
    private readonly Mock<IKafkaProducer> _kafkaProducerMock;
    private readonly ValidateOpportunityCommandHandler _handler;

    public ValidateOpportunityCommandHandlerTests()
    {
        var options = new DbContextOptionsBuilder<InventoryDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        
        _context = new InventoryDbContext(options);
        _opportunityWindowRepoMock = new Mock<IOpportunityWindowRepository>();
        _reservationRepoMock = new Mock<IReservationRepository>();
        _kafkaProducerMock = new Mock<IKafkaProducer>();
        
        _handler = new ValidateOpportunityCommandHandler(
            _context,
            _opportunityWindowRepoMock.Object,
            _reservationRepoMock.Object,
            _kafkaProducerMock.Object,
            new ReservationSettings());
        
        _context.WaitlistEntries.Add(new WaitlistEntry
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            EventId = Guid.NewGuid(),
            Section = "A",
            Status = WaitlistStatus.OFFERED,
            JoinedAt = DateTime.UtcNow.AddMinutes(-30),
            CreatedAt = DateTime.UtcNow.AddMinutes(-30),
            UpdatedAt = DateTime.UtcNow.AddMinutes(-30)
        });
        _context.SaveChanges();
    }

    private void AddWaitlistEntryToContext(Guid id, Guid userId, Guid eventId, string section)
    {
        _context.WaitlistEntries.Add(new WaitlistEntry
        {
            Id = id,
            UserId = userId,
            EventId = eventId,
            Section = section,
            Status = WaitlistStatus.OFFERED,
            JoinedAt = DateTime.UtcNow.AddMinutes(-30),
            CreatedAt = DateTime.UtcNow.AddMinutes(-30),
            UpdatedAt = DateTime.UtcNow.AddMinutes(-30)
        });
        _context.SaveChanges();
    }

    private void AddSeatToContext(Guid seatId, string section)
    {
        _context.Seats.Add(new Seat
        {
            Id = seatId,
            Section = section,
            Row = "1",
            Number = 1,
            Reserved = false
        });
        _context.SaveChanges();
    }

    [Fact]
    public async Task Handle_WithValidToken_ShouldReturnOpportunityDetails()
    {
        var token = Guid.NewGuid().ToString("N");
        var opportunityId = Guid.NewGuid();
        var waitlistEntryId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        var seatId = Guid.NewGuid();
        
        var opportunity = new OpportunityWindow
        {
            Id = opportunityId,
            WaitlistEntryId = waitlistEntryId,
            SeatId = seatId,
            Token = token,
            Status = OpportunityStatus.OFFERED,
            StartsAt = DateTime.UtcNow.AddMinutes(-5),
            ExpiresAt = DateTime.UtcNow.AddMinutes(5)
        };

        AddWaitlistEntryToContext(waitlistEntryId, userId, eventId, "A");
        AddSeatToContext(seatId, "A");

        _opportunityWindowRepoMock.Setup(r => r.GetByTokenAsync(token, It.IsAny<CancellationToken>()))
            .ReturnsAsync(opportunity);

        _opportunityWindowRepoMock.Setup(r => r.UpdateAsync(It.IsAny<OpportunityWindow>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _reservationRepoMock.Setup(r => r.AddAsync(It.IsAny<Reservation>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Reservation r, CancellationToken _) => r);

        var command = new ValidateOpportunityCommand(token);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Should().NotBeNull();
        result.Token.Should().Be(token);
        result.UserId.Should().Be(userId);
        result.EventId.Should().Be(eventId);
        result.ExpiresAt.Should().BeAfter(DateTime.UtcNow);
    }

    [Fact]
    public async Task Handle_WithExpiredToken_ShouldThrowException()
    {
        var token = Guid.NewGuid().ToString("N");
        var waitlistEntryId = Guid.NewGuid();
        
        var opportunity = new OpportunityWindow
        {
            Id = Guid.NewGuid(),
            WaitlistEntryId = waitlistEntryId,
            SeatId = Guid.NewGuid(),
            Token = token,
            Status = OpportunityStatus.OFFERED,
            StartsAt = DateTime.UtcNow.AddMinutes(-15),
            ExpiresAt = DateTime.UtcNow.AddMinutes(-5)
        };

        AddWaitlistEntryToContext(waitlistEntryId, Guid.NewGuid(), Guid.NewGuid(), "A");

        _opportunityWindowRepoMock.Setup(r => r.GetByTokenAsync(token, It.IsAny<CancellationToken>()))
            .ReturnsAsync(opportunity);

        var command = new ValidateOpportunityCommand(token);

        var act = () => _handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*expired*");
    }

    [Fact]
    public async Task Handle_WithNonExistentToken_ShouldThrowException()
    {
        var token = Guid.NewGuid().ToString("N");
        
        _opportunityWindowRepoMock.Setup(r => r.GetByTokenAsync(token, It.IsAny<CancellationToken>()))
            .ReturnsAsync((OpportunityWindow?)null);

        var command = new ValidateOpportunityCommand(token);

        var act = () => _handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("*not found*");
    }

    [Fact]
    public async Task Handle_WithAlreadyUsedOpportunity_ShouldThrowException()
    {
        var token = Guid.NewGuid().ToString("N");
        var waitlistEntryId = Guid.NewGuid();
        
        var opportunity = new OpportunityWindow
        {
            Id = Guid.NewGuid(),
            WaitlistEntryId = waitlistEntryId,
            SeatId = Guid.NewGuid(),
            Token = token,
            Status = OpportunityStatus.USED,
            StartsAt = DateTime.UtcNow.AddMinutes(-5),
            ExpiresAt = DateTime.UtcNow.AddMinutes(10),
            UsedAt = DateTime.UtcNow
        };

        AddWaitlistEntryToContext(waitlistEntryId, Guid.NewGuid(), Guid.NewGuid(), "A");

        _opportunityWindowRepoMock.Setup(r => r.GetByTokenAsync(token, It.IsAny<CancellationToken>()))
            .ReturnsAsync(opportunity);

        var command = new ValidateOpportunityCommand(token);

        var act = () => _handler.Handle(command, CancellationToken.None);

        try
        {
            await act();
            Assert.Fail("Expected exception was not thrown");
        }
        catch (InvalidOperationException ex)
        {
            ex.Message.Should().Contain("already");
        }
    }

    [Fact]
    public async Task Handle_WithValidOpportunity_ShouldCreateReservationWith15MinTTL()
    {
        var token = Guid.NewGuid().ToString("N");
        var opportunityId = Guid.NewGuid();
        var waitlistEntryId = Guid.NewGuid();
        var seatId = Guid.NewGuid();
        
        var opportunity = new OpportunityWindow
        {
            Id = opportunityId,
            WaitlistEntryId = waitlistEntryId,
            SeatId = seatId,
            Token = token,
            Status = OpportunityStatus.OFFERED,
            StartsAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddMinutes(10)
        };

        AddWaitlistEntryToContext(waitlistEntryId, Guid.NewGuid(), Guid.NewGuid(), "A");
        AddSeatToContext(seatId, "A");

        Reservation? capturedReservation = null;

        _opportunityWindowRepoMock.Setup(r => r.GetByTokenAsync(token, It.IsAny<CancellationToken>()))
            .ReturnsAsync(opportunity);

        _opportunityWindowRepoMock.Setup(r => r.UpdateAsync(It.IsAny<OpportunityWindow>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _reservationRepoMock.Setup(r => r.AddAsync(It.IsAny<Reservation>(), It.IsAny<CancellationToken>()))
            .Callback<Reservation, CancellationToken>((r, _) => capturedReservation = r)
            .ReturnsAsync((Reservation r, CancellationToken _) => r);

        var command = new ValidateOpportunityCommand(token);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Should().NotBeNull();
        capturedReservation.Should().NotBeNull();
        var expectedExpiry = DateTime.UtcNow.AddMinutes(15);
        capturedReservation.ExpiresAt.Should().BeCloseTo(expectedExpiry, TimeSpan.FromSeconds(10));
    }

    [Fact]
    public async Task Handle_WithValidOpportunity_ShouldUpdateStatusToInProgress()
    {
        var token = Guid.NewGuid().ToString("N");
        var waitlistEntryId = Guid.NewGuid();
        var seatId = Guid.NewGuid();
        
        var opportunity = new OpportunityWindow
        {
            Id = Guid.NewGuid(),
            WaitlistEntryId = waitlistEntryId,
            SeatId = seatId,
            Token = token,
            Status = OpportunityStatus.OFFERED,
            StartsAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddMinutes(10)
        };

        AddWaitlistEntryToContext(waitlistEntryId, Guid.NewGuid(), Guid.NewGuid(), "A");
        AddSeatToContext(seatId, "A");

        OpportunityWindow? updatedWindow = null;

        _opportunityWindowRepoMock.Setup(r => r.GetByTokenAsync(token, It.IsAny<CancellationToken>()))
            .ReturnsAsync(opportunity);

        _opportunityWindowRepoMock.Setup(r => r.UpdateAsync(It.IsAny<OpportunityWindow>(), It.IsAny<CancellationToken>()))
            .Callback<OpportunityWindow, CancellationToken>((w, _) => updatedWindow = w)
            .Returns(Task.CompletedTask);

        _reservationRepoMock.Setup(r => r.AddAsync(It.IsAny<Reservation>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Reservation r, CancellationToken _) => r);

        var command = new ValidateOpportunityCommand(token);

        await _handler.Handle(command, CancellationToken.None);

        updatedWindow.Should().NotBeNull();
        updatedWindow.Status.Should().Be(OpportunityStatus.IN_PROGRESS);
    }
}
