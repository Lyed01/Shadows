using System.Collections.Generic;

/// <summary>
/// Contrato del TDA Set (conjunto): una coleccion de elementos sin repetidos y
/// sin orden garantizado. Lo cumplen las dos implementaciones propias del
/// proyecto: SimpleArraySet, sobre un arreglo, y SimpleLinkedSet, sobre nodos
/// enlazados.
///
/// Lo que distingue al Set de una lista son dos cosas: no admite duplicados, y
/// responde preguntas de pertenencia y de comparacion entre conjuntos.
///
/// Nota de complejidad: a diferencia del TDA List, aca las dos
/// implementaciones cuestan lo mismo. Todas las operaciones basicas son O(n),
/// porque para saber si un elemento ya esta hay que recorrer. Elegir entre una
/// y otra no se decide por tiempo sino por memoria y por como se usa.
/// </summary>
public interface ISimpleSet<T> : IEnumerable<T>
{
    /// <summary>Cantidad de elementos guardados.</summary>
    int Count { get; }

    /// <summary>Si el conjunto no tiene ningun elemento.</summary>
    bool IsEmpty { get; }

    /// <summary>
    /// Agrega el elemento. Devuelve false si ya estaba, porque un conjunto no
    /// admite repetidos.
    /// </summary>
    bool Add(T item);

    /// <summary>Quita el elemento. Devuelve si lo encontro.</summary>
    bool Remove(T item);

    /// <summary>Si el elemento pertenece al conjunto.</summary>
    bool Contains(T item);

    /// <summary>Vacia el conjunto.</summary>
    void Clear();

    /// <summary>Copia los elementos a un arreglo nuevo, sin orden garantizado.</summary>
    T[] ToArray();

    /// <summary>Union: los elementos de este conjunto y los del otro.</summary>
    ISimpleSet<T> UnionWith(ISimpleSet<T> other);

    /// <summary>Interseccion: los elementos que estan en los dos.</summary>
    ISimpleSet<T> IntersectWith(ISimpleSet<T> other);

    /// <summary>Diferencia: los de este conjunto que no estan en el otro.</summary>
    ISimpleSet<T> DifferenceWith(ISimpleSet<T> other);
}
