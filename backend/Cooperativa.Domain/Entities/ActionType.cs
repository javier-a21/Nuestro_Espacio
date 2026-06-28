namespace Cooperativa.Domain.Entities;

/// <summary>
/// Catálogo extensible de tipos de acción. Empezamos con RIEGO y PODA;
/// añadir una nueva acción en el futuro = insertar una fila (sin migración de esquema).
/// </summary>
public class ActionType
{
    public int Id { get; set; }
    public string Code { get; set; } = default!;  // RIEGO, PODA, ABONO, ...
    public string Name { get; set; } = default!;
    public Role RequiredRole { get; set; }        // qué rol puede ejecutarla
    public bool Active { get; set; } = true;

    /// <summary>
    /// True = acción del ritual diario (cuenta para el crecimiento, una por día y rol).
    /// False = acción puntual fuera del ritual (p. ej. ABONAR para curar la marchitez).
    /// </summary>
    public bool Daily { get; set; } = true;
}
