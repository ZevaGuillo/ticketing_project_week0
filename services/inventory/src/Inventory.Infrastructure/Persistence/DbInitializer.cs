using Microsoft.EntityFrameworkCore;
using Inventory.Domain.Ports;
using Npgsql;

namespace Inventory.Infrastructure.Persistence;

/// <summary>
/// Inicializa la base de datos para el módulo Inventory: crea schema si es necesario y aplica migraciones.
/// Sigue el mismo patrón usado por el servicio Identity para consistencia.
/// </summary>
public class DbInitializer : IDbInitializer
{
    private readonly InventoryDbContext _db;

    public DbInitializer(InventoryDbContext db) => _db = db ?? throw new ArgumentNullException(nameof(db));

    public async Task InitializeAsync()
    {
        try
        {
            var connection = _db.Database.GetDbConnection();
            await connection.OpenAsync();

            try
            {
                // Crear schema con permisos necesarios
                using var createCommand = connection.CreateCommand();
                createCommand.CommandText = @"
                    CREATE SCHEMA IF NOT EXISTS bc_inventory;
                    ALTER SCHEMA bc_inventory OWNER TO postgres;
                    GRANT ALL PRIVILEGES ON SCHEMA bc_inventory TO postgres;
                ";
                await createCommand.ExecuteNonQueryAsync();
                Console.WriteLine("✓ Schema 'bc_inventory' verificado/creado exitosamente");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠ Warning al crear schema: {ex.Message}");
            }

            await connection.CloseAsync();

            // Aplicar migraciones
            try
            {
                await _db.Database.MigrateAsync();
            }
            catch (PostgresException pex) when (pex.SqlState == "42P01")
            {
                // Table doesn't exist, create migrations history table manually
                Console.WriteLine("↻ Creando tabla de migraciones manualmente...");
                await connection.OpenAsync();
                try
                {
                    using var createTableCommand = connection.CreateCommand();
                    createTableCommand.CommandText = @"
                        SET search_path = bc_inventory, public;
                        CREATE TABLE IF NOT EXISTS __EFMigrationsHistory (
                            MigrationId CHARACTER VARYING(150) NOT NULL PRIMARY KEY,
                            ProductVersion CHARACTER VARYING(32) NOT NULL
                        );
                        SET search_path = public;
                    ";
                    await createTableCommand.ExecuteNonQueryAsync();
                    Console.WriteLine("✓ Tabla de migraciones creada");
                }
                finally
                {
                    await connection.CloseAsync();
                }
                
                // Retry migrations after creating the history table
                await _db.Database.MigrateAsync();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"✗ Error fatal en inicialización de BD (Inventory): {ex.Message}");
            throw;
        }
    }
}
