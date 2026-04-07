using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Inventory.Domain.Entities;
using Xunit;

namespace Inventory.IntegrationTests;

public class ReservationEndpointsTests : IClassFixture<InventoryApiFactory>
{
    private readonly HttpClient _client;
    private readonly InventoryApiFactory _factory;

    public ReservationEndpointsTests(InventoryApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task PostReservations_WithoutXUserIdHeader_ShouldReturn400()
    {
        var request = new
        {
            seatId = Guid.NewGuid(),
            customerId = "test-customer"
        };

        var response = await _client.PostAsJsonAsync("/reservations", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task PostReservations_WithXUserIdHeader_ShouldPersistUserId()
    {
        var userId = Guid.NewGuid().ToString();
        _client.DefaultRequestHeaders.Add("X-User-Id", userId);

        var seatId = Guid.NewGuid();
        await CreateTestSeat(seatId);

        var request = new
        {
            seatId = seatId,
            customerId = "test-customer"
        };

        var response = await _client.PostAsJsonAsync("/reservations", request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        
        var content = await response.Content.ReadFromJsonAsync<ReservationResponse>();
        content.Should().NotBeNull();
        content!.CustomerId.Should().Be(userId);
    }

    [Fact]
    public async Task PostReservations_WithXUserRoleHeader_ShouldForwardRole()
    {
        var userId = Guid.NewGuid().ToString();
        _client.DefaultRequestHeaders.Add("X-User-Id", userId);
        _client.DefaultRequestHeaders.Add("X-User-Role", "User");

        var seatId = Guid.NewGuid();
        await CreateTestSeat(seatId);

        var request = new
        {
            seatId = seatId,
            customerId = "test-customer"
        };

        var response = await _client.PostAsJsonAsync("/reservations", request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    private async Task CreateTestSeat(Guid seatId)
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<Inventory.Infrastructure.Persistence.InventoryDbContext>();
        var seat = new Seat
        {
            Id = seatId,
            Section = "A",
            Row = "1",
            Number = 10,
            Reserved = false
        };
        context.Seats.Add(seat);
        await context.SaveChangesAsync();
    }

    private record ReservationResponse(
        Guid ReservationId,
        Guid SeatId,
        string CustomerId,
        DateTime ExpiresAt,
        string Status
    );
}
