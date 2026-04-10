using Identity.Application.DTOs;
using Identity.Application.UseCases.CreateUser;
using Identity.Application.UseCases.IssueToken;
using Identity.Infrastructure;
using Identity.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddScoped<IssueTokenHandler>();
builder.Services.AddScoped<CreateUserHandler>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddControllers()
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
app.MapControllers();

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
    public partial class Program { }
}
