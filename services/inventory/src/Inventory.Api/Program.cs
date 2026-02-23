using Inventory.Infrastructure;
using Inventory.Domain.Ports;

var builder = WebApplication.CreateBuilder(args);

// Registrar infra y adaptadores del servicio Inventory
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

// Aplicar migraciones / inicialización de BD al iniciar
using (var scope = app.Services.CreateScope())
{
    try
    {
        var dbInitializer = scope.ServiceProvider.GetRequiredService<IDbInitializer>();
        await dbInitializer.InitializeAsync();
        Console.WriteLine("✓ Inventory DB initialized");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"✗ Inventory DB initialization failed: {ex.Message}");
        throw;
    }
}

app.Run();

public partial class Program { }
