using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Xunit;

namespace Gateway.IntegrationTests;

public class GatewayEndpointsTests : IClassFixture<GatewayApiFactory>
{
    private readonly HttpClient _client;
    private readonly GatewayApiFactory _factory;

    public GatewayEndpointsTests(GatewayApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetCatalogEvents_WithoutToken_ShouldReturn200()
    {
        var response = await _client.GetAsync("/catalog/events");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task PostInventoryReservations_WithoutToken_ShouldReturn401()
    {
        var request = new
        {
            eventId = Guid.NewGuid(),
            quantity = 2
        };

        var response = await _client.PostAsJsonAsync("/inventory/reservations", request);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task PostInventoryReservations_WithValidToken_ShouldReturn200AndForwardUserId()
    {
        var token = _factory.GenerateValidToken();
        _client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var request = new
        {
            eventId = Guid.NewGuid(),
            quantity = 2
        };

        var response = await _client.PostAsJsonAsync("/inventory/reservations", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task PostInventoryReservations_WithExpiredToken_ShouldReturn401()
    {
        var token = _factory.GenerateExpiredToken();
        _client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var request = new
        {
            eventId = Guid.NewGuid(),
            quantity = 2
        };

        var response = await _client.PostAsJsonAsync("/inventory/reservations", request);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task PostAdminRoute_WithUserRole_ShouldReturn403()
    {
        var token = _factory.GenerateUserToken();
        _client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var response = await _client.GetAsync("/admin/users");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetHealth_ShouldReturn200Ok()
    {
        var response = await _client.GetAsync("/health");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadFromJsonAsync<HealthResponse>();
        content.Should().NotBeNull();
        content!.Status.Should().Be("healthy");
    }

    private record ProblemDetailsResponse
    {
        public string Title { get; init; } = string.Empty;
        public string Detail { get; init; } = string.Empty;
    }

    private record HealthResponse
    {
        public string Status { get; init; } = string.Empty;
    }
}
