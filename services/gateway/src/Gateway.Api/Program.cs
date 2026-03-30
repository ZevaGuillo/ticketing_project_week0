using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Yarp.ReverseProxy.Transforms;
using Gateway.Api.Transforms;
using Gateway.Api.Constants;

var builder = WebApplication.CreateBuilder(args);

var jwtKey = builder.Configuration["Jwt:Key"]!;
var jwtIssuer = builder.Configuration["Jwt:Issuer"]!;
var jwtAudience = builder.Configuration["Jwt:Audience"]!;

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy =>
        policy.RequireClaim(GatewayClaims.Role, "Admin"));
});

builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/health", () => Results.Ok(new { status = "healthy", service = "gateway" }));

app.MapGet("/catalog/events", () => Results.Ok(new { events = new[] { new { id = Guid.NewGuid(), name = "Test Event" } } }));

app.MapPost("/inventory/reservations", (HttpContext context) =>
{
    var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
    var userRole = context.User.FindFirstValue(ClaimTypes.Role);

    return Results.Ok(new { reservationId = Guid.NewGuid(), userId = userId });
}).RequireAuthorization();

app.MapGet("/admin/users", (HttpContext context) =>
{
    var userRole = context.User.FindFirstValue(ClaimTypes.Role);
    
    if (userRole != "Admin")
    {
        return Results.Forbid();
    }
    
    return Results.Ok(new { users = new[] { new { id = Guid.NewGuid(), email = "admin@example.com" } } });
}).RequireAuthorization("AdminOnly");

app.MapReverseProxy(proxyPipeline =>
{
    proxyPipeline.Use(async (context, next) =>
    {
        var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        var userRole = context.User.FindFirstValue(ClaimTypes.Role);

        if (!string.IsNullOrEmpty(userId))
        {
            context.Request.Headers[GatewayHeaders.UserId] = userId;
        }
        
        if (!string.IsNullOrEmpty(userRole))
        {
            context.Request.Headers[GatewayHeaders.UserRole] = userRole;
        }

        await next();
    });
});

app.Run();

public partial class Program { }
