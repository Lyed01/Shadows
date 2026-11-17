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
    [Tooltip("Si al alcanzar al jugador lo devuelve a donde empezo (sin matarlo) y vuelve a su lugar de origen.")]
    public bool devolverAlInicio = true;
    public float distanciaParaAlcanzar = 0.7f;

    private readonly IPathfindingSolver<Vector3Int> solver = new DijkstraSolver<Vector3Int>();
    private Rigidbody2D rb;
    private GridManager grid;
    private Jugador jugador;
    private ISimpleList<Vector3Int> ruta;
    private float proximoCalculo;
    private Vector3 origen;
    private Vector3? inicioDelJugador;
    private int largoAnterior = -2;
    private float proximoMensaje;

    /// <summary>Celdas de la ruta actual hasta el jugador, o -1 si no hay camino. Para el panel de depuracion.</summary>
    public int LargoDeRuta => ruta == null ? -1 : ruta.Count;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        origen = transform.position;
    }

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
            var grafo = grid.ConstruirGrafo();
            ruta = solver.FindPath(grafo, inicio, meta);
            // Mientras te persigue el largo cambia en cada recalculo: se cuenta cada
            // 3 s, o enseguida si aparece o se pierde el camino.
            bool cambioDeEstado = (LargoDeRuta < 0) != (largoAnterior < 0);
            if (LargoDeRuta != largoAnterior && (cambioDeEstado || Time.time >= proximoMensaje))
            {
                largoAnterior = LargoDeRuta;
                proximoMensaje = Time.time + 3f;
                Narrador.Decir(10, LargoDeRuta < 0
                    ? $"Dijkstra: no hay camino hasta vos en el grafo de {grafo.Count} celdas libres, el perseguidor se queda quieto."
                    : $"Dijkstra: ruta de {LargoDeRuta} celdas hasta vos sobre un grafo de {grafo.Count} celdas libres.");
            }
        }

        // El primer lugar donde se lo ve es el de partida
        if (inicioDelJugador == null) inicioDelJugador = jugador.transform.position;

        float distancia = Vector2.Distance(transform.position, jugador.transform.position);
        if (devolverAlInicio && distancia <= distanciaParaAlcanzar)
        {
            Narrador.Decir(10, "El perseguidor te alcanzo: volves al inicio y el vuelve a su lugar.");
            jugador.GetComponent<Rigidbody2D>().position = inicioDelJugador.Value;
            jugador.transform.position = inicioDelJugador.Value;
            rb.position = origen;
            ruta = null;
            return;
        }

        if (ruta == null || ruta.Count < 2) return;
        if (distancia <= distanciaParaDetenerse) return;

        // ruta[0] es la celda donde esta; la siguiente es el proximo paso.
        Vector2 destino = grid.sueloTilemap.GetCellCenterWorld(ruta[1]);
        Vector2 paso = Vector2.MoveTowards(rb.position, destino, velocidad * Time.fixedDeltaTime);
        rb.MovePosition(paso);
    }
}
