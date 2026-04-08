using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Yarp.ReverseProxy;
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
        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = signingKey,
            // always return the known key regardless of token's kid header
            IssuerSigningKeyResolver = (_, _, _, _) => [signingKey],
            ClockSkew = TimeSpan.Zero,
            NameClaimType = "sub",
            RoleClaimType = "role"
        };
    });

builder.Services.AddAuthorizationBuilder()
    .AddPolicy("AdminOnly", policy =>
        policy.RequireRole("Admin"));

var proxyBuilder = builder.Services.AddReverseProxy();
proxyBuilder.LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

proxyBuilder.AddTransforms(transforms =>
{
    transforms.RequestTransforms.Add(new ClaimsForwardingTransform());
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
