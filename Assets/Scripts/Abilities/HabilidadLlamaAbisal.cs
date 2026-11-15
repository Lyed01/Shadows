using Unity.Cinemachine;
using UnityEngine;

/// <summary>
/// Suelta la llama del abismo: el jugador queda quieto y la camara pasa a
/// seguir a la llama hasta que termina.
/// </summary>
public class HabilidadLlamaAbisal : IHabilidad
{
    private const int Costo = 2;
    /// <summary>Una celda: la llama aparece en la de adelante.</summary>
    private const float DistanciaAdelante = 1f;

    public AbilityType Tipo => AbilityType.AbyssFlame;

    public void UsarPrincipal(Jugador jugador)
    {
        if (jugador == null || jugador.gridManager == null)
        {
            Log.Aviso(typeof(HabilidadLlamaAbisal), "faltan referencias");
            return;
        }

        HUDHabilidad hud = jugador.hudHabilidad;
        if (hud != null && !hud.TieneCargas(Costo))
        {
            hud.MostrarAviso("Energía insuficiente.");
            return;
        }

        GameObject prefab = AbilityManager.Instance?.prefabAbyssFlame;
        if (prefab == null)
        {
            Log.Aviso(typeof(HabilidadLlamaAbisal), "prefabAbyssFlame no asignado en AbilityManager.");
            return;
        }

        jugador.SetInputBloqueado(true);
        jugador.SetControlActivo(false);

        GameObject llama = Object.Instantiate(prefab, jugador.transform.position, Quaternion.identity);

        AbyssFlame componente = llama.GetComponent<AbyssFlame>();
        if (componente == null)
        {
            Log.Aviso(typeof(HabilidadLlamaAbisal), $"El prefab {prefab.name} no tiene AbyssFlame.");
            Object.Destroy(llama);
            jugador.SetInputBloqueado(false);
            jugador.SetControlActivo(true);
            return;
        }

        // Nace delante de Shawy, hacia donde mira, y no encima de el.
        componente.Adelantar(jugador.DireccionMirada, DistanciaAdelante, jugador.GetComponent<Collider2D>());

        CinemachineCamera camara = Object.FindFirstObjectByType<CinemachineCamera>();
        if (camara != null)
            camara.Follow = llama.transform;

        componente.Inicializar(jugador);
        hud?.UsarCargas(Costo);

        AbilityManager.NotificarUso();
    }

    public void UsarSecundaria(Jugador jugador) { }
}
