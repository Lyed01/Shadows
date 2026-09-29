using System;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Implementacion estatica del TDA List: guarda los elementos en un arreglo
/// contiguo que se agranda al doble cuando se llena.
///
/// Conviene cuando lo que mas se hace es leer por indice, porque llega a
/// cualquier posicion en un solo paso. Las inserciones y borrados en el medio
/// cuestan, porque hay que correr todo lo que viene despues.
///
///   this[i]      O(1)
///   Add          O(1) amortizado
///   Insert       O(n)
///   RemoveAt     O(n)
///   RemoveFirst  O(n)
///   IndexOf      O(n)
/// </summary>
public class SimpleArrayList<T> : ISimpleList<T>
{
    private const int CapacidadInicial = 4;

    private T[] elementos;
    private int cantidad;

    public SimpleArrayList()
    {
        elementos = new T[CapacidadInicial];
        cantidad = 0;
    }

    public SimpleArrayList(int capacidad)
    {
        if (capacidad < 1) capacidad = CapacidadInicial;
        elementos = new T[capacidad];
        cantidad = 0;
    }

    public int Count => cantidad;

    /// <summary>Cuantos elementos entran sin volver a agrandar el arreglo.</summary>
    public int Capacidad => elementos.Length;

    public T this[int indice]
    {
        get
        {
            ValidarIndice(indice);
            return elementos[indice];
        }
        set
        {
            ValidarIndice(indice);
            elementos[indice] = value;
        }
    }

    public void Add(T item)
    {
        AsegurarEspacio(cantidad + 1);
        elementos[cantidad] = item;
        cantidad++;
    }

    public void Insert(int indice, T item)
    {
        // Insertar al final es valido, por eso se admite indice == cantidad
        if (indice < 0 || indice > cantidad)
            throw new ArgumentOutOfRangeException(nameof(indice));

        AsegurarEspacio(cantidad + 1);

        for (int i = cantidad; i > indice; i--)
            elementos[i] = elementos[i - 1];

        elementos[indice] = item;
        cantidad++;
    }

    /// <summary>
    /// Agrega todos los elementos del arreglo al final. Pide el espacio una
    /// sola vez en lugar de dejar que Add lo pida en cada vuelta.
    /// </summary>
    public void AddRange(T[] items)
    {
        if (items == null)
            throw new ArgumentNullException(nameof(items));

        AsegurarEspacio(cantidad + items.Length);

        for (int i = 0; i < items.Length; i++)
        {
            elementos[cantidad] = items[i];
            cantidad++;
        }
    }

    public bool Remove(T item)
    {
        int indice = IndexOf(item);
        if (indice < 0) return false;

        RemoveAt(indice);
        return true;
    }

    public void RemoveAt(int indice)
    {
        ValidarIndice(indice);

        for (int i = indice; i < cantidad - 1; i++)
            elementos[i] = elementos[i + 1];

        cantidad--;
        // Se limpia la ultima celda para no retener una referencia que ya no
        // pertenece a la lista y evitar que el recolector la de por viva.
        elementos[cantidad] = default;
    }

    /// <summary>
    /// Quita un tramo completo. Corre lo que queda una sola vez, en vez de
    /// llamar a RemoveAt en cada posicion y desplazar la cola una vez por
    /// elemento.
    /// </summary>
    public void RemoveRange(int indice, int cantidadAQuitar)
    {
        if (indice < 0 || cantidadAQuitar < 0 || indice + cantidadAQuitar > cantidad)
            throw new ArgumentOutOfRangeException(nameof(indice),
                $"Rango [{indice}, {indice + cantidadAQuitar}) fuera de una lista de {cantidad} elementos.");

        if (cantidadAQuitar == 0) return;

        for (int i = indice; i < cantidad - cantidadAQuitar; i++)
            elementos[i] = elementos[i + cantidadAQuitar];

        for (int i = cantidad - cantidadAQuitar; i < cantidad; i++)
            elementos[i] = default;

        cantidad -= cantidadAQuitar;
    }

    public T RemoveFirst()
    {
        if (cantidad == 0)
            throw new InvalidOperationException("La lista esta vacia.");

        T primero = elementos[0];
        RemoveAt(0);
        return primero;
    }

    public int IndexOf(T item)
    {
        var comparador = EqualityComparer<T>.Default;

        for (int i = 0; i < cantidad; i++)
            if (comparador.Equals(elementos[i], item))
                return i;

        return -1;
    }

    public bool Contains(T item) => IndexOf(item) >= 0;

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

    private void ValidarIndice(int indice)
    {
        if (indice < 0 || indice >= cantidad)
            throw new ArgumentOutOfRangeException(nameof(indice),
                $"Indice {indice} fuera de rango. La lista tiene {cantidad} elementos.");
    }

    /// <summary>
    /// Duplica la capacidad cuando hace falta. Crecer al doble y no de a uno es
    /// lo que hace que Add cueste O(1) en promedio: el costo de copiar se
    /// reparte entre todas las altas que entran en el espacio nuevo.
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
