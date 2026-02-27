using Catalog.Domain.Entities;
using FluentAssertions;

namespace Catalog.Domain.UnitTests;

public class EventTests
{
    #region Event.Create() Tests - Following Gherkin Scenarios (T101)

    [Fact]
    public void Event_Create_WithValidParameters_ShouldCreateEventSuccessfully()
    {
        // Arrange - Following Gherkin: "Crear evento válido"
        var eventName = "Concierto Foo Fighters 2026";
        var description = "Concierto en el Estadio Nacional";
        var eventDate = DateTime.Parse("2026-06-15T20:00:00Z").ToUniversalTime();
        var venue = "Estadio Nacional";
        var maxCapacity = 50000;
        var basePrice = 120.50m;

        // Act
        var eventEntity = Event.Create(eventName, description, eventDate, venue, maxCapacity, basePrice);

        // Assert - Following Gherkin expectations
        eventEntity.Should().NotBeNull();
        eventEntity.Id.Should().NotBeEmpty(); // "ID generado automáticamente"
        eventEntity.Name.Should().Be(eventName);
        eventEntity.Description.Should().Be(description);
        eventEntity.EventDate.Should().Be(eventDate);
        eventEntity.Venue.Should().Be(venue);
        eventEntity.MaxCapacity.Should().Be(maxCapacity);
        eventEntity.BasePrice.Should().Be(basePrice);
        eventEntity.Status.Should().Be("active"); // "está en estado 'active'"
        eventEntity.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        eventEntity.Seats.Should().NotBeNull().And.BeEmpty();
        eventEntity.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Event_Create_WithEmptyName_ShouldThrowArgumentException()
    {
        // Arrange - Following Gherkin: "Crear evento con datos inválidos - nombre vacío"
        var eventName = ""; // Empty name
        var description = "Evento sin nombre";
        var eventDate = DateTime.Parse("2026-06-15T20:00:00Z").ToUniversalTime();
        var venue = "Test Venue";
        var maxCapacity = 1000;
        var basePrice = 120.50m;

        // Act & Assert - Following Gherkin: "error de validación 'El nombre del evento es obligatorio'"
        var exception = Assert.Throws<ArgumentException>(() =>
            Event.Create(eventName, description, eventDate, venue, maxCapacity, basePrice));
        
        exception.Message.Should().Contain("El nombre del evento es obligatorio");
        exception.ParamName.Should().Be("name");
    }

    [Fact]
    public void Event_Create_WithWhiteSpaceName_ShouldThrowArgumentException()
    {
        // Arrange
        var eventName = "   "; // White space only
        var description = "Evento válido";
        var eventDate = DateTime.UtcNow.AddMonths(2);
        var venue = "Test Venue";
        var maxCapacity = 1000;
        var basePrice = 50.00m;

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            Event.Create(eventName, description, eventDate, venue, maxCapacity, basePrice));
        
        exception.Message.Should().Contain("El nombre del evento es obligatorio");
    }

    [Fact]
    public void Event_Create_WithPastDate_ShouldThrowArgumentException()
    {
        // Arrange - Following Gherkin: "Crear evento con fecha pasada"
        var eventName = "Evento Pasado";
        var description = "Evento en el pasado";
        var eventDate = DateTime.Parse("2020-01-01T20:00:00Z").ToUniversalTime(); // Past date
        var venue = "Test Venue";
        var maxCapacity = 1000;
        var basePrice = 50.00m;

        // Act & Assert - Following Gherkin: "error de validación 'La fecha del evento debe ser futura'"
        var exception = Assert.Throws<ArgumentException>(() =>
            Event.Create(eventName, description, eventDate, venue, maxCapacity, basePrice));
        
        exception.Message.Should().Contain("La fecha del evento debe ser futura");
        exception.ParamName.Should().Be("eventDate");
    }

    [Fact]
    public void Event_Create_WithNegativePrice_ShouldThrowArgumentException()
    {
        // Arrange - Following Gherkin: "Crear evento con precio negativo"
        var eventName = "Evento Gratis";
        var description = "Evento con precio inválido";
        var eventDate = DateTime.Parse("2026-06-15T20:00:00Z").ToUniversalTime();
        var venue = "Test Venue";
        var maxCapacity = 1000;
        var basePrice = -10.00m; // Negative price

        // Act & Assert - Following Gherkin: "error de validación 'El precio base debe ser mayor a cero'"
        var exception = Assert.Throws<ArgumentException>(() =>
            Event.Create(eventName, description, eventDate, venue, maxCapacity, basePrice));
        
        exception.Message.Should().Contain("El precio base debe ser mayor a cero");
        exception.ParamName.Should().Be("basePrice");
    }

    [Fact]
    public void Event_Create_WithZeroPrice_ShouldThrowArgumentException()
    {
        // Arrange
        var eventName = "Evento Gratis";
        var description = "Evento con precio cero";
        var eventDate = DateTime.UtcNow.AddMonths(2);
        var venue = "Test Venue";
        var maxCapacity = 1000;
        var basePrice = 0.00m; // Zero price

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            Event.Create(eventName, description, eventDate, venue, maxCapacity, basePrice));
        
        exception.Message.Should().Contain("El precio base debe ser mayor a cero");
    }

    [Fact]
    public void Event_Create_WithEmptyDescription_ShouldThrowArgumentException()
    {
        // Arrange
        var eventName = "Evento Válido";
        var description = ""; // Empty description
        var eventDate = DateTime.UtcNow.AddMonths(2);
        var venue = "Test Venue";
        var maxCapacity = 1000;
        var basePrice = 50.00m;

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            Event.Create(eventName, description, eventDate, venue, maxCapacity, basePrice));
        
        exception.Message.Should().Contain("La descripción del evento es obligatoria");
        exception.ParamName.Should().Be("description");
    }

    [Fact]
    public void Event_Create_WithEmptyVenue_ShouldThrowArgumentException()
    {
        // Arrange
        var eventName = "Evento Válido";
        var description = "Descripción válida";
        var eventDate = DateTime.UtcNow.AddMonths(2);
        var venue = ""; // Empty venue
        var maxCapacity = 1000;
        var basePrice = 50.00m;

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            Event.Create(eventName, description, eventDate, venue, maxCapacity, basePrice));
        
        exception.Message.Should().Contain("El venue es obligatorio");
        exception.ParamName.Should().Be("venue");
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(0)]
    public void Event_Create_WithInvalidCapacity_ShouldThrowArgumentException(int maxCapacity)
    {
        // Arrange
        var eventName = "Evento Válido";
        var description = "Descripción válida";
        var eventDate = DateTime.UtcNow.AddMonths(2);
        var venue = "Test Venue";
        var basePrice = 50.00m;

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            Event.Create(eventName, description, eventDate, venue, maxCapacity, basePrice));
        
        exception.Message.Should().Contain("La capacidad máxima debe ser mayor a cero");
        exception.ParamName.Should().Be("maxCapacity");
    }

    [Theory]
    [InlineData(0.01)]
    [InlineData(50.00)]
    [InlineData(999.99)]
    [InlineData(2500.75)]
    public void Event_Create_WithValidPrices_ShouldSucceed(decimal basePrice)
    {
        // Arrange
        var eventName = "Evento Con Precio Válido";
        var description = "Descripción válida";
        var eventDate = DateTime.UtcNow.AddMonths(2);
        var venue = "Test Venue";
        var maxCapacity = 1000;

        // Act
        var eventEntity = Event.Create(eventName, description, eventDate, venue, maxCapacity, basePrice);

        // Assert
        eventEntity.BasePrice.Should().Be(basePrice);
        eventEntity.Should().NotBeNull();
    }

    #endregion

    #region Business Logic Tests

    [Fact]
    public void Event_UpdateDetails_WithValidData_ShouldUpdateSuccessfully()
    {
        // Arrange
        var eventEntity = Event.Create(
            "Concierto Original",
            "Descripción original",
            DateTime.UtcNow.AddMonths(2),
            "Venue Original",
            10000,
            100.00m);

        // Act
        eventEntity.UpdateDetails(
            "Concierto Foo Fighters 2026 - SOLD OUT",
            "Evento agotado - últimas entradas",
            45000);

        // Assert
        eventEntity.Name.Should().Be("Concierto Foo Fighters 2026 - SOLD OUT");
        eventEntity.Description.Should().Be("Evento agotado - últimas entradas");
        eventEntity.MaxCapacity.Should().Be(45000);
        eventEntity.UpdatedAt.Should().NotBeNull();
        eventEntity.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void Event_Deactivate_ShouldDeactivateEvent()
    {
        // Arrange
        var eventEntity = Event.Create(
            "Evento a Desactivar",
            "Evento que será desactivado",
            DateTime.UtcNow.AddMonths(2),
            "Test Venue",
            1000,
            50.00m);

        // Act
        eventEntity.Deactivate();

        // Assert
        eventEntity.Status.Should().Be("inactive");
        eventEntity.UpdatedAt.Should().NotBeNull();
        eventEntity.IsActive.Should().BeFalse();
    }

    [Fact]
    public void Event_Reactivate_WithFutureDate_ShouldReactivateSuccessfully()
    {
        // Arrange
        var eventEntity = Event.Create(
            "Evento Desactivado",
            "Evento previamente desactivado",
            DateTime.UtcNow.AddMonths(2),
            "Test Venue",
            1000,
            50.00m);
        eventEntity.Deactivate();

        // Act
        eventEntity.Reactivate();

        // Assert
        eventEntity.Status.Should().Be("active");
        eventEntity.IsActive.Should().BeTrue();
        eventEntity.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public void Event_ValidateSeatCapacity_WithValidCount_ShouldNotThrow()
    {
        // Arrange
        var eventEntity = Event.Create(
            "Evento Capacidad",
            "Evento para validar capacidad",
            DateTime.UtcNow.AddMonths(2),
            "Test Venue",
            1000, // Max capacity
            50.00m);

        // Act & Assert
        eventEntity.Invoking(e => e.ValidateSeatCapacity(500))
            .Should().NotThrow();
        
        eventEntity.Invoking(e => e.ValidateSeatCapacity(1000))
            .Should().NotThrow();
    }

    [Fact]
    public void Event_ValidateSeatCapacity_WithExcessiveCount_ShouldThrowException()
    {
        // Arrange
        var eventEntity = Event.Create(
            "Evento Capacidad Limitada",
            "Evento con capacidad limitada",
            DateTime.UtcNow.AddMonths(2),
            "Test Venue",
            100, // Max capacity
            50.00m);

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => 
            eventEntity.ValidateSeatCapacity(150)); // Exceeds capacity
        
        exception.Message.Should().Contain("La cantidad de asientos excede la capacidad máxima del evento");
    }

    #endregion

    #region Legacy Tests - Keeping for compatibility

    [Fact]
    public void Event_AddSeat_ShouldAddToSeatsCollection()
    {
        // Arrange - Using Event.Create() factory method
        var eventEntity = Event.Create(
            "Test Event",
            "Event description",
            DateTime.UtcNow.AddDays(30),
            "Test Venue",
            1000,
            50.00m);
        
        var seat = new Seat
        {
            Id = Guid.NewGuid(),
            EventId = eventEntity.Id,
            SectionCode = "A",
            RowNumber = 1,
            SeatNumber = 1,
            Price = 75.00m
        };

        // Act
        eventEntity.Seats.Add(seat);

        // Assert
        eventEntity.Seats.Should().HaveCount(1);
        eventEntity.Seats.First().Should().Be(seat);
    }

    [Theory]
    [InlineData(0.01)]
    [InlineData(50.00)]
    [InlineData(999.99)]
    public void Event_BasePrice_ShouldAcceptValidValues(decimal price)
    {
        // Arrange & Act - Using Event.Create() factory method
        var eventEntity = Event.Create(
            "Test Event", 
            "Test Description", 
            DateTime.UtcNow.AddMonths(1), 
            "Test Venue", 
            1000, 
            price);

        // Assert
        eventEntity.BasePrice.Should().Be(price);
    }

    [Fact]
    public void Event_EventDate_ShouldAcceptFutureDate()
    {
        // Arrange
        var futureDate = DateTime.UtcNow.AddDays(30);
        
        // Act - Using Event.Create() factory method
        var eventEntity = Event.Create(
            "Test Event",
            "Test Description",
            futureDate,
            "Test Venue",
            1000,
            50.00m);

        // Assert
        eventEntity.EventDate.Should().Be(futureDate);
    }

    #endregion

    #region Seat Management and Business Logic Tests

    [Fact]
    public void IsBookable_WithFutureEventAndAvailableSeats_ShouldReturnTrue()
    {
        // Arrange
        var eventEntity = CreateValidEvent();
        AddAvailableSeats(eventEntity, 2);

        // Act
        var result = eventEntity.IsBookable();

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsBookable_WithDeactivatedEvent_ShouldReturnFalse()
    {
        // Arrange - Deactivated events are not bookable
        var eventEntity = CreateValidEvent();
        eventEntity.Deactivate();
        AddAvailableSeats(eventEntity, 2);

        // Act
        var result = eventEntity.IsBookable();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsBookable_WithNoAvailableSeats_ShouldReturnFalse()
    {
        // Arrange
        var eventEntity = CreateValidEvent();
        AddSoldSeats(eventEntity, 2);

        // Act
        var result = eventEntity.IsBookable();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void HasAvailableSeats_WithAvailableSeats_ShouldReturnTrue()
    {
        // Arrange
        var eventEntity = CreateValidEvent();
        AddAvailableSeats(eventEntity, 1);

        // Act
        var result = eventEntity.HasAvailableSeats();

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void HasAvailableSeats_WithNoAvailableSeats_ShouldReturnFalse()
    {
        // Arrange
        var eventEntity = CreateValidEvent();
        AddSoldSeats(eventEntity, 2);

        // Act
        var result = eventEntity.HasAvailableSeats();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void GetAvailableSeatsCount_WithMixedSeatStatuses_ShouldReturnCorrectCount()
    {
        // Arrange
        var eventEntity = CreateValidEvent();
        AddAvailableSeats(eventEntity, 3);
        AddReservedSeats(eventEntity, 2);
        AddSoldSeats(eventEntity, 1);

        // Act
        var result = eventEntity.GetAvailableSeatsCount();

        // Assert
        result.Should().Be(3);
    }

    [Fact]
    public void GetReservedSeatsCount_WithMixedSeatStatuses_ShouldReturnCorrectCount()
    {
        // Arrange
        var eventEntity = CreateValidEvent();
        AddAvailableSeats(eventEntity, 3);
        AddReservedSeats(eventEntity, 2);
        AddSoldSeats(eventEntity, 1);

        // Act
        var result = eventEntity.GetReservedSeatsCount();

        // Assert
        result.Should().Be(2);
    }

    [Fact]
    public void GetSoldSeatsCount_WithMixedSeatStatuses_ShouldReturnCorrectCount()
    {
        // Arrange
        var eventEntity = CreateValidEvent();
        AddAvailableSeats(eventEntity, 3);
        AddReservedSeats(eventEntity, 2);
        AddSoldSeats(eventEntity, 1);

        // Act
        var result = eventEntity.GetSoldSeatsCount();

        // Assert
        result.Should().Be(1);
    }

    [Fact]
    public void GetAvailableSeats_ShouldReturnOnlyAvailableSeats()
    {
        // Arrange
        var eventEntity = CreateValidEvent();
        AddAvailableSeats(eventEntity, 2);
        AddReservedSeats(eventEntity, 1);
        AddSoldSeats(eventEntity, 1);

        // Act
        var result = eventEntity.GetAvailableSeats();

        // Assert
        result.Should().HaveCount(2);
        result.Should().OnlyContain(s => s.IsAvailable());
    }

    [Fact]
    public void GetSeatsBySection_WithSpecificSection_ShouldReturnOnlySeatsFromThatSection()
    {
        // Arrange
        var eventEntity = CreateValidEvent();
        AddSeatToSection(eventEntity, "VIP", Seat.StatusAvailable);
        AddSeatToSection(eventEntity, "VIP", Seat.StatusReserved);
        AddSeatToSection(eventEntity, "General", Seat.StatusAvailable);

        // Act
        var result = eventEntity.GetSeatsBySection("VIP");

        // Assert
        result.Should().HaveCount(2);
        result.Should().OnlyContain(s => s.SectionCode == "VIP");
    }

    [Fact]
    public void IsValidForCreation_WithValidData_ShouldReturnTrue()
    {
        // Arrange
        var eventEntity = CreateValidEvent();

        // Act
        var result = eventEntity.IsValidForCreation();

        // Assert
        result.Should().BeTrue();
    }

    #endregion

    #region Test Helper Methods

    private static Event CreateValidEvent()
    {
        return Event.Create(
            "Test Concert",
            "A great test concert", 
            DateTime.UtcNow.AddDays(30),
            "Test Venue",
            1000,
            50.00m);
    }

    private static void AddAvailableSeats(Event eventEntity, int count)
    {
        for (int i = 0; i < count; i++)
        {
            eventEntity.Seats.Add(CreateSeat(eventEntity.Id, Seat.StatusAvailable, i + 1));
        }
    }

    private static void AddReservedSeats(Event eventEntity, int count)
    {
        for (int i = 0; i < count; i++)
        {
            eventEntity.Seats.Add(CreateSeat(eventEntity.Id, Seat.StatusReserved, i + 100));
        }
    }

    private static void AddSoldSeats(Event eventEntity, int count)
    {
        for (int i = 0; i < count; i++)
        {
            eventEntity.Seats.Add(CreateSeat(eventEntity.Id, Seat.StatusSold, i + 200));
        }
    }

    private static void AddSeatToSection(Event eventEntity, string section, string status)
    {
        var seat = CreateSeat(eventEntity.Id, status, 1);
        seat.SectionCode = section;
        eventEntity.Seats.Add(seat);
    }

    private static Seat CreateSeat(Guid eventId, string status, int seatNumber)
    {
        return new Seat
        {
            Id = Guid.NewGuid(),
            EventId = eventId,
            SectionCode = "General",
            RowNumber = 1,
            SeatNumber = seatNumber,
            Price = 75.00m,
            Status = status
        };
    }

    #endregion
}