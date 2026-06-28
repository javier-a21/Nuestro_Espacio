namespace Cooperativa.Domain;

/// <summary>
/// Máquina de los 5 estados de la planta. Lógica pura y sin dependencias,
/// reutilizable tanto por el job de Hangfire como por los tests.
/// </summary>
public static class PlantEvaluator
{
    /// <param name="currentStreak">Racha actual de días consecutivos exitosos.</param>
    /// <param name="bothActedToday">¿Han completado ambos su acción hoy?</param>
    /// <param name="hoursSinceInteraction">Horas desde la última interacción de cualquiera.</param>
    public static PlantLevel Evaluate(int currentStreak, bool bothActedToday, double hoursSinceInteraction)
    {
        // La decadencia por inactividad tiene prioridad sobre la racha.
        if (hoursSinceInteraction >= 48) return PlantLevel.Withered;   // Marchita
        if (hoursSinceInteraction >= 24) return PlantLevel.Drooping;   // Alicaída

        // Con interacción reciente (<24h):
        if (currentStreak >= 7 && bothActedToday) return PlantLevel.Radiant; // Radiante
        if (currentStreak >= 1) return PlantLevel.Healthy;                    // Sana
        return PlantLevel.Stable;                                            // Estable
    }
}
