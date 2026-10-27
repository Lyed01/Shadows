using System.Collections.Generic;

/// <summary>
/// Contrato del TDA Stack (pila): LIFO, el ultimo que entra es el primero que
/// sale. Lo cumplen SimpleArrayStack (arreglo) y SimpleLinkedStack (nodos).
///
/// Leer de una pila vacia (Pop o Peek) tira InvalidOperationException.
/// Hereda de IEnumerable para recorrerla con foreach de la cima hacia el fondo,
/// sin sacar nada.
/// </summary>
public interface ISimpleStack<T> : IEnumerable<T>
{
    int Count { get; }

    bool IsEmpty { get; }

    /// <summary>Apila un elemento: queda en la cima.</summary>
    void Push(T item);

    /// <summary>Quita y devuelve el elemento de la cima.</summary>
    T Pop();

    /// <summary>Devuelve el elemento de la cima sin quitarlo.</summary>
    T Peek();

    void Clear();

    /// <summary>Copia los elementos a un arreglo nuevo, de la cima al fondo.</summary>
    T[] ToArray();
}
