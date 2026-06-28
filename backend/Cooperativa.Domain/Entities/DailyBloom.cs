namespace Cooperativa.Domain.Entities;

/// <summary>
/// "Brote del día": recompensa que se acuña el día en que AMBOS miembros completan
/// su acción. Queda guardado como recuerdo (con la nota del día) y, acumulados,
/// forman el ramo/álbum de la cooperativa. Uno por día como máximo.
/// </summary>
public class DailyBloom
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid CooperativeId { get; set; }
    public Cooperative Cooperative { get; set; } = default!;

    /// <summary>Día (en el huso compartido) en que floreció.</summary>
    public DateOnly Date { get; set; }

    /// <summary>Semilla aleatoria para la variedad visual de la flor (forma/matiz).</summary>
    public int Seed { get; set; }

    /// <summary>Clima de ese día: influye en el color base de la flor.</summary>
    public WeatherType Weather { get; set; }

    /// <summary>Racha alcanzada ese día: influye en el nº de pétalos.</summary>
    public int Streak { get; set; }

    /// <summary>Nota guardada como recuerdo del día (puede ser null).</summary>
    public string? Note { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
