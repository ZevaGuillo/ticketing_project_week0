using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using Identity.Domain.Entities;
using Identity.Domain.ValueObjects;

namespace Identity.IntegrationTests.Common;

public class TokenFactory
{
    private const string DefaultKey = "dev-secret-key-minimum-32-chars-required-for-security";
    private const string DefaultIssuer = "SpecKit.Identity";
    private const string DefaultAudience = "SpecKit.Services";

    public static string CreateValidToken(User user, TimeSpan? expiry = null)
    {
        return CreateToken(user, expiry ?? TimeSpan.FromHours(2));
    }

    public static string CreateExpiredToken(User user)
    {
        return CreateToken(user, TimeSpan.FromHours(-1));
    }

    public static string CreateTokenWithCustomClaims(User user, IEnumerable<Claim> additionalClaims, TimeSpan? expiry = null)
    {
        var key = Encoding.UTF8.GetBytes(DefaultKey);
        
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new("role", user.Role.ToString())
        };
        
        claims.AddRange(additionalClaims);

        var descriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.Add(expiry ?? TimeSpan.FromHours(2)),
            Issuer = DefaultIssuer,
            Audience = DefaultAudience,
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(key),
                SecurityAlgorithms.HmacSha256)
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(descriptor);
        return tokenHandler.WriteToken(token);
    }

    private static string CreateToken(User user, TimeSpan expiry)
    {
        var key = Encoding.UTF8.GetBytes(DefaultKey);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim("role", user.Role.ToString())
        };

        var descriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.Add(expiry),
            Issuer = DefaultIssuer,
            Audience = DefaultAudience,
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(key),
                SecurityAlgorithms.HmacSha256)
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(descriptor);
        return tokenHandler.WriteToken(token);
    }

    public static string CreateTokenForUser(Guid userId, string email, Role role = Role.User)
    {
        var user = new User(email, "hashed", role);
        
        var idProperty = typeof(User).GetProperty("Id");
        var backingField = typeof(User).GetField("<Id>k__BackingField", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        backingField?.SetValue(user, userId);
        
        return CreateValidToken(user);
    }

    public static string CreateExpiredTokenForUser(Guid userId, string email, Role role = Role.User)
    {
        var user = new User(email, "hashed", role);
        
        var backingField = typeof(User).GetField("<Id>k__BackingField", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        backingField?.SetValue(user, userId);
        
        return CreateExpiredToken(user);
    }

    public static (string Token, DateTime ExpiresAt) CreateTokenWithExpiry(User user)
    {
        var expiry = TimeSpan.FromHours(2);
        var expiresAt = DateTime.UtcNow.Add(expiry);
        return (CreateValidToken(user, expiry), expiresAt);
    }
}
