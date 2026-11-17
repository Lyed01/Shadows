using System;

/// <summary>
/// TDA Graph con lista de adyacencia generica: un diccionario propio que asocia
/// cada nodo con la lista propia de sus aristas salientes. Los costos son
/// enteros y no negativos (Dijkstra no admite otros). Un arreglo-diccionario
/// alcanza: buscar un nodo es O(n), pero los grafos del juego son de cientos de
/// nodos y recorrer arreglos contiguos es lo mas barato en memoria.
/// </summary>
public class Graph<T>
{
    private readonly ISimpleDictionary<T, ISimpleList<Edge<T>>> adyacencia =
        new SimpleArrayDictionary<T, ISimpleList<Edge<T>>>();

    /// <summary>Cantidad de nodos.</summary>
    public int Count => adyacencia.Count;

    /// <summary>Agrega el nodo. Devuelve false si ya estaba.</summary>
    public bool AddNode(T node) => adyacencia.TryAdd(node, new SimpleLinkedList<Edge<T>>());

    public bool ContainsNode(T node) => adyacencia.ContainsKey(node);

    /// <summary>
    /// Une dos nodos que ya existen. Por defecto va en los dos sentidos; con
    /// bidireccional en false solo de from a to. Si la arista ya existe le
    /// cambia el costo.
    /// </summary>
    public void AddEdge(T from, T to, int cost, bool bidireccional = true)
    {
        if (cost < 0) throw new ArgumentOutOfRangeException(nameof(cost), "El costo no puede ser negativo.");
        if (!adyacencia.ContainsKey(from) || !adyacencia.ContainsKey(to))
            throw new ArgumentException("Los dos nodos tienen que existir antes de unirlos.");

        PonerArista(from, to, cost);
        if (bidireccional) PonerArista(to, from, cost);
    }

    /// <summary>Quita la arista de from a to (y la inversa si es bidireccional). Devuelve si existia.</summary>
    public bool RemoveEdge(T from, T to, bool bidireccional = true)
    {
        bool quito = QuitarArista(from, to);
        if (bidireccional) quito |= QuitarArista(to, from);
        return quito;
    }

    /// <summary>Quita el nodo y toda arista que llegue a el. Devuelve si existia.</summary>
    public bool RemoveNode(T node)
    {
        if (!adyacencia.Remove(node)) return false;
        foreach (var par in adyacencia) QuitarArista(par.Key, node);
        return true;
    }

    public bool HasEdge(T from, T to)
    {
        return adyacencia.TryGetValue(from, out var aristas) && BuscarArista(aristas, to) >= 0;
    }

    /// <summary>Aristas salientes del nodo. Tira KeyNotFoundException si el nodo no existe.</summary>
    public ISimpleList<Edge<T>> GetEdges(T node) => adyacencia[node];

    /// <summary>Copia los nodos a un arreglo nuevo.</summary>
    public T[] Nodes() => adyacencia.Keys();

    public void Clear() => adyacencia.Clear();

    private void PonerArista(T from, T to, int cost)
    {
        var aristas = adyacencia[from];
        int i = BuscarArista(aristas, to);
        if (i >= 0) aristas[i] = new Edge<T>(to, cost);
        else aristas.Add(new Edge<T>(to, cost));
    }

    private bool QuitarArista(T from, T to)
    {
        if (!adyacencia.TryGetValue(from, out var aristas)) return false;
        int i = BuscarArista(aristas, to);
        if (i < 0) return false;
        aristas.RemoveAt(i);
        return true;
    }

    private static int BuscarArista(ISimpleList<Edge<T>> aristas, T to)
    {
        var comparador = System.Collections.Generic.EqualityComparer<T>.Default;
        int i = 0;
        foreach (var arista in aristas)
        {
            if (comparador.Equals(arista.To, to)) return i;
            i++;
        }
        return -1;
    }
}
