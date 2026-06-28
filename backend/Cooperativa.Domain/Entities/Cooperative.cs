namespace Cooperativa.Domain.Entities;

/// <summary>
/// Unidad de vínculo 1:1 (la "cooperativa") y dueña de la habitación compartida.
/// </summary>
public class Cooperative
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string? Name { get; set; }

    /// <summary>Código que usa el segundo miembro para unirse.</summary>
    public string InviteCode { get; set; } = default!;

    public CooperativeStatus Status { get; set; } = CooperativeStatus.Pending;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    // Navegación
    public ICollection<AppUser> Members { get; set; } = new List<AppUser>();
    public RoomState? Room { get; set; }
    public ICollection<DailyAction> DailyActions { get; set; } = new List<DailyAction>();
    public ICollection<DailyBloom> Blooms { get; set; } = new List<DailyBloom>();
    public ICollection<Plant> Plants { get; set; } = new List<Plant>();
}
