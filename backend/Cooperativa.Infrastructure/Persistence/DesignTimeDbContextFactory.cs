using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Cooperativa.Infrastructure.Persistence;

/// <summary>
/// Permite a "dotnet ef migrations" crear el contexto sin arrancar la Api.
/// La cadena real se toma de la variable de entorno COOPERATIVA_DB; si no existe,
/// usa un placeholder (suficiente para GENERAR migraciones; para aplicarlas hace falta la real).
/// </summary>
public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var conn = Environment.GetEnvironmentVariable("COOPERATIVA_DB")
                   ?? "Host=localhost;Database=cooperativa;Username=postgres;Password=postgres";

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(conn)
            .Options;

        return new AppDbContext(options);
    }
}
