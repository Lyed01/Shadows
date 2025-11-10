using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.Events;
using Unity.Cinemachine;

public enum ResultadoColocacion
{
    Exito,
    NoExisteCelda,
    CeldaBloqueada,
    CeldaOcupada,
    FueraDeRango,
}

public class GridManager : MonoBehaviour
{
    [System.Serializable]
    public class CellData
    {
        public bool unlocked = false;
        public bool occupied = false;
        public Vector3Int cellPos;
    }

    [Header("Referencias")]
    public Tilemap sueloTilemap;
    public Tile tileDesbloqueado;
    public Tile tileSeleccion;
    public GameObject prefabBloque;
    public GameObject prefabBloqueReflectante;

    [Header("Spawn del Jugador")]
    public Transform spawnTransform;          // Solo define la posición inicial (GameManager instancia al jugador)
    public HUDHabilidad hudHabilidad;         // Referencia opcional para las habilidades
    public GameObject panelHUDHabilidad;

    [Header("Eventos")]
    public UnityEvent onPlayerDeath;          // 🔹 visible en el inspector

    // === Datos internos ===
    private Dictionary<Vector3Int, CellData> celdas = new();
    private List<GameObject> bloquesInstanciados = new();
    private List<Vector3Int> celdasMostradas = new();

    void Start()
    {
        InicializarCeldas();
    }

    // === INICIALIZACIÓN ===
    void InicializarCeldas()
    {
        if (sueloTilemap == null)
        {
            Debug.LogError("❌ GridManager: falta asignar el Tilemap del suelo.");
            return;
        }

        BoundsInt bounds = sueloTilemap.cellBounds;
        celdas.Clear();

        for (int x = bounds.xMin; x < bounds.xMax; x++)
        {
            for (int y = bounds.yMin; y < bounds.yMax; y++)
            {
                Vector3Int pos = new(x, y, 0);
                if (sueloTilemap.HasTile(pos))
                    celdas[pos] = new CellData { cellPos = pos };
            }
        }

        Debug.Log($"🧩 GridManager inicializó {celdas.Count} celdas activas.");
    }

    // === BLOQUES Y CELDAS ===
    public ResultadoColocacion IntentarColocarBloque(Vector3 worldPos, bool reflectante, Vector3 jugadorPos, float rango)
    {
        Vector3Int cellPos = sueloTilemap.WorldToCell(worldPos);

        // Si la celda no existe o no tiene tile de suelo, buscar una cercana hacia abajo
        if (!celdas.ContainsKey(cellPos) || !sueloTilemap.HasTile(cellPos))
        {
            Vector3Int abajo = new Vector3Int(cellPos.x, cellPos.y - 1, cellPos.z);
            if (celdas.ContainsKey(abajo) && sueloTilemap.HasTile(abajo))
                cellPos = abajo;
            else
                return ResultadoColocacion.NoExisteCelda;
        }

        CellData data = celdas[cellPos];
        float distancia = Vector3.Distance(sueloTilemap.GetCellCenterWorld(cellPos), jugadorPos);

        if (!data.unlocked)
            return ResultadoColocacion.CeldaBloqueada;
        if (data.occupied)
            return ResultadoColocacion.CeldaOcupada;
        if (distancia > rango)
            return ResultadoColocacion.FueraDeRango;

        // ✅ Colocación exitosa
        Vector3 spawnPos = sueloTilemap.GetCellCenterWorld(cellPos);
        GameObject prefab = reflectante ? prefabBloqueReflectante : prefabBloque;
        GameObject bloque = Instantiate(prefab, spawnPos, Quaternion.identity);

        ShadowBlock sb = bloque.GetComponent<ShadowBlock>();
        if (sb != null)
        {
            sb.cellPos = cellPos;
            sb.gridManager = this;
            sb.hudHabilidad = hudHabilidad;
        }

        data.occupied = true;
        celdas[cellPos] = data;
        bloquesInstanciados.Add(bloque);

        return ResultadoColocacion.Exito;
    }

    public void EliminarShadowBlocks()
    {
        foreach (GameObject b in bloquesInstanciados)
            if (b != null) Destroy(b);

        bloquesInstanciados.Clear();
    }

    public void ResetearCeldas()
    {
        foreach (var kvp in celdas)
        {
            kvp.Value.occupied = false;
            if (kvp.Value.unlocked)
                sueloTilemap.SetTile(kvp.Key, tileDesbloqueado);
        }
    }

    public void LiberarCelda(Vector3Int cellPos)
    {
        if (!celdas.ContainsKey(cellPos)) return;
        celdas[cellPos].occupied = false;
        sueloTilemap.SetTile(cellPos, tileDesbloqueado);
    }

    public bool EsCeldaColocable(Vector3Int cellPos, Vector3 jugadorPos, float rango)
    {
        if (!celdas.ContainsKey(cellPos)) return false;
        var data = celdas[cellPos];
        float dist = Vector3.Distance(sueloTilemap.GetCellCenterWorld(cellPos), jugadorPos);
        return data.unlocked && !data.occupied && dist <= rango;
    }

    // === CORRUPCIÓN ===
    public void CorromperCeldas(Vector3 worldPos)
    {
        if (sueloTilemap == null) return;

        // 🔹 Ajuste para compensar el pivot del jugador (mitad de una celda hacia abajo)
        Vector3 ajuste = new Vector3(0f, -sueloTilemap.cellSize.y * 0.5f, 0f);
        Vector3Int centerCell = sueloTilemap.WorldToCell(worldPos + ajuste);

        Vector3Int spawnCell = sueloTilemap.WorldToCell(spawnTransform.position);
        if (centerCell == spawnCell) return;

        Vector3Int[] offsets = new Vector3Int[]
        {
        Vector3Int.zero,
        new(0, 1, 0),
        new(0, -1, 0),
        new(-1, 0, 0),
        new(1, 0, 0)
        };

        foreach (Vector3Int offset in offsets)
        {
            Vector3Int cellPos = centerCell + offset;
            if (!celdas.ContainsKey(cellPos)) continue;

            CellData data = celdas[cellPos];
            data.unlocked = true;
            celdas[cellPos] = data;

            if (tileDesbloqueado != null)
                sueloTilemap.SetTile(cellPos, tileDesbloqueado);
        }
    }

    public void CorromperCeldaUnica(Vector3Int cellPos)
    {
        if (!celdas.ContainsKey(cellPos)) return;

        CellData data = celdas[cellPos];
        data.unlocked = true;
        celdas[cellPos] = data;

        if (tileDesbloqueado != null)
            sueloTilemap.SetTile(cellPos, tileDesbloqueado);
    }

    // === CELDAS VISIBLES PARA EL JUGADOR ===
    public void MostrarCeldasDisponibles(Vector3 worldPos, float rango)
    {
        OcultarCeldasDisponibles();
        Vector3Int centerCell = sueloTilemap.WorldToCell(worldPos);

        foreach (var kvp in celdas)
        {
            float distancia = Vector3Int.Distance(centerCell, kvp.Key);
            if (kvp.Value.unlocked && !kvp.Value.occupied && distancia <= rango)
            {
                sueloTilemap.SetTile(kvp.Key, tileSeleccion);
                celdasMostradas.Add(kvp.Key);
            }
        }
    }

    public void OcultarCeldasDisponibles()
    {
        foreach (var cellPos in celdasMostradas)
        {
            if (celdas.ContainsKey(cellPos) && celdas[cellPos].unlocked)
                sueloTilemap.SetTile(cellPos, tileDesbloqueado);
        }
        celdasMostradas.Clear();
    }
    public void NotificarMuerteJugador()
    {
        Debug.Log("🕯️ Muerte del jugador detectada (GridManager)");
        onPlayerDeath?.Invoke();
    }



}

