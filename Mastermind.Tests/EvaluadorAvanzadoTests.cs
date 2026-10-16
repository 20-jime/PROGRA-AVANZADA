using Mastermind.Core;

// Las pruebas comparten estado estático (sesión de cheat codes, colores, logger),
// por eso se ejecutan una por una.
[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace Mastermind.Tests;

/// <summary>
/// Pruebas de los requisitos avanzados (paso 4 del flujo de trabajo).
/// </summary>
public class EvaluadorAvanzadoTests : IDisposable
{
    private readonly List<string> _log = new();

    public EvaluadorAvanzadoTests()
    {
        Evaluador.RestablecerValoresPorDefecto();
        Evaluador.Logger = _log.Add;
        Evaluador.Reloj = () => new DateTime(2026, 10, 17, 9, 5, 3);
        Evaluador.ConfigurarCheatCodes(CheatCodeService.DesdeLista(new[]
        {
            new CheatCodeInfo("MASTER-001", 3),
            new CheatCodeInfo("PISTA-777", 1),
            new CheatCodeInfo("AGOTADO-000", 0)
        }));
    }

    public void Dispose() => Evaluador.RestablecerValoresPorDefecto();

    // ------------------------------------------------------------
    // Logging
    // ------------------------------------------------------------

    [Fact]
    public void Log_UsaElFormatoDelEnunciado()
    {
        Evaluador.EvaluarIntento("RGBY", "RBYG");

        Assert.Single(_log);
        Assert.Equal(
            "En la fecha 17-10-2026 09:05:03 se evaluó el intento RBYG contra el patrón RGBY. " +
            "Resultado: 1 bien posicionados – 3 con color correcto.",
            _log[0]);
    }

    // ------------------------------------------------------------
    // Validaciones
    // ------------------------------------------------------------

    [Theory]
    [InlineData(null, "RGBY", TipoErrorEntrada.Nula)]
    [InlineData("RGBY", null, TipoErrorEntrada.Nula)]
    [InlineData("", "RGBY", TipoErrorEntrada.Vacia)]
    [InlineData("RGBY", "   ", TipoErrorEntrada.Vacia)]
    [InlineData("RGB", "RGB", TipoErrorEntrada.TamanoNoPermitido)]
    [InlineData("RGBYR", "RGBYR", TipoErrorEntrada.TamanoNoPermitido)]
    [InlineData("RGBY", "RGB", TipoErrorEntrada.LongitudesDistintas)]
    [InlineData("RGBY", "RGBYRG", TipoErrorEntrada.LongitudesDistintas)]
    [InlineData("RGBZ", "RGBY", TipoErrorEntrada.ColorInvalido)]
    [InlineData("RGBY", "RG1Y", TipoErrorEntrada.ColorInvalido)]
    public void EntradasInvalidas_LanzanExcepcionConElTipoCorrecto(
        string? secreto, string? intento, TipoErrorEntrada tipoEsperado)
    {
        var ex = Assert.Throws<EntradaInvalidaException>(
            () => Evaluador.EvaluarIntento(secreto!, intento!));

        Assert.Equal(tipoEsperado, ex.Tipo);
    }

    [Fact]
    public void EntradaInvalida_NoGeneraLog()
    {
        Assert.Throws<EntradaInvalidaException>(() => Evaluador.EvaluarIntento("RGB", "RGB"));

        Assert.Empty(_log);
    }

    [Fact]
    public void AceptaLetrasEnMinuscula()
    {
        var (bien, color) = Evaluador.EvaluarIntento("rgby", "RGBY");

        Assert.Equal(4, bien);
        Assert.Equal(0, color);
    }

    // ------------------------------------------------------------
    // Configuración dinámica de colores y tamaños
    // ------------------------------------------------------------

    [Fact]
    public void ConfigurarColores_CambiaLosColoresValidos()
    {
        ConfiguracionColores.Configurar(new Dictionary<char, string>
        {
            ['A'] = "Alpha", ['B'] = "Beta", ['C'] = "Gamma", ['D'] = "Delta"
        });

        var (bien, _) = Evaluador.EvaluarIntento("ABCD", "ABCD");
        Assert.Equal(4, bien);

        var ex = Assert.Throws<EntradaInvalidaException>(
            () => Evaluador.EvaluarIntento("RGBY", "RGBY"));
        Assert.Equal(TipoErrorEntrada.ColorInvalido, ex.Tipo);
    }

    [Fact]
    public void RestablecerColores_VuelveALosPorDefecto()
    {
        ConfiguracionColores.Configurar(new Dictionary<char, string> { ['A'] = "Alpha" });
        ConfiguracionColores.Restablecer();

        var (bien, _) = Evaluador.EvaluarIntento("RGBY", "RGBY");
        Assert.Equal(4, bien);
    }

    [Fact]
    public void ConfigurarColores_SinColores_LanzaExcepcion()
    {
        Assert.Throws<ArgumentException>(
            () => ConfiguracionColores.Configurar(new Dictionary<char, string>()));
    }

    [Fact]
    public void ConfigurarTamanos_PermiteOtrosTamanos()
    {
        ConfiguracionColores.ConfigurarTamanos(5);

        var (bien, _) = Evaluador.EvaluarIntento("RGBYR", "RGBYR");
        Assert.Equal(5, bien);

        var ex = Assert.Throws<EntradaInvalidaException>(
            () => Evaluador.EvaluarIntento("RGBY", "RGBY"));
        Assert.Equal(TipoErrorEntrada.TamanoNoPermitido, ex.Tipo);
    }

    [Fact]
    public void CargarColoresDesdeArchivoJson()
    {
        string ruta = Path.Combine(Path.GetTempPath(), $"colores-{Guid.NewGuid():N}.json");
        File.WriteAllText(ruta, "{ \"colores\": { \"A\": \"Alpha\", \"B\": \"Beta\" } }");
        try
        {
            ConfiguracionColores.CargarDesdeArchivo(ruta);

            Assert.True(ConfiguracionColores.EsColorValido('A'));
            Assert.False(ConfiguracionColores.EsColorValido('R'));
        }
        finally
        {
            File.Delete(ruta);
        }
    }

    // ------------------------------------------------------------
    // Cheat code
    // ------------------------------------------------------------

    [Fact]
    public void CheatValido_RevelaUnaPosicionDelSecreto()
    {
        Evaluador.GeneradorAleatorio = _ => 1;   // posición 2 (índice 1)

        var r = Evaluador.EvaluarIntento("RGBY", "RRRR", "MASTER-001");

        Assert.Equal(EstadoCheat.Aplicado, r.EstadoCheat);
        Assert.Equal(2, r.PosicionRevelada);
        Assert.Equal('G', r.ColorRevelado);
        Assert.Contains("posición 2", r.MensajeCheat);
    }

    [Fact]
    public void CheatValido_NoAlteraElResultadoDeLaEvaluacion()
    {
        Evaluador.GeneradorAleatorio = _ => 1;

        var (bien, color) = Evaluador.EvaluarIntento("RGBY", "RRRR", "MASTER-001");

        Assert.Equal(1, bien);
        Assert.Equal(0, color);
    }

    [Fact]
    public void CheatInutil_PosicionYaAcertada_DiceSinSuerte()
    {
        Evaluador.GeneradorAleatorio = _ => 0;   // la R de la posición 1 ya está acertada

        var r = Evaluador.EvaluarIntento("RGBY", "RBYG", "MASTER-001");

        Assert.Equal(EstadoCheat.SinSuerte, r.EstadoCheat);
        Assert.Equal("sin suerte", r.MensajeCheat);
        Assert.Null(r.ColorRevelado);
    }

    [Fact]
    public void CheatInutil_SiempreSeConsume()
    {
        Evaluador.GeneradorAleatorio = _ => 0;
        Evaluador.EvaluarIntento("RGBY", "RBYG", "MASTER-001");   // sin suerte, pero se consume

        var r = Evaluador.EvaluarIntento("RGBY", "RBYG", "MASTER-001");

        Assert.Equal(EstadoCheat.YaUsadoEnPartida, r.EstadoCheat);
    }

    [Fact]
    public void CheatRepetido_EnLaMismaPartida_SeRechaza()
    {
        Evaluador.GeneradorAleatorio = _ => 1;
        Evaluador.EvaluarIntento("RGBY", "RRRR", "MASTER-001");

        var mismoCodigo = Evaluador.EvaluarIntento("RGBY", "RRRR", "MASTER-001");
        var otroCodigo = Evaluador.EvaluarIntento("RGBY", "RRRR", "PISTA-777");

        Assert.Equal(EstadoCheat.YaUsadoEnPartida, mismoCodigo.EstadoCheat);
        Assert.Equal(EstadoCheat.YaUsadoEnPartida, otroCodigo.EstadoCheat);
        Assert.Null(mismoCodigo.PosicionRevelada);
    }

    [Fact]
    public void NuevaPartida_PermiteUsarOtroCheat()
    {
        Evaluador.GeneradorAleatorio = _ => 1;
        Evaluador.EvaluarIntento("RGBY", "RRRR", "MASTER-001");

        Evaluador.NuevaPartida();
        var r = Evaluador.EvaluarIntento("RGBY", "RRRR", "MASTER-001");

        Assert.Equal(EstadoCheat.Aplicado, r.EstadoCheat);
    }

    [Fact]
    public void CheatSinIntentosDisponibles_SeAgotaEntrePartidas()
    {
        Evaluador.GeneradorAleatorio = _ => 1;
        Evaluador.EvaluarIntento("RGBY", "RRRR", "PISTA-777");   // único uso permitido

        Evaluador.NuevaPartida();
        var r = Evaluador.EvaluarIntento("RGBY", "RRRR", "PISTA-777");

        Assert.Equal(EstadoCheat.SinIntentosDisponibles, r.EstadoCheat);
    }

    [Fact]
    public void CheatConCeroIntentos_SeRechazaYNoGastaLaPartida()
    {
        Evaluador.GeneradorAleatorio = _ => 1;

        var agotado = Evaluador.EvaluarIntento("RGBY", "RRRR", "AGOTADO-000");
        var valido = Evaluador.EvaluarIntento("RGBY", "RRRR", "MASTER-001");

        Assert.Equal(EstadoCheat.SinIntentosDisponibles, agotado.EstadoCheat);
        Assert.Equal(EstadoCheat.Aplicado, valido.EstadoCheat);
    }

    [Fact]
    public void CheatInexistente_SeRechazaYNoSeConsume()
    {
        Evaluador.GeneradorAleatorio = _ => 1;

        var falso = Evaluador.EvaluarIntento("RGBY", "RRRR", "NO-EXISTE");
        var valido = Evaluador.EvaluarIntento("RGBY", "RRRR", "MASTER-001");

        Assert.Equal(EstadoCheat.CodigoInexistente, falso.EstadoCheat);
        Assert.Equal(EstadoCheat.Aplicado, valido.EstadoCheat);
    }

    [Fact]
    public void CheatCode_NoDistingueMayusculasDeMinusculas()
    {
        Evaluador.GeneradorAleatorio = _ => 1;

        var r = Evaluador.EvaluarIntento("RGBY", "RRRR", "master-001");

        Assert.Equal(EstadoCheat.Aplicado, r.EstadoCheat);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void SinCheatCode_EstadoNoSolicitado(string? cheat)
    {
        var r = Evaluador.EvaluarIntento("RGBY", "RBYG", cheat);

        Assert.Equal(EstadoCheat.NoSolicitado, r.EstadoCheat);
    }

    [Fact]
    public void UsoDeCheat_QuedaRegistradoEnElLog()
    {
        Evaluador.GeneradorAleatorio = _ => 1;

        Evaluador.EvaluarIntento("RGBY", "RRRR", "MASTER-001");

        Assert.Equal(2, _log.Count);
        Assert.Contains("cheat code", _log[1]);
    }

    [Fact]
    public void ArchivoJsonDeCheatCodes_SeCargaCorrectamente()
    {
        string ruta = Path.Combine(AppContext.BaseDirectory, "CheatCodes.json");

        var servicio = CheatCodeService.DesdeArchivo(ruta);

        Assert.True(servicio.Existe("MASTER-001"));
        Assert.True(servicio.TieneIntentosDisponibles("MASTER-001"));
        Assert.False(servicio.TieneIntentosDisponibles("AGOTADO-000"));
        Assert.False(servicio.Existe("NO-EXISTE"));
    }
}
