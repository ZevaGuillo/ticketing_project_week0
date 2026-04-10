using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using Identity.Domain.Entities;
using Identity.Domain.Ports;
using Identity.Domain.ValueObjects;
using Microsoft.Extensions.Configuration;

namespace Identity.Infrastructure.Security;

public class JwtTokenGenerator : ITokenGenerator
{
    private readonly string _key;
    private readonly string _issuer;
    private readonly string _audience;
    private readonly int _expirationMinutes;

    public JwtTokenGenerator(IConfiguration configuration)
    {
        _key = configuration["Jwt:Key"]!;
        _issuer = configuration["Jwt:Issuer"]!;
        _audience = configuration["Jwt:Audience"]!;
        _expirationMinutes = configuration.GetValue<int>("Jwt:ExpirationMinutes", 120);
    }

    public string Generate(User user)
    {
        var result = GenerateWithExpiration(user);
        return result.Token;
    }

    public TokenGeneratorResult GenerateWithExpiration(User user)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(_key);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim("role", user.Role.ToString())
        };

        var expiresAt = DateTime.UtcNow.AddMinutes(_expirationMinutes);

        var signingKey = new SymmetricSecurityKey(key) { KeyId = "speckit-hmac-key" };

        var descriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = expiresAt,
            Issuer = _issuer,
            Audience = _audience,
            SigningCredentials = new SigningCredentials(
                signingKey,
                SecurityAlgorithms.HmacSha256)
        };

        var token = tokenHandler.CreateToken(descriptor);
        return new TokenGeneratorResult(tokenHandler.WriteToken(token), expiresAt);
    }
}