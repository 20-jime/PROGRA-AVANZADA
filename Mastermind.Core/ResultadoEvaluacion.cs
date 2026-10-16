namespace Mastermind.Core;

public enum EstadoCheat
{
    NoSolicitado,
    Aplicado,
    SinSuerte,
    CodigoInexistente,
    YaUsadoEnPartida,
    SinIntentosDisponibles
}

/// <summary>
/// Resultado de evaluar un intento. Se puede deconstruir como
/// (bienPosicionados, colorCorrecto) igual que en el algoritmo básico.
/// </summary>
public record ResultadoEvaluacion(
    int BienPosicionados,
    int ColorCorrecto,
    EstadoCheat EstadoCheat = EstadoCheat.NoSolicitado,
    int? PosicionRevelada = null,   // 1 = primera posición
    char? ColorRevelado = null,
    string? MensajeCheat = null)
{
    public void Deconstruct(out int bienPosicionados, out int colorCorrecto)
    {
        bienPosicionados = BienPosicionados;
        colorCorrecto = ColorCorrecto;
    }
}
