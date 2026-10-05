using System.Collections;
using UnityEngine;

/// <summary>
/// Mueve al jugador a la celda que marca el cursor, si esta dentro del rango y
/// es una celda donde podria pararse. El salto se hace con una corrutina
/// porque va acompañado de las animaciones de desaparecer y aparecer.
/// </summary>
public class HabilidadTeletransporte : IHabilidad
{
    private const int Costo = 1;
    private const float DuracionFase = 0.25f;

    public AbilityType Tipo => AbilityType.ShadowTp;

    public void UsarPrincipal(Jugador jugador)
    {
        if (jugador == null || jugador.gridManager == null || jugador.hudHabilidad == null)
        {
            Log.Aviso(typeof(HabilidadTeletransporte), "faltan referencias necesarias.");
            return;
        }

        HUDHabilidad hud = jugador.hudHabilidad;

        if (!hud.TieneCargas())
        {
            hud.MostrarAviso("Sin energía");
            return;
        }

        Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mouseWorld.z = 0;

        GridManager grid = jugador.gridManager;
        Vector3Int celda = grid.sueloTilemap.WorldToCell(mouseWorld);

        if (!grid.EsCeldaColocable(celda, jugador.transform.position, jugador.rangoHabilidad))
        {
            hud.MostrarAviso("No puedes teletransportarte ahí");
            return;
        }

        jugador.StartCoroutine(Saltar(jugador, grid, celda));
    }

    public void UsarSecundaria(Jugador jugador) { }

    private static IEnumerator Saltar(Jugador jugador, GridManager grid, Vector3Int celdaDestino)
    {
        Animator anim = jugador.GetComponent<Animator>();
        HUDHabilidad hud = jugador.hudHabilidad;

        AudioManager.Instance?.ReproducirTeleport();
        jugador.SetInputBloqueado(true);

        if (anim != null)
        {
            anim.Play("Teleport_Disappear");
            yield return new WaitForSeconds(DuracionFase);
        }

        Vector3 destino = grid.sueloTilemap.GetCellCenterWorld(celdaDestino);
        destino.y += 0.8f;
        jugador.transform.position = destino;

        if (anim != null)
        {
            anim.Play("Teleport_Appear");
            yield return new WaitForSeconds(DuracionFase);
        }

        jugador.SetInputBloqueado(false);
        hud.UsarCargas(Costo);
        AbilityManager.OnUsarHabilidad?.Invoke();

        jugador.SendMessage("DesactivarModoHabilidad", SendMessageOptions.DontRequireReceiver);
    }
}
