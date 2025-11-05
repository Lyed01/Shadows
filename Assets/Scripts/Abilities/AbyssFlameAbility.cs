using UnityEngine;
using Unity.Cinemachine;

public static class AbyssFlameAbility
{
    public static void Lanzar(Jugador jugador)
    {
        if (jugador == null || jugador.gridManager == null)
        {
            Debug.LogWarning("⚠️ AbyssFlameAbility: faltan referencias");
            return;
        }

        var hud = jugador.hudHabilidad;
        if (hud != null && !hud.TieneCargas(2))
        {
            hud.MostrarAviso("Energía insuficiente.");
            return;
        }

        var prefab = AbilityManager.Instance?.prefabAbyssFlame;
        if (prefab == null)
        {
            Debug.LogError("❌ AbyssFlameAbility: prefab no asignado en AbilityManager");
            return;
        }

        jugador.SetInputBloqueado(true);
        jugador.SetControlActivo(false);

        Vector3 spawnPos = jugador.transform.position;
        GameObject flame = Object.Instantiate(prefab, spawnPos, Quaternion.identity);

        var flameComp = flame.GetComponent<AbyssFlame>();
        if (flameComp == null)
        {
            Debug.LogError("❌ AbyssFlameAbility: el prefab no tiene AbyssFlame.cs");
            jugador.SetInputBloqueado(false);
            jugador.SetControlActivo(true);
            return;
        }

        var cineCam = Object.FindFirstObjectByType<CinemachineCamera>();
        if (cineCam != null)
            cineCam.Follow = flame.transform;

        flameComp.Inicializar(jugador);
        hud?.UsarCargas(2);

        AbilityManager.OnUsarHabilidad?.Invoke(); // ✅ Notifica el uso
    }
}
