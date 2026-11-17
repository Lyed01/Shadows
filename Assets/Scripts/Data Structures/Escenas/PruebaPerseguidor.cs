using System.Collections;
using UnityEngine;

/// <summary>
/// Prepara la escena PruebaPerseguidor (TP10): aunque corra junto a CoreManagers,
/// nadie inicializa al jugador ni desbloquea celdas. Con esto se puede probar
/// que NPCPerseguidor rodea obstaculos: clic izquierdo pone un bloque de sombra
/// en la celda del mouse y clic derecho deshace el ultimo.
/// </summary>
[DefaultExecutionOrder(-1000)]
public class PruebaPerseguidor : MonoBehaviour
{
    public GridManager grid;
    public Jugador jugador;

    IEnumerator Start()
    {
        // Si el Play arranca por CoreManagers, su BootstrapLoader salta al menu y
        // pisa esta escena. El orden de ejecucion -1000 hace que este Start corra
        // antes que el suyo, y desactivado ya no corre.
        var bootstrap = FindFirstObjectByType<BootstrapLoader>();
        if (bootstrap != null) bootstrap.enabled = false;

        // GridManager arma sus celdas en su propio Start.
        yield return null;

        jugador.Inicializar(grid, HUDHabilidad.Instance);

        var suelo = grid.sueloTilemap;
        suelo.CompressBounds();
        foreach (var pos in suelo.cellBounds.allPositionsWithin)
            if (suelo.HasTile(pos)) grid.CorromperCeldaUnica(pos);
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Vector3 mundo = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            mundo.z = 0f;
            grid.IntentarColocarBloque(mundo, ShadowBlock.IdSombra, jugador.transform.position, float.MaxValue);
        }
        if (Input.GetMouseButtonDown(1)) grid.DeshacerUltimoBloque();
    }
}
