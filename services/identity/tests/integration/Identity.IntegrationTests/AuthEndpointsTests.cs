using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Identity.Domain.ValueObjects;
using Xunit;

namespace Identity.IntegrationTests;

public class AuthEndpointsTests : IClassFixture<IdentityApiFactory>
{
    private readonly HttpClient _client;
    private readonly IdentityApiFactory _factory;

    public AuthEndpointsTests(IdentityApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task PostRegister_WithValidData_ShouldReturn201Created()
    {
        var request = new
        {
            email = $"newuser_{Guid.NewGuid()}@example.com",
            password = "ValidPassword123!",
            role = "User"
        };

        var response = await _client.PostAsJsonAsync("/auth/register", request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        
        var content = await response.Content.ReadFromJsonAsync<RegisterResponse>();
        content.Should().NotBeNull();
        content!.Email.Should().Be(request.email);
    }

    [Fact]
    public async Task PostRegister_WithExistingEmail_ShouldReturn400BadRequest()
    {
        var email = $"duplicate_{Guid.NewGuid()}@example.com";
        var request = new
        {
            email = email,
            password = "ValidPassword123!",
            role = "User"
        };

        await _client.PostAsJsonAsync("/auth/register", request);
        
        var duplicateRequest = new
        {
            email = email,
            password = "AnotherPassword123!",
            role = "User"
        };
        
        var response = await _client.PostAsJsonAsync("/auth/register", duplicateRequest);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task PostRegister_WithWeakPassword_ShouldReturn400BadRequest()
    {
        var request = new
        {
            email = $"user_{Guid.NewGuid()}@example.com",
            password = "short",
            role = "User"
        };

        var response = await _client.PostAsJsonAsync("/auth/register", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task PostRegister_WithInvalidEmail_ShouldReturn400BadRequest()
    {
        var request = new
        {
            email = "not-an-email",
            password = "ValidPassword123!",
            role = "User"
        };

        var response = await _client.PostAsJsonAsync("/auth/register", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task PostRegister_WithoutRole_ShouldDefaultToUser()
    {
        var request = new
        {
            email = $"user_{Guid.NewGuid()}@example.com",
            password = "ValidPassword123!"
        };

        var response = await _client.PostAsJsonAsync("/auth/register", request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        
        var content = await response.Content.ReadFromJsonAsync<RegisterResponse>();
        content!.Role.Should().Be("User");
    }

    [Fact]
    public async Task PostRegister_WithAdminRole_ShouldCreateAdminUser()
    {
        var request = new
        {
            email = $"admin_{Guid.NewGuid()}@example.com",
            password = "AdminPassword123!",
            role = "Admin"
        };

        var response = await _client.PostAsJsonAsync("/auth/register", request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        
        var content = await response.Content.ReadFromJsonAsync<RegisterResponse>();
        content!.Role.Should().Be("Admin");
    }

    [Fact]
    public async Task PostToken_WithValidCredentials_ShouldReturn200Ok()
    {
        var email = $"login_{Guid.NewGuid()}@example.com";
        var password = "UserPassword123!";
        
        var registerRequest = new
        {
            email = email,
            password = password,
            role = "User"
        };
        await _client.PostAsJsonAsync("/auth/register", registerRequest);

        var tokenRequest = new
        {
            email = email,
            password = password
        };

        var response = await _client.PostAsJsonAsync("/auth/token", tokenRequest);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadFromJsonAsync<TokenResponse>();
        content.Should().NotBeNull();
        content!.Token.Should().NotBeEmpty();
        content.ExpiresAt.Should().BeAfter(DateTime.UtcNow);
    }

    [Fact]
    public async Task PostToken_WithInvalidEmail_ShouldReturn401Unauthorized()
    {
        var request = new
        {
            email = "nonexistent@example.com",
            password = "SomePassword123!"
        };

        var response = await _client.PostAsJsonAsync("/auth/token", request);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task PostToken_WithWrongPassword_ShouldReturn401Unauthorized()
    {
        var email = $"wrongpass_{Guid.NewGuid()}@example.com";
        var password = "CorrectPassword123!";
        
        var registerRequest = new
        {
            email = email,
            password = password,
            role = "User"
        };
        await _client.PostAsJsonAsync("/auth/register", registerRequest);

        var tokenRequest = new
        {
            email = email,
            password = "WrongPassword456!"
        };

        var response = await _client.PostAsJsonAsync("/auth/token", tokenRequest);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task PostToken_WithEmptyEmail_ShouldReturn400BadRequest()
    {
        var request = new
        {
            email = "",
            password = "SomePassword123!"
        };

        var response = await _client.PostAsJsonAsync("/auth/token", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task PostToken_WithEmptyPassword_ShouldReturn401Unauthorized()
    {
        var email = $"emptypass_{Guid.NewGuid()}@example.com";
        var password = "CorrectPassword123!";
        
        var registerRequest = new
        {
            email = email,
            password = password,
            role = "User"
        };
        await _client.PostAsJsonAsync("/auth/register", registerRequest);

        var tokenRequest = new
        {
            email = email,
            password = ""
        };

        var response = await _client.PostAsJsonAsync("/auth/token", tokenRequest);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
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

    private record RegisterResponse
    {
        public Guid UserId { get; init; }
        public string Email { get; init; } = string.Empty;
        public string Role { get; init; } = string.Empty;
    }

    private record TokenResponse
    {
        public string Token { get; init; } = string.Empty;
        public DateTime ExpiresAt { get; init; }
        public string UserRole { get; init; } = string.Empty;
        public string UserEmail { get; init; } = string.Empty;
    }

    private record HealthResponse
    {
        public string Status { get; init; } = string.Empty;
        public string Service { get; init; } = string.Empty;
    }
}
