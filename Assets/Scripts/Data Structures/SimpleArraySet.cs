using System;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Implementacion estatica del TDA Set: guarda los elementos en un arreglo
/// contiguo que se agranda al doble cuando se llena, igual que
/// SimpleArrayList.
///
/// Como el conjunto no garantiza orden, dos cosas se simplifican: las altas
/// van siempre al final, y las bajas no corren el resto del arreglo, sino que
/// traen el ultimo elemento al hueco que quedo.
///
///   Contains  O(n)   hay que recorrer para saber si ya esta
///   Add       O(n)   llama a Contains antes de agregar
///   Remove    O(n)   busca y tapa el hueco con el ultimo
///   Count     O(1)
///
/// Conviene sobre la dinamica cuando el conjunto es chico y se pregunta mucho
/// por pertenencia: los elementos estan uno al lado del otro en memoria, asi
/// que el recorrido es mas rapido en la practica aunque la complejidad sea la
/// misma, y no se paga un nodo por elemento.
/// </summary>
public class SimpleArraySet<T> : ISimpleSet<T>
{
    private const int CapacidadInicial = 4;

    private T[] elementos;
    private int cantidad;

    public SimpleArraySet()
    {
        elementos = new T[CapacidadInicial];
        cantidad = 0;
    }

    public SimpleArraySet(int capacidad)
    {
        if (capacidad < 1) capacidad = CapacidadInicial;
        elementos = new T[capacidad];
        cantidad = 0;
    }

    public int Count => cantidad;

    public bool IsEmpty => cantidad == 0;

    /// <summary>Cuantos elementos entran sin volver a agrandar el arreglo.</summary>
    public int Capacidad => elementos.Length;

    public bool Add(T item)
    {
        if (item == null)
            throw new ArgumentNullException(nameof(item));

        if (Contains(item)) return false;

        AsegurarEspacio(cantidad + 1);
        elementos[cantidad] = item;
        cantidad++;

        return true;
    }

    public bool Remove(T item)
    {
        if (item == null)
            throw new ArgumentNullException(nameof(item));

        var comparador = EqualityComparer<T>.Default;

        for (int i = 0; i < cantidad; i++)
        {
            if (!comparador.Equals(elementos[i], item)) continue;

            // Sin orden que respetar, alcanza con traer el ultimo a este lugar
            // en vez de correr todo lo que viene despues.
            cantidad--;
            elementos[i] = elementos[cantidad];
            elementos[cantidad] = default;

            return true;
        }

        return false;
    }

    public bool Contains(T item)
    {
        if (item == null)
            throw new ArgumentNullException(nameof(item));

        var comparador = EqualityComparer<T>.Default;

        for (int i = 0; i < cantidad; i++)
            if (comparador.Equals(elementos[i], item))
                return true;

        return false;
    }

    public void Clear()
    {
        for (int i = 0; i < cantidad; i++)
            elementos[i] = default;

        cantidad = 0;
    }

    public T[] ToArray()
    {
        T[] copia = new T[cantidad];

        for (int i = 0; i < cantidad; i++)
            copia[i] = elementos[i];

        return copia;
    }

    public ISimpleSet<T> UnionWith(ISimpleSet<T> other)
    {
        ValidarOtro(other);

        var resultado = new SimpleArraySet<T>(cantidad + other.Count);

        for (int i = 0; i < cantidad; i++)
            resultado.Add(elementos[i]);

        foreach (T item in other)
            resultado.Add(item);

        return resultado;
    }

    public ISimpleSet<T> IntersectWith(ISimpleSet<T> other)
    {
        ValidarOtro(other);

        var resultado = new SimpleArraySet<T>();

        for (int i = 0; i < cantidad; i++)
            if (other.Contains(elementos[i]))
                resultado.Add(elementos[i]);

        return resultado;
    }

    public ISimpleSet<T> DifferenceWith(ISimpleSet<T> other)
    {
        ValidarOtro(other);

        var resultado = new SimpleArraySet<T>();

        for (int i = 0; i < cantidad; i++)
            if (!other.Contains(elementos[i]))
                resultado.Add(elementos[i]);

        return resultado;
    }

    private static void ValidarOtro(ISimpleSet<T> other)
    {
        if (other == null)
            throw new ArgumentNullException(nameof(other));
    }

    /// <summary>
    /// Duplica la capacidad cuando hace falta, igual que SimpleArrayList: el
    /// costo de copiar se reparte entre todas las altas que entran en el
    /// espacio nuevo.
    /// </summary>
    private void AsegurarEspacio(int necesaria)
    {
        if (necesaria <= elementos.Length) return;

        int nueva = elementos.Length * 2;
        while (nueva < necesaria) nueva *= 2;

        T[] copia = new T[nueva];
        for (int i = 0; i < cantidad; i++)
            copia[i] = elementos[i];

        elementos = copia;
    }

    public IEnumerator<T> GetEnumerator()
    {
        for (int i = 0; i < cantidad; i++)
            yield return elementos[i];
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
