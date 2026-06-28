using Cooperativa.Domain;
using Cooperativa.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Cooperativa.Infrastructure.Persistence;

public class AppDbContext : IdentityDbContext<AppUser, IdentityRole<Guid>, Guid>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Cooperative> Cooperatives => Set<Cooperative>();
    public DbSet<RoomState> RoomStates => Set<RoomState>();
    public DbSet<DailyAction> DailyActions => Set<DailyAction>();
    public DbSet<ActionType> ActionTypes => Set<ActionType>();
    public DbSet<DailyBloom> DailyBlooms => Set<DailyBloom>();
    public DbSet<Plant> Plants => Set<Plant>();
    public DbSet<PlantNote> PlantNotes => Set<PlantNote>();
    public DbSet<Photo> Photos => Set<Photo>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        base.OnModelCreating(b);

        b.Entity<Cooperative>(e =>
        {
            e.HasIndex(x => x.InviteCode).IsUnique();
            e.Property(x => x.InviteCode).HasMaxLength(16);
            e.Property(x => x.Name).HasMaxLength(80);

            e.HasMany(x => x.Members)
                .WithOne(u => u.Cooperative!)
                .HasForeignKey(u => u.CooperativeId)
                .OnDelete(DeleteBehavior.SetNull);

            e.HasOne(x => x.Room)
                .WithOne(r => r.Cooperative)
                .HasForeignKey<RoomState>(r => r.CooperativeId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<RoomState>(e =>
        {
            e.HasKey(x => x.CooperativeId); // PK = FK (1:1)
            e.Property(x => x.LastNote).HasMaxLength(50);
            e.Property(x => x.TimeZoneId).HasMaxLength(64);
            e.Property(x => x.PendingTimeZoneId).HasMaxLength(64);
        });

        b.Entity<DailyAction>(e =>
        {
            // Una acción de cada tipo por usuario y día (extensible a varias acciones futuras).
            e.HasIndex(x => new { x.CooperativeId, x.UserId, x.ActionTypeId, x.Date }).IsUnique();

            e.HasOne(x => x.Cooperative)
                .WithMany(c => c.DailyActions)
                .HasForeignKey(x => x.CooperativeId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(x => x.ActionType)
                .WithMany()
                .HasForeignKey(x => x.ActionTypeId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        b.Entity<DailyBloom>(e =>
        {
            // Un único brote por cooperativa y día.
            e.HasIndex(x => new { x.CooperativeId, x.Date }).IsUnique();
            e.Property(x => x.Note).HasMaxLength(50);

            e.HasOne(x => x.Cooperative)
                .WithMany(c => c.Blooms)
                .HasForeignKey(x => x.CooperativeId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<Plant>(e =>
        {
            // Búsqueda frecuente de la planta ACTIVA (MaturedAt == null) de cada cooperativa.
            e.HasIndex(x => new { x.CooperativeId, x.MaturedAt });

            e.HasOne(x => x.Cooperative)
                .WithMany(c => c.Plants)
                .HasForeignKey(x => x.CooperativeId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<PlantNote>(e =>
        {
            e.HasIndex(x => x.PlantId);
            e.Property(x => x.Text).HasMaxLength(50);
            e.Property(x => x.AuthorName).HasMaxLength(64);

            e.HasOne(x => x.Plant)
                .WithMany(p => p.Notes)
                .HasForeignKey(x => x.PlantId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<Photo>(e =>
        {
            // Una foto por cooperativa y ranura.
            e.HasIndex(x => new { x.CooperativeId, x.Slot }).IsUnique();
            e.Property(x => x.ContentType).HasMaxLength(40);

            e.HasOne(x => x.Cooperative)
                .WithMany()
                .HasForeignKey(x => x.CooperativeId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<ActionType>(e =>
        {
            e.HasIndex(x => x.Code).IsUnique();
            e.Property(x => x.Code).HasMaxLength(32);
            e.Property(x => x.Name).HasMaxLength(64);

            // Catálogo inicial (extensible: añadir filas no requiere migración de esquema).
            e.HasData(
                new ActionType { Id = 1, Code = "RIEGO", Name = "Riego", RequiredRole = Role.A, Active = true, Daily = true },
                new ActionType { Id = 2, Code = "PODA", Name = "Poda", RequiredRole = Role.B, Active = true, Daily = true },
                // Acción puntual (no diaria): cura la marchitez de la planta en cultivo.
                new ActionType { Id = 3, Code = "ABONO", Name = "Abonar", RequiredRole = Role.A, Active = true, Daily = false }
            );
        });
    }
}
