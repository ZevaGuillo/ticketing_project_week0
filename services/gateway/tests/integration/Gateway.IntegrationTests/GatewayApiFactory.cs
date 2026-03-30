using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Xunit.Abstractions;

namespace Gateway.IntegrationTests;

public class GatewayApiFactory : WebApplicationFactory<Program>
{
    private readonly string _jwtKey = "dev-secret-key-minimum-32-chars-required-for-security";
    private readonly string _jwtIssuer = "SpecKit.Identity";
    private readonly string _jwtAudience = "SpecKit.Services";

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

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        
        builder.ConfigureAppConfiguration(configuration =>
        {
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Jwt:Key"] = _jwtKey,
                    ["Jwt:Issuer"] = _jwtIssuer,
                    ["Jwt:Audience"] = _jwtAudience,
                    ["Jwt:ExpirationMinutes"] = "120"
                })
                .Build();

            configuration.AddConfiguration(config);
        });
    }
}
