using Cooperativa.Domain;
using Xunit;

namespace Cooperativa.Domain.Tests;

public class PlantEvaluatorTests
{
    [Fact]
    public void Marchita_cuando_48h_o_mas_sin_interaccion()
    {
        Assert.Equal(PlantLevel.Withered, PlantEvaluator.Evaluate(currentStreak: 10, bothActedToday: true, hoursSinceInteraction: 48));
        Assert.Equal(PlantLevel.Withered, PlantEvaluator.Evaluate(0, false, 100));
    }

    [Fact]
    public void Alicaida_entre_24h_y_48h()
    {
        Assert.Equal(PlantLevel.Drooping, PlantEvaluator.Evaluate(5, true, 24));
        Assert.Equal(PlantLevel.Drooping, PlantEvaluator.Evaluate(5, true, 47.9));
    }

    [Fact]
    public void Estable_cuando_reciente_y_sin_racha()
    {
        Assert.Equal(PlantLevel.Stable, PlantEvaluator.Evaluate(0, false, 1));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(6)]
    public void Sana_cuando_racha_entre_1_y_6(int streak)
    {
        Assert.Equal(PlantLevel.Healthy, PlantEvaluator.Evaluate(streak, false, 2));
    }

    [Fact]
    public void Radiante_cuando_racha_7_o_mas_y_ambos_actuaron_hoy()
    {
        Assert.Equal(PlantLevel.Radiant, PlantEvaluator.Evaluate(7, bothActedToday: true, hoursSinceInteraction: 1));
        Assert.Equal(PlantLevel.Radiant, PlantEvaluator.Evaluate(30, true, 0));
    }

    [Fact]
    public void Racha_alta_pero_falta_una_accion_es_Sana()
    {
        Assert.Equal(PlantLevel.Healthy, PlantEvaluator.Evaluate(9, bothActedToday: false, hoursSinceInteraction: 3));
    }

    [Fact]
    public void La_decadencia_tiene_prioridad_sobre_la_racha()
    {
        Assert.Equal(PlantLevel.Withered, PlantEvaluator.Evaluate(20, true, 50)); // racha alta, 50h => marchita
        Assert.Equal(PlantLevel.Drooping, PlantEvaluator.Evaluate(20, true, 30)); // racha alta, 30h => alicaída
    }
}
