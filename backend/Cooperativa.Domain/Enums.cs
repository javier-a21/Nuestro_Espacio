namespace Cooperativa.Domain;

/// <summary>Rol asimétrico e intransferible dentro de la cooperativa.</summary>
public enum Role
{
    A = 0, // Riego
    B = 1  // Poda
}

/// <summary>Estado del emparejamiento 1:1.</summary>
public enum CooperativeStatus
{
    Pending = 0,  // esperando al segundo miembro
    Complete = 1  // dos miembros vinculados
}

/// <summary>Clima escriptado (opción B): lo controla la app, no una API real.</summary>
public enum WeatherType
{
    Clear = 0,   // Despejado
    Cloudy = 1,  // Nublado
    Rain = 2,    // Lluvia
    Sunny = 3    // Sol
}

/// <summary>Los 5 estados de salud de la planta (valor entero 1..5).</summary>
public enum PlantLevel
{
    Withered = 1, // Marchita  — 48h+ sin interacción
    Drooping = 2, // Alicaída  — 24h sin interacción
    Stable = 3,   // Estable   — primer día tras racha rota
    Healthy = 4,  // Sana      — racha 1..6
    Radiant = 5   // Radiante  — racha >=7 y ambas acciones hoy
}

/// <summary>Especie de planta que se cultiva (2 ahora, ampliable en el futuro).</summary>
public enum Species
{
    Rosa = 0,
    Girasol = 1
}

/// <summary>
/// Eje de SALUD de la planta en cultivo, independiente del crecimiento.
/// El descuido o un evento la marchitan; "abonar" la devuelve a Sana.
/// </summary>
public enum PlantHealth
{
    Healthy = 0,  // Sana
    Wilting = 1,  // Marchitándose
    Wilted = 2    // Marchita (el crecimiento se pausa hasta abonar)
}

/// <summary>Evento climático escriptado que puede dañar la planta si no se protege.</summary>
public enum WeatherEventType
{
    None = 0,
    Heatwave = 1, // Ola de calor → dar sombra / retirar del sol
    ColdSnap = 2  // Helada / frío → cubrir / tapar
}

/// <summary>Fase del evento: se AVISA antes (pronóstico) y luego GOLPEA.</summary>
public enum WeatherEventStage
{
    None = 0,
    Forecast = 1, // pronóstico: hay margen para prepararse
    Active = 2    // en curso: si no está protegida, la planta sufre
}
