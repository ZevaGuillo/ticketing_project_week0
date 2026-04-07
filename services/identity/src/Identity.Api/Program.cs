using Identity.Api;
using Identity.Domain.Ports;
using Identity.Domain.ValueObjects;
using Identity.Application.UseCases.IssueToken;
using Identity.Application.UseCases.CreateUser;
using Identity.Infrastructure;
using Identity.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddScoped<IssueTokenHandler>();
builder.Services.AddScoped<CreateUserHandler>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddMvc()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

builder.Services.AddCors(options =>
{
    options.AddPolicy("FrontendPolicy", policy =>
    {
        policy.WithOrigins("http://localhost:3000")
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

app.UseCors("FrontendPolicy");

app.MapPost("/auth/register", async (HttpContext context) =>
{
    var handler = context.RequestServices.GetRequiredService<CreateUserHandler>();
    
    try
    {
        var request = await context.Request.ReadFromJsonAsync<CreateUserRequest>();
        if (request == null)
        {
            return Results.BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Bad Request",
                Detail = "Invalid request body"
            });
        }

        var role = Enum.TryParse<Role>(request.Role, true, out var parsedRole) 
            ? parsedRole 
            : Role.User;
        
        var userId = await handler.Handle(
            new CreateUserCommand(request.Email, request.Password, role));

        var response = new CreateUserResponse(
            userId: userId,
            email: request.Email,
            role: role.ToString()
        );

        return Results.Created($"/users/{userId}", response);
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(new ProblemDetails
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "Validation Error",
            Detail = ex.Message
        });
    }
    catch (InvalidOperationException ex)
    {
        return Results.BadRequest(new ProblemDetails
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "Conflict",
            Detail = ex.Message
        });
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new ProblemDetails
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "Bad Request",
            Detail = ex.Message
        });
    }
});

app.MapPost("/auth/token", async (HttpContext context) =>
{
    var handler = context.RequestServices.GetRequiredService<IssueTokenHandler>();
    
    try
    {
        var request = await context.Request.ReadFromJsonAsync<IssueTokenRequest>();
        if (request == null)
        {
            return Results.BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Bad Request",
                Detail = "Invalid request body"
            });
        }

        if (string.IsNullOrWhiteSpace(request.Email))
        {
            return Results.BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Validation Error",
                Detail = "Email is required"
            });
        }

        if (string.IsNullOrWhiteSpace(request.Password))
        {
            return Results.Json(new ProblemDetails
            {
                Status = StatusCodes.Status401Unauthorized,
                Title = "Unauthorized",
                Detail = "Invalid credentials"
            }, statusCode: StatusCodes.Status401Unauthorized);
        }

        var result = await handler.Handle(
            new IssueTokenCommand(request.Email, request.Password));

        var response = new IssueTokenResponse(
            token: result.AccessToken,
            expiresAt: result.ExpiresAt,
            userRole: result.UserRole.ToString(),
            userEmail: result.UserEmail
        );

        return Results.Ok(response);
    }
    catch (Exception)
    {
        return Results.Json(new ProblemDetails
        {
            Status = StatusCodes.Status401Unauthorized,
            Title = "Unauthorized",
            Detail = "Invalid credentials"
        }, statusCode: StatusCodes.Status401Unauthorized);
    }
});

app.MapGet("/health", () => Results.Ok(new { status = "healthy", service = "identity" }));
app.MapGet("/auth/health", () => Results.Ok(new { status = "healthy", service = "identity" }));

app.MapGet("/internal/users/{id:guid}", async (Guid id, IUserRepository userRepo) =>
{
    var user = await userRepo.GetByIdAsync(id);
    if (user == null) return Results.NotFound();
    return Results.Ok(new { userId = user.Id, email = user.Email });
});

using (var scope = app.Services.CreateScope())
{
    try
    {
        var dbContext = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        dbContext.Database.Migrate();
        Console.WriteLine("Identity migrations applied successfully");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Warning: Could not apply migrations: {ex.Message}");
    }
}

app.Run();

namespace Identity.Api
{
    public record IssueTokenRequest(string Email, string Password);

    public record IssueTokenResponse(
        string token,
        DateTime expiresAt,
        string userRole,
        string userEmail
    );

    public record CreateUserRequest(string Email, string Password, string Role = "User");

    public record CreateUserResponse(
        Guid userId,
        string email,
        string role
    );

    public partial class Program { }
}
