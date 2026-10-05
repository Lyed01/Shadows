using Unity.Cinemachine;
using UnityEngine;

/// <summary>
/// Suelta la llama del abismo: el jugador queda quieto y la camara pasa a
/// seguir a la llama hasta que termina.
/// </summary>
public class HabilidadLlamaAbisal : IHabilidad
{
    private const int Costo = 2;

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

        CinemachineCamera camara = Object.FindFirstObjectByType<CinemachineCamera>();
        if (camara != null)
            camara.Follow = llama.transform;

        componente.Inicializar(jugador);
        hud?.UsarCargas(Costo);

        AbilityManager.OnUsarHabilidad?.Invoke();
    }

    public void UsarSecundaria(Jugador jugador) { }
}
