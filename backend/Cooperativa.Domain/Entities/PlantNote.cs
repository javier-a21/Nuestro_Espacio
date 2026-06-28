namespace Cooperativa.Domain.Entities;

/// <summary>
/// Una nota dejada por la pareja MIENTRAS criaba una planta concreta. Se conservan todas
/// (a diferencia de la nota fugaz de la sala, que solo guarda la última) para formar el
/// "librito de notas" de la ficha de cada flor.
/// </summary>
public class PlantNote
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid PlantId { get; set; }
    public Plant Plant { get; set; } = default!;

    public Guid CooperativeId { get; set; }

    public Guid AuthorId { get; set; }
    public string AuthorName { get; set; } = default!;

    public string Text { get; set; } = default!;

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
