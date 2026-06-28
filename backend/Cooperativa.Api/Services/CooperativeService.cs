using Cooperativa.Domain;
using Cooperativa.Domain.Entities;
using Cooperativa.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Cooperativa.Api.Services;

public class CooperativeService
{
    private readonly AppDbContext _db;
    public CooperativeService(AppDbContext db) => _db = db;

    /// <summary>Crea la cooperativa (estado Pendiente) y asigna al creador el rol A (Riego).</summary>
    public async Task<(Guid CooperativeId, string InviteCode)?> CreateAsync(Guid userId, string? name)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user is null || user.CooperativeId is not null) return null; // ya pertenece a una

        var coop = new Cooperative
        {
            Name = name,
            InviteCode = GenerateCode(),
            Status = CooperativeStatus.Pending
        };
        coop.Room = new RoomState { CooperativeId = coop.Id };
        // Primera planta en cultivo (eje crecimiento 0..10 + salud).
        coop.Plants.Add(new Plant
        {
            CooperativeId = coop.Id,
            Species = (Species)Random.Shared.Next(2),
            Seed = Random.Shared.Next(),
        });
        _db.Cooperatives.Add(coop);

        user.CooperativeId = coop.Id;
        user.Role = Role.A;
        await _db.SaveChangesAsync();

        return (coop.Id, coop.InviteCode);
    }

    /// <summary>Une al usuario con código de invitación y asigna rol B (Poda). Cierra el vínculo 1:1.</summary>
    public async Task<bool> JoinAsync(Guid userId, string code)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user is null || user.CooperativeId is not null) return false;

        var coop = await _db.Cooperatives
            .Include(c => c.Members)
            .FirstOrDefaultAsync(c => c.InviteCode == code);
        if (coop is null || coop.Status == CooperativeStatus.Complete || coop.Members.Count >= 2)
            return false;

        user.CooperativeId = coop.Id;
        user.Role = Role.B;
        coop.Status = CooperativeStatus.Complete;
        await _db.SaveChangesAsync();

        return true;
    }

    // Código legible (sin caracteres ambiguos como 0/O, 1/I).
    private static string GenerateCode()
    {
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        var rnd = Random.Shared;
        return new string(Enumerable.Range(0, 6).Select(_ => chars[rnd.Next(chars.Length)]).ToArray());
    }
}
