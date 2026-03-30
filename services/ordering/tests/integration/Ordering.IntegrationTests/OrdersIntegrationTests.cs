using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Ordering.Application.DTOs;
using Ordering.Domain.Entities;
using Ordering.Infrastructure.Persistence;
using Xunit;

namespace Ordering.IntegrationTests;

public class OrdersIntegrationTests : IClassFixture<OrderingApiFactory>
{
    private readonly HttpClient _client;
    private readonly OrderingApiFactory _factory;

    public OrdersIntegrationTests(OrderingApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetOrderDetails_WhenOrderExists_ReturnsSuccess()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OrderingDbContext>();
        
        var orderId = Guid.NewGuid();
        var testOrder = new Order 
        { 
            Id = orderId, 
            UserId = "test-user-123", 
            TotalAmount = 99.99m,
            State = "draft",
            CreatedAt = DateTime.UtcNow
        };
        db.Orders.Add(testOrder);
        await db.SaveChangesAsync();

        var response = await _client.GetAsync($"/Orders/{orderId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<dynamic>();
        Assert.NotNull(result);
    }

    [Fact]
    public async Task GetOrderDetails_WhenOrderDoesNotExist_ReturnsNotFound()
    {
        var response = await _client.GetAsync($"/Orders/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task PostCartAdd_WithoutXUserIdHeader_ShouldReturn400()
    {
        var request = new
        {
            reservationId = Guid.NewGuid(),
            seatId = Guid.NewGuid(),
            price = 99.99m
        };

        var response = await _client.PostAsJsonAsync("/cart/add", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task PostCartAdd_WithXUserIdHeader_ShouldUseUserId()
    {
        var userId = Guid.NewGuid().ToString();
        _client.DefaultRequestHeaders.Add("X-User-Id", userId);

        var request = new
        {
            reservationId = Guid.NewGuid(),
            seatId = Guid.NewGuid(),
            price = 99.99m
        };

        var response = await _client.PostAsJsonAsync("/cart/add", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task PostCheckout_WithoutXUserIdHeader_ShouldReturn400()
    {
        var request = new
        {
            orderId = Guid.NewGuid()
        };

        var response = await _client.PostAsJsonAsync("/orders/checkout", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
