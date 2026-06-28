namespace Cooperativa.Domain.Entities;

/// <summary>
/// Estado vivo de la habitación compartida (relación 1:1 con <see cref="Cooperative"/>).
/// </summary>
public class RoomState
{
    // PK = FK hacia Cooperative (1:1)
    public Guid CooperativeId { get; set; }
    public Cooperative Cooperative { get; set; } = default!;

    public PlantLevel PlantLevel { get; set; } = PlantLevel.Stable;
    public int CurrentStreak { get; set; }

    // Nota fugaz (sólo se guarda la última, máx 50 caracteres)
    public string? LastNote { get; set; }
    public Guid? LastNoteAuthorId { get; set; }
    public DateTimeOffset? LastNoteAt { get; set; }

    /// <summary>Huso horario COMPARTIDO (IANA, p.ej. "Europe/Madrid"). Rige reloj y racha.</summary>
    public string TimeZoneId { get; set; } = "Europe/Madrid";

    /// <summary>Cambio de huso pendiente; se aplica de noche (job retardado) y reinicia la planta.</summary>
    public string? PendingTimeZoneId { get; set; }

    /// <summary>Clima escriptado actual.</summary>
    public WeatherType Weather { get; set; } = WeatherType.Clear;

    /// <summary>Última interacción de cualquiera, para calcular la decadencia (24h/48h).</summary>
    public DateTimeOffset? LastInteractionAt { get; set; }

    /// <summary>Último día (en el huso compartido) cuya racha ya fue evaluada por el job.</summary>
    public DateOnly? LastStreakEvaluation { get; set; }

    // --- Evento climático (frío/calor) ---
    /// <summary>Evento climático actual que puede dañar la planta.</summary>
    public WeatherEventType WeatherEvent { get; set; } = WeatherEventType.None;
    public WeatherEventStage EventStage { get; set; } = WeatherEventStage.None;
    /// <summary>¿La pareja ya hizo la acción de protección (sombra/cubrir)?</summary>
    public bool EventHandled { get; set; }
    /// <summary>Inicio de la fase actual del evento (para temporizar las transiciones).</summary>
    public DateTimeOffset? EventSince { get; set; }
}
