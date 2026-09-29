using System;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Implementacion estatica del TDA Priority Queue: guarda pares (item,
/// prioridad) en un arreglo sin ordenar que se agranda al doble cuando se
/// llena, igual que SimpleArrayList.
///
/// Conviene cuando lo que mas se hace es encolar (Enqueue), porque agregar es
/// siempre appendear al final. El costo se paga al sacar el mas prioritario
/// (Dequeue/Peek), que tiene que recorrer todo el arreglo para encontrarlo.
/// Por eso es la eleccion correcta para un proceso donde las altas son mucho
/// mas frecuentes que las bajas.
///
/// El numero de prioridad mas chico es el que sale primero.
///
///   Enqueue       O(1) amortizado
///   Dequeue       O(n)
///   Peek          O(n)
///   Contains      O(n)
/// </summary>
public class SimpleArrayPriorityQueue<T> : ISimplePriorityQueue<T>
{
    private struct Entrada
    {
        public T Item;
        public int Prioridad;

        public Entrada(T item, int prioridad)
        {
            Item = item;
            Prioridad = prioridad;
        }
    }

    private const int CapacidadInicial = 4;

    private Entrada[] elementos;
    private int cantidad;

    public SimpleArrayPriorityQueue()
    {
        elementos = new Entrada[CapacidadInicial];
        cantidad = 0;
    }

    public SimpleArrayPriorityQueue(int capacidad)
    {
        if (capacidad < 1) capacidad = CapacidadInicial;
        elementos = new Entrada[capacidad];
        cantidad = 0;
    }

    public int Count => cantidad;

    public bool IsEmpty => cantidad == 0;

    /// <summary>Cuantos elementos entran sin volver a agrandar el arreglo.</summary>
    public int Capacidad => elementos.Length;

    public void Enqueue(T item, int prioridad)
    {
        AsegurarEspacio(cantidad + 1);
        elementos[cantidad] = new Entrada(item, prioridad);
        cantidad++;
    }

    public T Dequeue()
    {
        int indice = IndiceDelMasPrioritario();
        T item = elementos[indice].Item;

        // El arreglo no esta ordenado, asi que no hace falta correr todo lo
        // que viene despues: alcanza con traer el ultimo a este lugar.
        cantidad--;
        elementos[indice] = elementos[cantidad];
        elementos[cantidad] = default;

        return item;
    }

    public T Peek()
    {
        ValidarNoVacia();
        return elementos[IndiceDelMasPrioritario()].Item;
    }

    public int GetHighestPriority()
    {
        ValidarNoVacia();
        return elementos[IndiceDelMasPrioritario()].Prioridad;
    }

    public bool Contains(T item)
    {
        var comparador = EqualityComparer<T>.Default;

        for (int i = 0; i < cantidad; i++)
            if (comparador.Equals(elementos[i].Item, item))
                return true;

        return false;
    }

    public void Clear()
    {
        for (int i = 0; i < cantidad; i++)
            elementos[i] = default;

        cantidad = 0;
    }

    /// <summary>
    /// Copia los items pendientes. Salen en el orden interno del arreglo, que
    /// no es el de prioridad: el orden lo garantiza Dequeue, no el arreglo.
    /// </summary>
    public T[] ToArray()
    {
        T[] copia = new T[cantidad];

        for (int i = 0; i < cantidad; i++)
            copia[i] = elementos[i].Item;

        return copia;
    }

    private int IndiceDelMasPrioritario()
    {
        ValidarNoVacia();

        // Gana el numero mas chico. El > estricto deja ganar al primero que
        // aparece cuando hay empate, que es el que llego antes.
        int mejor = 0;
        for (int i = 1; i < cantidad; i++)
            if (elementos[i].Prioridad < elementos[mejor].Prioridad)
                mejor = i;

        return mejor;
    }

    private void ValidarNoVacia()
    {
        if (cantidad == 0)
            throw new InvalidOperationException("La cola esta vacia.");
    }

    /// <summary>
    /// Duplica la capacidad cuando hace falta, igual que SimpleArrayList: el
    /// costo de copiar se reparte entre todas las altas que entran en el
    /// espacio nuevo, y eso es lo que deja a Enqueue en O(1) amortizado.
    /// </summary>
    private void AsegurarEspacio(int necesaria)
    {
        if (necesaria <= elementos.Length) return;

        int nueva = elementos.Length * 2;
        while (nueva < necesaria) nueva *= 2;

        Entrada[] copia = new Entrada[nueva];
        for (int i = 0; i < cantidad; i++)
            copia[i] = elementos[i];

        elementos = copia;
    }

    public IEnumerator<T> GetEnumerator()
    {
        for (int i = 0; i < cantidad; i++)
            yield return elementos[i].Item;
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
