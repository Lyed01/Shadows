using UnityEngine;
using System.Collections;

public static class ShadowTpAbility
{
    public static void Teletransportar(Jugador jugador)
    {
        if (jugador == null || jugador.gridManager == null || jugador.hudHabilidad == null)
        {
            Debug.LogWarning("⚠️ ShadowTpAbility: faltan referencias necesarias.");
            return;
        }

        HUDHabilidad hud = jugador.hudHabilidad;

        // === Verificar cargas ===
        if (!hud.TieneCargas())
        {
            hud.MostrarAviso("Sin energía");
            return;
        }

        // === Obtener posición del mouse ===
        Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mouseWorld.z = 0;

        GridManager grid = jugador.gridManager;
        Vector3Int cell = grid.sueloTilemap.WorldToCell(mouseWorld);

        // === Validar celda ===
        bool esValida = grid.EsCeldaColocable(cell, jugador.transform.position, jugador.rangoHabilidad);

        if (!esValida)
        {
            hud.MostrarAviso("No puedes teletransportarte ahí");
            return;
        }

        // === Ejecutar teletransporte con animaciones ===
        jugador.StartCoroutine(EjecutarTeleportConAnimaciones(jugador, grid, cell));
    }

    private static IEnumerator EjecutarTeleportConAnimaciones(Jugador jugador, GridManager grid, Vector3Int cellDestino)
    {
        Animator anim = jugador.GetComponent<Animator>();
        HUDHabilidad hud = jugador.hudHabilidad;
        AudioManager.Instance?.ReproducirTeleport();

        jugador.SetInputBloqueado(true);

        // 🔹 Fase 1 — Desaparecer
        if (anim != null)
        {
            anim.Play("Teleport_Disappear");
            yield return new WaitForSeconds(0.25f);
        }

        // 🔹 Fase 2 — Mover al destino
        Vector3 destino = grid.sueloTilemap.GetCellCenterWorld(cellDestino);
        jugador.transform.position = destino;

        // 🔹 Fase 3 — Aparecer
        if (anim != null)
        {
            anim.Play("Teleport_Appear");
            yield return new WaitForSeconds(0.25f);
        }

        // 🔹 Fase 4 — Finalizar
        jugador.SetInputBloqueado(false);
        hud.UsarCargas(1);
        AbilityManager.OnUsarHabilidad?.Invoke();

        jugador.SendMessage("DesactivarModoHabilidad", SendMessageOptions.DontRequireReceiver);
        Debug.Log("✨ Teletransporte completado correctamente");
    }
}
