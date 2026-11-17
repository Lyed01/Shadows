/// <summary>
/// Contrato de un algoritmo que busca un camino entre dos nodos de un Graph.
/// Separarlo del grafo permite cambiar de algoritmo (Dijkstra, A*) sin tocar a
/// quien lo usa.
/// </summary>
public interface IPathfindingSolver<T>
{
    /// <summary>
    /// Camino de menor costo de start a goal, ambos incluidos y en orden, o null
    /// si no hay camino o alguno de los dos no esta en el grafo. Si son el mismo
    /// nodo devuelve un camino de un solo elemento.
    /// </summary>
    ISimpleList<T> FindPath(Graph<T> graph, T start, T goal);
}
