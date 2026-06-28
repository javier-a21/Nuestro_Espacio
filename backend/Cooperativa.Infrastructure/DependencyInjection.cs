using Cooperativa.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Cooperativa.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, string connectionString, bool useSqlite)
    {
        services.AddDbContext<AppDbContext>(opt =>
        {
            if (useSqlite) opt.UseSqlite(connectionString);   // dev local sin dependencias
            else opt.UseNpgsql(connectionString);             // PostgreSQL (Neon) en real
        });
        return services;
    }
}
