namespace Cooperativa.Domain.Entities;

/// <summary>
/// Una planta que la cooperativa cultiva. Tiene DOS ejes independientes:
///  - Crecimiento (<see cref="GrowthStage"/> 0..<see cref="MaxStage"/>): solo sube, con cada
///    día en que AMBOS cumplen. Es progreso permanente.
///  - Salud (<see cref="Health"/>): estado temporal; el descuido o un evento la marchitan y
///    "abonar" la recupera. Mientras no está Sana, el crecimiento se pausa.
/// Cuando alcanza la fase máxima, "madura" (<see cref="MaturedAt"/>) y pasa al estante/invernadero
/// como trofeo permanente; entonces nace una nueva planta activa.
/// </summary>
public class Plant
{
    /// <summary>Fase máxima: al alcanzarla la planta madura.</summary>
    public const int MaxStage = 10;

    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid CooperativeId { get; set; }
    public Cooperative Cooperative { get; set; } = default!;

    public Species Species { get; set; }

    /// <summary>Semilla para la variedad visual (matiz/forma).</summary>
    public int Seed { get; set; }

    /// <summary>Fase de crecimiento actual (0..<see cref="MaxStage"/>).</summary>
    public int GrowthStage { get; set; }

    public PlantHealth Health { get; set; } = PlantHealth.Healthy;

    /// <summary>Acciones acumuladas durante su crianza (stat de la ficha).</summary>
    public int ActionsCount { get; set; }

    /// <summary>Notas dejadas durante su crianza (stat de la ficha).</summary>
    public int NotesCount { get; set; }

    public DateTimeOffset StartedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>Null = en crecimiento (la planta ACTIVA). Con valor = madura (en el estante).</summary>
    public DateTimeOffset? MaturedAt { get; set; }

    public ICollection<PlantNote> Notes { get; set; } = new List<PlantNote>();
}
