using UnityEngine;

/// <summary>
/// Lo que una luz le hace a lo que toca, igual para el foco y la cenital.
/// El daño a los bloques queda en cada luz: se anota el impacto mas cercano
/// de cada bloque y se aplica una sola vez por frame.
/// </summary>
public static class EfectosDeLuz
{
    /// <param name="letal">Si el impacto cae en el nucleo del haz. El borde solo alumbra.</param>
    /// <param name="avisarReceptores">La cenital no activa receptores.</param>
    public static void Aplicar(Collider2D tocado, TipoLuz tipo, bool letal, bool avisarReceptores)
    {
        if (letal && tocado.TryGetComponent(out Jugador jugador))
            jugador.Matar();

        // La luz roja apaga la AbyssFlame.
        if (tipo == TipoLuz.Roja && tocado.TryGetComponent(out AbyssFlame llama))
            llama.Extinguir();

        if (avisarReceptores && tocado.TryGetComponent(out LightReceptor receptor))
            receptor.RecibirLuz(tipo);
    }
}
