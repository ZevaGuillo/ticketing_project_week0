using Microsoft.EntityFrameworkCore;
using Identity.Domain.Ports;
using Npgsql;

namespace Identity.Infrastructure.Persistence;

/// <summary>
/// Implementación del puerto IDbInitializer usando Entity Framework Core.
/// Responsable de aplicar las migraciones a la base de datos.
/// </summary>
public class DbInitializer : IDbInitializer
{
    private readonly IdentityDbContext _dbContext;

    public DbInitializer(IdentityDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    /// <summary>
    /// Aplica todas las migraciones pendientes a la base de datos.
    /// Crea el schema bc_identity si no existe, luego aplica las migraciones.
    /// </summary>
    public async Task InitializeAsync()
    {
        try
        {
            // Obtener conexión a la base de datos
            var connection = _dbContext.Database.GetDbConnection();
            await connection.OpenAsync();
            
            try
            {
                // Crear schema con permisos necesarios
                using var createCommand = connection.CreateCommand();
                createCommand.CommandText = @"
                    CREATE SCHEMA IF NOT EXISTS bc_identity;
                    ALTER SCHEMA bc_identity OWNER TO postgres;
                    GRANT ALL PRIVILEGES ON SCHEMA bc_identity TO postgres;
                ";
                await createCommand.ExecuteNonQueryAsync();
                Console.WriteLine("✓ Schema 'bc_identity' verificado/creado exitosamente");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠ Warning al crear schema: {ex.Message}");
            }
            
            await connection.CloseAsync();

            // Aplicar migraciones
            try
            {
                await _dbContext.Database.MigrateAsync();
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
                        SET search_path = bc_identity, public;
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
                await _dbContext.Database.MigrateAsync();
            }
            
            Console.WriteLine("✓ Migraciones aplicadas correctamente en schema bc_identity");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"✗ Error fatal en inicialización de BD: {ex.Message}");
            throw;
        }
    }
}