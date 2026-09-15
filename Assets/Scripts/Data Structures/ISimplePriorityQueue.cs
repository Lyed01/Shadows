using System.Collections.Generic;

/// <summary>
/// Contrato del TDA Priority Queue (cola de prioridad). Lo cumplen las dos
/// implementaciones propias del proyecto: SimpleArrayPriorityQueue, sobre un
/// arreglo sin ordenar, y SimpleLinkedPriorityQueue, sobre nodos enlazados que
/// se mantienen ordenados por prioridad.
///
/// A diferencia de una cola comun (FIFO), el elemento que sale primero no es
/// el que entro primero sino el de mayor prioridad. Un numero de prioridad
/// mas alto significa que se atiende antes. Ante un empate no se garantiza
/// el orden de llegada.
///
/// Hereda de IEnumerable para poder recorrer los elementos pendientes con
/// foreach (por ejemplo, para depurar), sin sacarlos de la cola ni respetar
/// necesariamente el orden de prioridad en ese recorrido.
/// </summary>
public interface ISimplePriorityQueue<T> : IEnumerable<T>
{
    /// <summary>Cantidad de elementos guardados.</summary>
    int Count { get; }

    /// <summary>Agrega un elemento con su prioridad. Mayor numero, mayor prioridad.</summary>
    void Enqueue(T item, float prioridad);

    /// <summary>Quita y devuelve el elemento de mayor prioridad.</summary>
    T Dequeue();

    /// <summary>Devuelve el elemento de mayor prioridad sin quitarlo.</summary>
    T Peek();

    /// <summary>Prioridad del elemento que esta a la cabeza, sin quitarlo.</summary>
    float PeekPriority();

    /// <summary>Si el elemento esta en la cola.</summary>
    bool Contains(T item);

    /// <summary>Vacia la cola.</summary>
    void Clear();
}
