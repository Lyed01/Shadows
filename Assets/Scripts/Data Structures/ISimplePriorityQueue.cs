using System.Collections.Generic;

/// <summary>
/// Contrato del TDA Priority Queue (cola de prioridad). Lo cumplen las dos
/// implementaciones propias del proyecto: SimpleArrayPriorityQueue, sobre un
/// arreglo sin ordenar, y SimpleLinkedPriorityQueue, sobre nodos enlazados que
/// se mantienen ordenados por prioridad.
///
/// A diferencia de una cola comun (FIFO), el elemento que sale primero no es
/// el que entro primero sino el mas prioritario. La prioridad se numera como
/// los puestos de una carrera: el 1 va antes que el 2, asi que el numero mas
/// chico es el que se atiende primero. Entre dos elementos de igual prioridad
/// sale antes el que llego primero.
///
/// Salvo por Enqueue, que recibe la prioridad, y por GetHighestPriority, la
/// interfaz es la misma que la de una Queue.
///
/// Hereda de IEnumerable para poder recorrer los elementos pendientes con
/// foreach (por ejemplo, para depurar), sin sacarlos de la cola ni respetar
/// necesariamente el orden de prioridad en ese recorrido.
/// </summary>
public interface ISimplePriorityQueue<T> : IEnumerable<T>
{
    /// <summary>Cantidad de elementos guardados.</summary>
    int Count { get; }

    /// <summary>Si la cola no tiene ningun elemento.</summary>
    bool IsEmpty { get; }

    /// <summary>
    /// Agrega un elemento con su prioridad. Cuanto mas chico el numero, antes
    /// se atiende.
    /// </summary>
    void Enqueue(T item, int prioridad);

    /// <summary>Quita y devuelve el elemento mas prioritario.</summary>
    T Dequeue();

    /// <summary>Devuelve el elemento mas prioritario sin quitarlo.</summary>
    T Peek();

    /// <summary>Prioridad del primer elemento de la cola, sin quitarlo.</summary>
    int GetHighestPriority();

    /// <summary>Si el elemento esta en la cola.</summary>
    bool Contains(T item);

    /// <summary>Vacia la cola.</summary>
    void Clear();

    /// <summary>Copia los elementos pendientes a un arreglo nuevo.</summary>
    T[] ToArray();
}
