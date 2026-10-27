using System.Collections.Generic;

/// <summary>
/// Contrato del TDA Queue (cola): FIFO, el primero que entra es el primero que
/// sale. Lo cumplen SimpleArrayQueue (arreglo) y SimpleLinkedQueue (nodos).
///
/// Leer de una cola vacia (Dequeue o Peek) tira InvalidOperationException.
/// Hereda de IEnumerable para recorrerla con foreach del frente al final, sin
/// sacar nada.
/// </summary>
public interface ISimpleQueue<T> : IEnumerable<T>
{
    int Count { get; }

    bool IsEmpty { get; }

    /// <summary>Encola un elemento: queda al final.</summary>
    void Enqueue(T item);

    /// <summary>Quita y devuelve el elemento del frente.</summary>
    T Dequeue();

    /// <summary>Devuelve el elemento del frente sin quitarlo.</summary>
    T Peek();

    void Clear();

    /// <summary>Copia los elementos a un arreglo nuevo, del frente al final.</summary>
    T[] ToArray();
}
