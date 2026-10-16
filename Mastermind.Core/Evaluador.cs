namespace Mastermind.Core;

public static class Evaluador
{
    /// <summary>
    /// Compara el intento del jugador contra el patrón secreto.
    /// Primero cuenta coincidencias exactas y luego colores correctos
    /// en posición incorrecta, sin contar dos veces la misma ficha.
    /// </summary>
    public static (int bienPosicionados, int colorCorrecto) EvaluarIntento(
        string patronSecreto, string intentoJugador)
    {
        int bien = 0;
        var sobrantesSecreto = new Dictionary<char, int>();
        var sobrantesIntento = new Dictionary<char, int>();

        for (int i = 0; i < patronSecreto.Length; i++)
        {
            if (patronSecreto[i] == intentoJugador[i])
            {
                bien++;
            }
            else
            {
                sobrantesSecreto[patronSecreto[i]] = sobrantesSecreto.GetValueOrDefault(patronSecreto[i]) + 1;
                sobrantesIntento[intentoJugador[i]] = sobrantesIntento.GetValueOrDefault(intentoJugador[i]) + 1;
            }
        }

        int colorCorrecto = 0;
        foreach (var (color, cantidad) in sobrantesIntento)
        {
            if (sobrantesSecreto.TryGetValue(color, out int enSecreto))
                colorCorrecto += Math.Min(cantidad, enSecreto);
        }

        return (bien, colorCorrecto);
    }
}
