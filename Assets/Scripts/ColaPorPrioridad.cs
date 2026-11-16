using System.Collections.Generic;

/// <summary>
/// Cola donde sale primero el numero de prioridad mas chico. A igual
/// prioridad sale el que entro antes. Las colas del juego tienen pocos
/// elementos, asi que insertar ordenado en una lista alcanza.
/// </summary>
public class ColaPorPrioridad<T>
{
    private readonly List<(int prioridad, T item)> elementos = new();

    public int Count => elementos.Count;

    public void Enqueue(T item, int prioridad)
    {
        int i = elementos.Count;
        while (i > 0 && elementos[i - 1].prioridad > prioridad) i--;
        elementos.Insert(i, (prioridad, item));
    }

    public T Dequeue()
    {
        T item = elementos[0].item;
        elementos.RemoveAt(0);
        return item;
    }

    public bool Contains(T item) => elementos.Exists(e => EqualityComparer<T>.Default.Equals(e.item, item));

    public void Clear() => elementos.Clear();
}
