using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Yarp.ReverseProxy;
using Yarp.ReverseProxy.Transforms;
using Gateway.Api.Constants;
using Gateway.Api.Transforms;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins("http://localhost:3000")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var jwtKey = builder.Configuration["Jwt:Key"]!;
var jwtIssuer = builder.Configuration["Jwt:Issuer"]!;
var jwtAudience = builder.Configuration["Jwt:Audience"]!;

Console.WriteLine($"[JWT CONFIG] Key: {jwtKey.Substring(0, 10)}..., Issuer: {jwtIssuer}, Audience: {jwtAudience}");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ClockSkew = TimeSpan.Zero,
            NameClaimType = "sub",
            RoleClaimType = "role"
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy =>
        policy.RequireRole("Admin"));
});

var proxyBuilder = builder.Services.AddReverseProxy();
proxyBuilder.LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

proxyBuilder.AddTransforms(transforms =>
{
    transforms.AddRequestTransform(context =>
    {
        var userId = context.HttpContext.Request.Headers["X-User-Id"].FirstOrDefault();
        
        context.ProxyRequest.Headers.Remove("X-User-Id");
        
        if (!string.IsNullOrEmpty(userId))
        {
            context.ProxyRequest.Headers.TryAddWithoutValidation("X-User-Id", userId);
        }
        
        var userRole = context.HttpContext.Request.Headers["X-User-Role"].FirstOrDefault();
        context.ProxyRequest.Headers.Remove("X-User-Role");
        if (!string.IsNullOrEmpty(userRole))
        {
            context.ProxyRequest.Headers.TryAddWithoutValidation("X-User-Role", userRole);
        }

        var userEmail = context.HttpContext.User.FindFirstValue("email");
        if (!string.IsNullOrEmpty(userEmail))
        {
            context.ProxyRequest.Headers.Remove("X-User-Email");
            context.ProxyRequest.Headers.TryAddWithoutValidation("X-User-Email", userEmail);
        }
        
        return ValueTask.CompletedTask;
    });
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.UseCors();

app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/health", () => Results.Ok(new { status = "healthy", service = "gateway" }));

app.MapReverseProxy();

app.Run();

public partial class Program { }
