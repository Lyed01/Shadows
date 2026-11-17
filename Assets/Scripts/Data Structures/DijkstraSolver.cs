/// <summary>
/// Dijkstra con la cola de prioridad propia: se expande siempre el nodo pendiente
/// de menor distancia acumulada. Usa la version enlazada de la cola porque
/// Dequeue es O(1) (la mas usada) y el costo de ordenar se paga al encolar.
/// Un nodo puede encolarse varias veces con distancias distintas; al salir, el
/// que ya fue visitado se descarta.
/// </summary>
public class DijkstraSolver<T> : IPathfindingSolver<T>
{
    public ISimpleList<T> FindPath(Graph<T> graph, T start, T goal)
    {
        if (!graph.ContainsNode(start) || !graph.ContainsNode(goal)) return null;

        var distancia = new SimpleArrayDictionary<T, int>();
        var anterior = new SimpleArrayDictionary<T, T>();
        var visitados = new SimpleArraySet<T>();
        var pendientes = new SimpleLinkedPriorityQueue<T>();

        distancia[start] = 0;
        pendientes.Enqueue(start, 0);

        while (!pendientes.IsEmpty)
        {
            T actual = pendientes.Dequeue();
            if (!visitados.Add(actual)) continue;
            if (System.Collections.Generic.EqualityComparer<T>.Default.Equals(actual, goal)) break;

            int dActual = distancia[actual];
            foreach (var arista in graph.GetEdges(actual))
            {
                if (visitados.Contains(arista.To)) continue;

                int nueva = dActual + arista.Cost;
                if (distancia.TryGetValue(arista.To, out int previa) && previa <= nueva) continue;

                distancia[arista.To] = nueva;
                anterior[arista.To] = actual;
                pendientes.Enqueue(arista.To, nueva);
            }
        }

        if (!visitados.Contains(goal)) return null;

        var camino = new SimpleLinkedList<T>();
        T paso = goal;
        camino.Insert(0, paso);
        while (anterior.TryGetValue(paso, out T previo))
        {
            paso = previo;
            camino.Insert(0, paso);
        }
        return camino;
    }
}
