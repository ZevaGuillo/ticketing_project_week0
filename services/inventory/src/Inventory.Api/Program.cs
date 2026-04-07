using Inventory.Infrastructure;
using Inventory.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Inventory.Domain.Ports;
using UserContext;
using IUserContext = UserContext.IUserContext;
using UserContextImpl = UserContext.UserContext;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(Inventory.Application.UseCases.CreateReservation.CreateReservationCommand).Assembly));

builder.Services.AddControllers();

builder.Services.AddScoped<IUserContext, UserContextImpl>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

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

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("FrontendPolicy");

app.Use(async (context, next) =>
{
    Console.WriteLine($"[MIDDLEWARE] Incoming headers: {string.Join(", ", context.Request.Headers.Select(h => $"{h.Key}={h.Value}"))}");
    
    var userContext = context.RequestServices.GetRequiredService<IUserContext>();
    var userId = context.Request.Headers[UserContextExtensions.UserIdHeader].FirstOrDefault();
    var userRole = context.Request.Headers[UserContextExtensions.UserRoleHeader].FirstOrDefault();

    Console.WriteLine($"[MIDDLEWARE] X-User-Id: '{userId}', X-User-Role: '{userRole}'");

    if (!string.IsNullOrEmpty(userId))
    {
        userContext.SetUserId(userId);
    }

    if (!string.IsNullOrEmpty(userRole))
    {
        userContext.SetUserRole(userRole);
    }

    await next();
});

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    try
    {
        var dbContext = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();
        dbContext.Database.Migrate();
        Console.WriteLine("Inventory migrations applied successfully");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Warning: Could not apply migrations: {ex.Message}");
    }
}

app.Run();

public partial class Program { }