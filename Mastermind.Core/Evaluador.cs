using System.Globalization;

namespace Mastermind.Core;

public static class Evaluador
{
    private static readonly object Candado = new();
    private static bool _cheatUsadoEnPartida;
    private static CheatCodeService? _cheats;

    /// <summary>Destino del log. Por defecto escribe en mastermind.log.</summary>
    public static Action<string> Logger { get; set; } = EscribirEnArchivo;

    /// <summary>Reloj usado para el log (reemplazable en pruebas).</summary>
    public static Func<DateTime> Reloj { get; set; } = () => DateTime.Now;

    /// <summary>Genera un número en [0, max). Reemplazable en pruebas.</summary>
    public static Func<int, int> GeneradorAleatorio { get; set; } = max => Random.Shared.Next(max);

    // ---------------------------------------------------------------
    // Gestión de partida / sesión
    // ---------------------------------------------------------------

    public static void ConfigurarCheatCodes(CheatCodeService servicio) => _cheats = servicio;

    /// <summary>Empieza una partida nueva: vuelve a permitir un cheat code.</summary>
    public static void NuevaPartida()
    {
        lock (Candado) { _cheatUsadoEnPartida = false; }
    }

    /// <summary>Reinicia la partida y el conteo de usos de cada cheat code.</summary>
    public static void ReiniciarSesion()
    {
        lock (Candado)
        {
            _cheatUsadoEnPartida = false;
            _cheats?.ReiniciarSesion();
        }
    }

    /// <summary>Deja todo como al inicio (útil entre pruebas).</summary>
    public static void RestablecerValoresPorDefecto()
    {
        lock (Candado)
        {
            _cheatUsadoEnPartida = false;
            _cheats = null;
            Logger = EscribirEnArchivo;
            Reloj = () => DateTime.Now;
            GeneradorAleatorio = max => Random.Shared.Next(max);
        }
        ConfiguracionColores.Restablecer();
    }

    // ---------------------------------------------------------------
    // Evaluación
    // ---------------------------------------------------------------

    public static ResultadoEvaluacion EvaluarIntento(
        string patronSecreto, string intentoJugador, string? cheatCode = null)
    {
        Validar(patronSecreto, intentoJugador);

        string secreto = patronSecreto.ToUpperInvariant();
        string intento = intentoJugador.ToUpperInvariant();

        var (bien, color) = Calcular(secreto, intento);

        var resultado = new ResultadoEvaluacion(bien, color);

        if (!string.IsNullOrWhiteSpace(cheatCode))
            resultado = AplicarCheat(resultado, secreto, intento, cheatCode.Trim());

        RegistrarLog(secreto, intento, resultado);
        return resultado;
    }

    /// <summary>Algoritmo básico: coincidencias exactas primero, luego colores sobrantes.</summary>
    private static (int bien, int color) Calcular(string secreto, string intento)
    {
        int bien = 0;
        var sobrantesSecreto = new Dictionary<char, int>();
        var sobrantesIntento = new Dictionary<char, int>();

        for (int i = 0; i < secreto.Length; i++)
        {
            if (secreto[i] == intento[i])
            {
                bien++;
            }
            else
            {
                sobrantesSecreto[secreto[i]] = sobrantesSecreto.GetValueOrDefault(secreto[i]) + 1;
                sobrantesIntento[intento[i]] = sobrantesIntento.GetValueOrDefault(intento[i]) + 1;
            }
        }

        int colorCorrecto = 0;
        foreach (var (c, cantidad) in sobrantesIntento)
        {
            if (sobrantesSecreto.TryGetValue(c, out int enSecreto))
                colorCorrecto += Math.Min(cantidad, enSecreto);
        }

        return (bien, colorCorrecto);
    }

    // ---------------------------------------------------------------
    // Validación de entradas
    // ---------------------------------------------------------------

    private static void Validar(string? patronSecreto, string? intentoJugador)
    {
        if (patronSecreto is null)
            throw new EntradaInvalidaException(TipoErrorEntrada.Nula,
                "El patrón secreto no puede ser nulo.", nameof(patronSecreto));
        if (intentoJugador is null)
            throw new EntradaInvalidaException(TipoErrorEntrada.Nula,
                "El intento del jugador no puede ser nulo.", nameof(intentoJugador));

        if (string.IsNullOrWhiteSpace(patronSecreto))
            throw new EntradaInvalidaException(TipoErrorEntrada.Vacia,
                "El patrón secreto no puede estar vacío.", nameof(patronSecreto));
        if (string.IsNullOrWhiteSpace(intentoJugador))
            throw new EntradaInvalidaException(TipoErrorEntrada.Vacia,
                "El intento del jugador no puede estar vacío.", nameof(intentoJugador));

        if (!ConfiguracionColores.EsTamanoValido(patronSecreto.Length))
            throw new EntradaInvalidaException(TipoErrorEntrada.TamanoNoPermitido,
                $"El tamaño {patronSecreto.Length} no es válido. Permitidos: {string.Join(", ", ConfiguracionColores.TamanosValidos)}.",
                nameof(patronSecreto));

        if (intentoJugador.Length != patronSecreto.Length)
            throw new EntradaInvalidaException(TipoErrorEntrada.LongitudesDistintas,
                $"El intento tiene {intentoJugador.Length} posiciones y el patrón {patronSecreto.Length}.",
                nameof(intentoJugador));

        ValidarColores(patronSecreto, nameof(patronSecreto));
        ValidarColores(intentoJugador, nameof(intentoJugador));
    }

    private static void ValidarColores(string texto, string parametro)
    {
        foreach (char c in texto)
        {
            if (!ConfiguracionColores.EsColorValido(c))
                throw new EntradaInvalidaException(TipoErrorEntrada.ColorInvalido,
                    $"El color '{c}' no es válido.", parametro);
        }
    }

    // ---------------------------------------------------------------
    // Cheat code
    // ---------------------------------------------------------------

    private static CheatCodeService ObtenerCheats()
    {
        if (_cheats is not null) return _cheats;

        string ruta = Path.Combine(AppContext.BaseDirectory, "CheatCodes.json");
        try
        {
            _cheats = File.Exists(ruta)
                ? CheatCodeService.DesdeArchivo(ruta)
                : CheatCodeService.DesdeLista(Array.Empty<CheatCodeInfo>());
        }
        catch (Exception)
        {
            _cheats = CheatCodeService.DesdeLista(Array.Empty<CheatCodeInfo>());
        }
        return _cheats;
    }

    private static ResultadoEvaluacion AplicarCheat(
        ResultadoEvaluacion resultado, string secreto, string intento, string cheatCode)
    {
        lock (Candado)
        {
            var servicio = ObtenerCheats();

            if (!servicio.Existe(cheatCode))
                return resultado with
                {
                    EstadoCheat = EstadoCheat.CodigoInexistente,
                    MensajeCheat = "Cheat code inválido."
                };

            if (_cheatUsadoEnPartida)
                return resultado with
                {
                    EstadoCheat = EstadoCheat.YaUsadoEnPartida,
                    MensajeCheat = "Ya usaste un cheat code en esta partida."
                };

            if (!servicio.TieneIntentosDisponibles(cheatCode))
                return resultado with
                {
                    EstadoCheat = EstadoCheat.SinIntentosDisponibles,
                    MensajeCheat = "Este cheat code ya no tiene intentos disponibles."
                };

            // El cheat válido siempre se consume, haya suerte o no.
            servicio.Consumir(cheatCode);
            _cheatUsadoEnPartida = true;

            int posicion = GeneradorAleatorio(secreto.Length);

            if (intento[posicion] == secreto[posicion])
                return resultado with
                {
                    EstadoCheat = EstadoCheat.SinSuerte,
                    PosicionRevelada = posicion + 1,
                    MensajeCheat = "sin suerte"
                };

            char colorSecreto = secreto[posicion];
            string nombre = ConfiguracionColores.Colores.GetValueOrDefault(colorSecreto, colorSecreto.ToString());
            return resultado with
            {
                EstadoCheat = EstadoCheat.Aplicado,
                PosicionRevelada = posicion + 1,
                ColorRevelado = colorSecreto,
                MensajeCheat = $"La posición {posicion + 1} es {colorSecreto} ({nombre})."
            };
        }
    }

    // ---------------------------------------------------------------
    // Logging
    // ---------------------------------------------------------------

    private static void RegistrarLog(string secreto, string intento, ResultadoEvaluacion r)
    {
        string fecha = Reloj().ToString("dd-MM-yyyy HH:mm:ss", CultureInfo.InvariantCulture);

        Logger($"En la fecha {fecha} se evaluó el intento {intento} contra el patrón {secreto}. " +
               $"Resultado: {r.BienPosicionados} bien posicionados – {r.ColorCorrecto} con color correcto.");

        if (r.EstadoCheat != EstadoCheat.NoSolicitado)
            Logger($"En la fecha {fecha} se usó un cheat code. Estado: {r.EstadoCheat}. {r.MensajeCheat}");
    }

    private static void EscribirEnArchivo(string linea)
    {
        try
        {
            File.AppendAllText(Path.Combine(AppContext.BaseDirectory, "mastermind.log"),
                linea + Environment.NewLine);
        }
        catch (Exception)
        {
            // El log nunca debe romper la evaluación.
        }
    }
}
