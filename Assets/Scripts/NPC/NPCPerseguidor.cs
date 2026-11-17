using UnityEngine;

/// <summary>
/// NPC que camina hasta el jugador por el camino mas corto sobre la grilla
/// (TP10): GridManager arma el Graph de celdas libres y DijkstraSolver busca la
/// ruta. Como los bloques de sombra ocupan celdas, el NPC los rodea y se
/// recalcula la ruta cada tanto. Ponerlo en cualquier NPC con Rigidbody2D.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class NPCPerseguidor : MonoBehaviour
{
    public float velocidad = 2f;
    [Tooltip("Cada cuantos segundos se vuelve a buscar la ruta (los bloques cambian el mapa).")]
    public float intervaloDeRuta = 0.5f;
    [Tooltip("A que distancia del jugador se queda quieto.")]
    public float distanciaParaDetenerse = 0.6f;

    private readonly IPathfindingSolver<Vector3Int> solver = new DijkstraSolver<Vector3Int>();
    private Rigidbody2D rb;
    private GridManager grid;
    private Jugador jugador;
    private ISimpleList<Vector3Int> ruta;
    private float proximoCalculo;

    /// <summary>Celdas de la ruta actual hasta el jugador, o -1 si no hay camino. Para el panel de depuracion.</summary>
    public int LargoDeRuta => ruta == null ? -1 : ruta.Count;

    void Awake() => rb = GetComponent<Rigidbody2D>();

    // Dibuja la ruta actual en la vista Scene (y en Game con Gizmos activado).
    void OnDrawGizmos()
    {
        if (ruta == null || grid == null || grid.sueloTilemap == null) return;
        Gizmos.color = Color.cyan;
        Vector3 anterior = grid.sueloTilemap.GetCellCenterWorld(ruta[0]);
        for (int i = 1; i < ruta.Count; i++)
        {
            Vector3 actual = grid.sueloTilemap.GetCellCenterWorld(ruta[i]);
            Gizmos.DrawLine(anterior, actual);
            anterior = actual;
        }
    }

    void FixedUpdate()
    {
        if (grid == null) grid = FindFirstObjectByType<GridManager>();
        if (jugador == null) jugador = FindFirstObjectByType<Jugador>();
        if (grid == null || jugador == null || grid.sueloTilemap == null) return;

        if (Time.time >= proximoCalculo)
        {
            proximoCalculo = Time.time + intervaloDeRuta;
            var inicio = grid.sueloTilemap.WorldToCell(transform.position);
            var meta = grid.sueloTilemap.WorldToCell(jugador.transform.position);
            ruta = solver.FindPath(grid.ConstruirGrafo(), inicio, meta);
        }

        if (ruta == null || ruta.Count < 2) return;
        if (Vector2.Distance(transform.position, jugador.transform.position) <= distanciaParaDetenerse) return;

        // ruta[0] es la celda donde esta; la siguiente es el proximo paso.
        Vector2 destino = grid.sueloTilemap.GetCellCenterWorld(ruta[1]);
        Vector2 paso = Vector2.MoveTowards(rb.position, destino, velocidad * Time.fixedDeltaTime);
        rb.MovePosition(paso);
    }
}
