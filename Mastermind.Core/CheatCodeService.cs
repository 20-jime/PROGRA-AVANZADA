using System.Text.Json;

namespace Mastermind.Core;

public record CheatCodeInfo(string Hash, int UsosDisponibles);

/// <summary>
/// Administra los cheat codes definidos en el archivo JSON y cuántas veces
/// se ha usado cada uno durante la sesión.
/// </summary>
public class CheatCodeService
{
    private readonly Dictionary<string, int> _disponibles = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, int> _usos = new(StringComparer.OrdinalIgnoreCase);

    public static CheatCodeService DesdeLista(IEnumerable<CheatCodeInfo> codigos)
    {
        var servicio = new CheatCodeService();
        foreach (var c in codigos)
            servicio._disponibles[c.Hash] = c.UsosDisponibles;
        return servicio;
    }

    /// <summary>
    /// Carga un JSON como el del enunciado:
    /// [ { "hash": "A1B2C3D4E5F6G7H8I9J0ABCDE", "usosDisponibles": 1 } ]
    /// </summary>
    public static CheatCodeService DesdeArchivo(string ruta)
    {
        var json = File.ReadAllText(ruta);
        var datos = JsonSerializer.Deserialize<List<CheatCodeInfo>>(json,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        return DesdeLista(datos ?? new List<CheatCodeInfo>());
    }

    public bool Existe(string codigo) => _disponibles.ContainsKey(codigo);

    public int UsosRealizados(string codigo) => _usos.GetValueOrDefault(codigo);

    public bool TieneIntentosDisponibles(string codigo) =>
        _disponibles.TryGetValue(codigo, out int total) && UsosRealizados(codigo) < total;

    public void Consumir(string codigo) => _usos[codigo] = UsosRealizados(codigo) + 1;

    public void ReiniciarSesion() => _usos.Clear();
}
