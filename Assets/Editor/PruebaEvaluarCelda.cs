using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Comprueba GridManager.EvaluarCelda sobre una grilla armada en memoria. Es un menu
/// y no un test de NUnit por lo mismo que PruebaRecorteFranjas.
/// </summary>
public static class PruebaEvaluarCelda
{
    [MenuItem("Shadows/Pruebas/Evaluar celda")]
    public static void Ejecutar()
    {
        var raiz = new GameObject("PruebaEvaluarCelda", typeof(Grid));
        var tile = ScriptableObject.CreateInstance<Tile>();
        try
        {
            var mapa = new GameObject("Suelo", typeof(Tilemap)).GetComponent<Tilemap>();
            mapa.transform.SetParent(raiz.transform);

            // Una fila de suelo de x = 0 a x = 5. La celda 6 queda sin tile.
            for (int x = 0; x <= 5; x++)
                mapa.SetTile(new Vector3Int(x, 0, 0), tile);

            var grid = raiz.AddComponent<GridManager>();
            grid.sueloTilemap = mapa;
            typeof(GridManager).GetMethod("InicializarCeldas", BindingFlags.NonPublic | BindingFlags.Instance)
                .Invoke(grid, null);

            for (int x = 0; x <= 4; x++)
                grid.CorromperCeldaUnica(new Vector3Int(x, 0, 0));

            // El jugador parado en la celda 0, corrido hacia el borde: el rango
            // se cuenta desde su celda, no desde sus pies.
            Vector3 jugador = new Vector3(0.9f, 0.5f, 0f);
            const float rango = 3f;

            int fallos = 0;
            fallos += Caso(grid, "Sin tile", new Vector3Int(6, 0, 0), jugador, rango, ResultadoColocacion.NoExisteCelda);
            fallos += Caso(grid, "Sin corromper", new Vector3Int(5, 0, 0), jugador, rango, ResultadoColocacion.CeldaBloqueada);
            fallos += Caso(grid, "Dentro del rango", new Vector3Int(2, 0, 0), jugador, rango, ResultadoColocacion.Exito);
            fallos += Caso(grid, "Justo en el borde del rango", new Vector3Int(3, 0, 0), jugador, rango, ResultadoColocacion.Exito);
            fallos += Caso(grid, "Fuera del rango", new Vector3Int(4, 0, 0), jugador, rango, ResultadoColocacion.FueraDeRango);

            if (fallos == 0)
                Debug.Log("Evaluar celda: todas las comprobaciones en verde");
            else
                Debug.LogError($"Evaluar celda: {fallos} comprobaciones fallaron");
        }
        finally
        {
            Object.DestroyImmediate(raiz);
            Object.DestroyImmediate(tile);
        }
    }

    private static int Caso(GridManager grid, string nombre, Vector3Int celda, Vector3 jugador, float rango,
        ResultadoColocacion esperado)
    {
        ResultadoColocacion obtenido = grid.EvaluarCelda(celda, jugador, rango);
        if (obtenido == esperado) return 0;

        Debug.LogError($"{nombre}: esperaba {esperado} y dio {obtenido}");
        return 1;
    }
}
