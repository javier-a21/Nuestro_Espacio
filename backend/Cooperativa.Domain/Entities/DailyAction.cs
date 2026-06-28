namespace Cooperativa.Domain.Entities;

/// <summary>
/// Registro de auditoría que valida el cumplimiento del ritual diario.
/// La fecha (sin hora) actúa de índice para comprobar el "día actual".
/// </summary>
public class DailyAction
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid CooperativeId { get; set; }
    public Cooperative Cooperative { get; set; } = default!;

    public Guid UserId { get; set; }
    public AppUser User { get; set; } = default!;

    public int ActionTypeId { get; set; }
    public ActionType ActionType { get; set; } = default!;

    /// <summary>Día (en el huso compartido) en que se ejecutó la acción.</summary>
    public DateOnly Date { get; set; }
}
