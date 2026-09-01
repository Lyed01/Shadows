using System.Collections.Generic;

/// <summary>
/// Contrato del TDA List. Lo cumplen las dos implementaciones propias del
/// proyecto: SimpleArrayList, sobre un arreglo, y SimpleLinkedList, sobre nodos
/// enlazados.
///
/// Hereda de IEnumerable para que el codigo del juego pueda recorrer una lista
/// con foreach, que es como ya lo hace en todos lados.
/// </summary>
public interface ISimpleList<T> : IEnumerable<T>
{
    /// <summary>Cantidad de elementos guardados.</summary>
    int Count { get; }

    /// <summary>Lectura y escritura por posicion.</summary>
    T this[int indice] { get; set; }

    /// <summary>Agrega al final.</summary>
    void Add(T item);

    /// <summary>Inserta en una posicion, corriendo el resto hacia atras.</summary>
    void Insert(int indice, T item);

    /// <summary>Quita la primera aparicion del elemento. Devuelve si lo encontro.</summary>
    bool Remove(T item);

    /// <summary>Quita el elemento de una posicion.</summary>
    void RemoveAt(int indice);

    /// <summary>Quita el primer elemento y lo devuelve.</summary>
    T RemoveFirst();

    /// <summary>Posicion de la primera aparicion, o -1 si no esta.</summary>
    int IndexOf(T item);

    /// <summary>Si el elemento esta en la lista.</summary>
    bool Contains(T item);

    /// <summary>Vacia la lista.</summary>
    void Clear();
}
