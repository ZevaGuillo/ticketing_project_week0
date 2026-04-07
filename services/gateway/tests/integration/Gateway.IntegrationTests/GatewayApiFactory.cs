using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace Gateway.IntegrationTests;

public class GatewayApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly string _jwtKey = "dev-secret-key-minimum-32-chars-required-for-security";
    private readonly string _jwtIssuer = "SpecKit.Identity";
    private readonly string _jwtAudience = "SpecKit.Services";

    private IHost? _mockBackend;
    private string _mockBackendAddress = "";

    public string GenerateValidToken()
    {
        var userId = Guid.NewGuid().ToString();
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, userId),
            new Claim(JwtRegisteredClaimNames.Email, "test@example.com"),
            new Claim("role", "User")
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _jwtIssuer,
            audience: _jwtAudience,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(2),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public string GenerateExpiredToken()
    {
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, Guid.NewGuid().ToString()),
            new Claim(JwtRegisteredClaimNames.Email, "test@example.com"),
            new Claim("role", "User")
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _jwtIssuer,
            audience: _jwtAudience,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(-1),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public string GenerateUserToken()
    {
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, Guid.NewGuid().ToString()),
            new Claim(JwtRegisteredClaimNames.Email, "user@example.com"),
            new Claim("role", "User")
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _jwtIssuer,
            audience: _jwtAudience,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(2),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public string GenerateAdminToken()
    {
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, Guid.NewGuid().ToString()),
            new Claim(JwtRegisteredClaimNames.Email, "admin@example.com"),
            new Claim("role", "Admin")
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _jwtIssuer,
            audience: _jwtAudience,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(2),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public async Task InitializeAsync()
    {
        _mockBackend = new HostBuilder()
            .ConfigureWebHostDefaults(webBuilder =>
            {
                webBuilder.UseUrls("http://127.0.0.1:0")
                    .Configure(app =>
                    {
                        app.Run(async context =>
                        {
                            context.Response.StatusCode = 200;
                            context.Response.ContentType = "application/json";
                            await context.Response.WriteAsync("[]");
                        });
                    });
            })
            .Build();

        await _mockBackend.StartAsync();

        var server = _mockBackend.Services.GetRequiredService<IServer>();
        var addressFeature = server.Features.Get<IServerAddressesFeature>()!;
        _mockBackendAddress = addressFeature.Addresses.First();
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        if (_mockBackend != null)
        {
            await _mockBackend.StopAsync();
            _mockBackend.Dispose();
        }
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        
        builder.ConfigureAppConfiguration(configuration =>
        {
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Key"] = _jwtKey,
                ["Jwt:Issuer"] = _jwtIssuer,
                ["Jwt:Audience"] = _jwtAudience,
                ["Jwt:ExpirationMinutes"] = "120",
                ["ReverseProxy:Clusters:catalog:Destinations:catalog:Address"] = _mockBackendAddress,
                ["ReverseProxy:Clusters:inventory:Destinations:inventory:Address"] = _mockBackendAddress,
                ["ReverseProxy:Clusters:identity:Destinations:identity:Address"] = _mockBackendAddress,
                ["ReverseProxy:Clusters:ordering:Destinations:ordering:Address"] = _mockBackendAddress,
                ["ReverseProxy:Clusters:payment:Destinations:payment:Address"] = _mockBackendAddress,
                ["ReverseProxy:Clusters:fulfillment:Destinations:fulfillment:Address"] = _mockBackendAddress,
            });
        });
    }
}
