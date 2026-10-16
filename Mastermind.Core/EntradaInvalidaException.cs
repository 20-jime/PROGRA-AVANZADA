namespace Mastermind.Core;

public enum TipoErrorEntrada
{
    Nula,
    Vacia,
    TamanoNoPermitido,
    LongitudesDistintas,
    ColorInvalido
}

/// <summary>Se lanza cuando el patrón secreto o el intento no cumplen las reglas.</summary>
public class EntradaInvalidaException : ArgumentException
{
    public TipoErrorEntrada Tipo { get; }

    public EntradaInvalidaException(TipoErrorEntrada tipo, string mensaje, string? nombreParametro = null)
        : base(mensaje, nombreParametro)
    {
        Tipo = tipo;
    }
}
