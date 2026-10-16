using System.Text.Json;

namespace Mastermind.Core;

/// <summary>Colores y tamaños válidos. Se pueden reconfigurar en tiempo de ejecución.</summary>
public static class ConfiguracionColores
{
    private static readonly Dictionary<char, string> PorDefecto = new()
    {
        ['Y'] = "Yellow",
        ['C'] = "Cyan",
        ['K'] = "Black",
        ['P'] = "Purple",
        ['W'] = "White",
        ['M'] = "Pink",
        ['R'] = "Red",
        ['B'] = "Brown",
        ['G'] = "Green"
    };

    private static Dictionary<char, string> _colores = new(PorDefecto);
    private static HashSet<int> _tamanos = new() { 4, 6 };

    public static IReadOnlyDictionary<char, string> Colores => _colores;
    public static IReadOnlyCollection<int> TamanosValidos => _tamanos;

    public static bool EsColorValido(char color) => _colores.ContainsKey(char.ToUpperInvariant(color));
    public static bool EsTamanoValido(int tamano) => _tamanos.Contains(tamano);

    /// <summary>Reemplaza la lista de colores válidos (clave = letra, valor = nombre).</summary>
    public static void Configurar(IDictionary<char, string> colores)
    {
        ArgumentNullException.ThrowIfNull(colores);
        if (colores.Count == 0)
            throw new ArgumentException("Debe haber al menos un color configurado.", nameof(colores));

        var nuevos = new Dictionary<char, string>();
        foreach (var (letra, nombre) in colores)
            nuevos[char.ToUpperInvariant(letra)] = nombre;

        _colores = nuevos;
    }

    /// <summary>Reemplaza los tamaños de patrón permitidos.</summary>
    public static void ConfigurarTamanos(params int[] tamanos)
    {
        if (tamanos is null || tamanos.Length == 0 || tamanos.Any(t => t <= 0))
            throw new ArgumentException("Los tamaños deben ser números positivos.", nameof(tamanos));

        _tamanos = new HashSet<int>(tamanos);
    }

    /// <summary>
    /// Carga colores desde un JSON como: { "colores": { "R": "Red", "G": "Green" } }
    /// </summary>
    public static void CargarDesdeArchivo(string ruta)
    {
        var json = File.ReadAllText(ruta);
        var datos = JsonSerializer.Deserialize<ArchivoColores>(json,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        if (datos?.Colores is null)
            throw new InvalidDataException("El archivo no contiene la sección 'colores'.");

        var dic = new Dictionary<char, string>();
        foreach (var (clave, nombre) in datos.Colores)
        {
            if (clave.Length != 1)
                throw new InvalidDataException($"La clave '{clave}' debe ser una sola letra.");
            dic[clave[0]] = nombre;
        }
        Configurar(dic);
    }

    public static void Restablecer()
    {
        _colores = new Dictionary<char, string>(PorDefecto);
        _tamanos = new HashSet<int> { 4, 6 };
    }

    private record ArchivoColores(Dictionary<string, string>? Colores);
}
