using Mastermind.Core;

namespace Mastermind.Tests;

/// <summary>
/// Pruebas del algoritmo básico (paso 2 del flujo de trabajo).
/// Se usa deconstrucción (var (bien, color) = ...) para que sigan
/// funcionando aunque el tipo de retorno evolucione en los requisitos avanzados.
/// </summary>
public class EvaluadorBasicoTests
{
    [Fact]
    public void CoincidenciaCompleta_DevuelveCuatroBienPosicionados()
    {
        var (bien, color) = Evaluador.EvaluarIntento("RGBY", "RGBY");

        Assert.Equal(4, bien);
        Assert.Equal(0, color);
    }

    [Fact]
    public void CoincidenciaParcial_EjemploDelEnunciado()
    {
        // RGBY vs RBYG -> bienPosicionados = 1 [R], colorCorrecto = 3 [BYG]
        var (bien, color) = Evaluador.EvaluarIntento("RGBY", "RBYG");

        Assert.Equal(1, bien);
        Assert.Equal(3, color);
    }

    [Fact]
    public void NingunaCoincidencia_DevuelveCeroYCero()
    {
        var (bien, color) = Evaluador.EvaluarIntento("RRRR", "GGGG");

        Assert.Equal(0, bien);
        Assert.Equal(0, color);
    }

    [Theory]
    [InlineData("RRGB", "RGRR", 1, 2)]       // el intento repite R más veces que el secreto
    [InlineData("RGBY", "RRRR", 1, 0)]       // una sola R en el secreto: no se cuenta doble
    [InlineData("RRRG", "GRRR", 2, 2)]       // duplicados mezclados
    [InlineData("RGBYRG", "GRBYGR", 2, 4)]   // patrón de 6 posiciones
    public void ManejoDeDuplicados_NoCuentaDosVecesLaMismaFicha(
        string secreto, string intento, int bienEsperado, int colorEsperado)
    {
        var (bien, color) = Evaluador.EvaluarIntento(secreto, intento);

        Assert.Equal(bienEsperado, bien);
        Assert.Equal(colorEsperado, color);
    }

    [Fact]
    public void TodosLosColoresCorrectosEnPosicionIncorrecta()
    {
        var (bien, color) = Evaluador.EvaluarIntento("RGBY", "GRYB");

        Assert.Equal(0, bien);
        Assert.Equal(4, color);
    }
}
