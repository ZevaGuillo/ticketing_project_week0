using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Inventory.Domain.Entities;
using Inventory.Domain.Enums;
using Xunit;

namespace Inventory.IntegrationTests;

public class WaitlistEndpointsTests : IClassFixture<InventoryApiFactory>
{
    private readonly HttpClient _client;
    private readonly InventoryApiFactory _factory;

    public WaitlistEndpointsTests(InventoryApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task JoinWaitlist_WithoutXUserIdHeader_ShouldReturn400()
    {
        var request = new
        {
            eventId = Guid.NewGuid(),
            section = "A"
        };

        var response = await _client.PostAsJsonAsync("/api/waitlist/join", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task JoinWaitlist_WithValidData_ShouldReturn201()
    {
        var userId = Guid.NewGuid();
        _client.DefaultRequestHeaders.Add("X-User-Id", userId.ToString());

        var request = new
        {
            eventId = Guid.NewGuid(),
            section = "A"
        };

        var response = await _client.PostAsJsonAsync("/api/waitlist/join", request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var content = await response.Content.ReadFromJsonAsync<JoinWaitlistResponse>();
        content.Should().NotBeNull();
        content!.WaitlistEntryId.Should().NotBeEmpty();
        content.QueuePosition.Should().BeGreaterThan(0);
        content.Status.Should().Be(WaitlistStatus.ACTIVE.ToString());
    }

    [Fact]
    public async Task JoinWaitlist_ForSameUserAndSection_ShouldReturn409()
    {
        var userId = Guid.NewGuid();
        _client.DefaultRequestHeaders.Add("X-User-Id", userId.ToString());

        var request = new
        {
            eventId = Guid.NewGuid(),
            section = "A"
        };

        var firstResponse = await _client.PostAsJsonAsync("/api/waitlist/join", request);
        firstResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var secondResponse = await _client.PostAsJsonAsync("/api/waitlist/join", request);
        secondResponse.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task GetWaitlistStatus_WithExistingEntry_ShouldReturn200()
    {
        var userId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        _client.DefaultRequestHeaders.Add("X-User-Id", userId.ToString());

        var joinRequest = new
        {
            eventId = eventId,
            section = "A"
        };
        await _client.PostAsJsonAsync("/api/waitlist/join", joinRequest);

        var statusResponse = await _client.GetAsync($"/api/waitlist/status?eventId={eventId}&section=A");

        statusResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await statusResponse.Content.ReadFromJsonAsync<GetWaitlistStatusResponse>();
        content.Should().NotBeNull();
        content!.WaitlistEntryId.Should().NotBeEmpty();
        content.Status.Should().Be(WaitlistStatus.ACTIVE.ToString());
    }

    [Fact]
    public async Task GetWaitlistStatus_WithNonExistingEntry_ShouldReturn404()
    {
        var userId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        _client.DefaultRequestHeaders.Add("X-User-Id", userId.ToString());

        var statusResponse = await _client.GetAsync($"/api/waitlist/status?eventId={eventId}&section=A");

        statusResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private record JoinWaitlistResponse(
        Guid WaitlistEntryId,
        int QueuePosition,
        DateTime JoinedAt,
        string Status
    );

    private record GetWaitlistStatusResponse(
        Guid WaitlistEntryId,
        int QueuePosition,
        string Status,
        DateTime JoinedAt,
        DateTime? NotifiedAt
    );
}