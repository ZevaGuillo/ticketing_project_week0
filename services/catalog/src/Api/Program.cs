using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Catalog.Application.UseCases.GetEventSeatmap;
using Catalog.Application.Ports;
using Catalog.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddControllers();

// Add CORS policy for frontend on localhost:3000
builder.Services.AddCors(options =>
{
    options.AddPolicy("FrontendPolicy", policy =>
    {
        policy.WithOrigins("http://localhost:3000")
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Add Infrastructure services
builder.Services.AddInfrastructure(builder.Configuration);

// Add MediatR
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(GetEventSeatmapHandler).Assembly));

var app = builder.Build();

// Aplicar migraciones / inicialización de BD al iniciar
using (var scope = app.Services.CreateScope())
{
    try
    {
        var dbInitializer = scope.ServiceProvider.GetRequiredService<IDbInitializer>();
        await dbInitializer.InitializeAsync();
        Console.WriteLine("✓ Catalog DB initialized");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"✗ Inventory DB initialization failed: {ex.Message}");
        throw;
    }
}


// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

app.UseCors("FrontendPolicy");
app.UseRouting();
app.MapControllers();

app.Run();

public partial class Program { }
