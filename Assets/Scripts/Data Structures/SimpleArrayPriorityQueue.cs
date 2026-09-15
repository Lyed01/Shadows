using System;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Implementacion estatica del TDA Priority Queue: guarda pares (item,
/// prioridad) en un arreglo sin ordenar que se agranda al doble cuando se
/// llena, igual que SimpleArrayList.
///
/// Conviene cuando lo que mas se hace es encolar (Enqueue), porque agregar es
/// siempre appendear al final. El costo se paga al sacar el de mayor
/// prioridad (Dequeue/Peek), que tiene que recorrer todo el arreglo para
/// encontrarlo. Por eso es la eleccion correcta para un proceso donde las
/// altas son mucho mas frecuentes que las bajas.
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
        public float Prioridad;

        public Entrada(T item, float prioridad)
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

    /// <summary>Cuantos elementos entran sin volver a agrandar el arreglo.</summary>
    public int Capacidad => elementos.Length;

    public void Enqueue(T item, float prioridad)
    {
        AsegurarEspacio(cantidad + 1);
        elementos[cantidad] = new Entrada(item, prioridad);
        cantidad++;
    }

    public T Dequeue()
    {
        int indice = IndiceDeMayorPrioridad();
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
        return elementos[IndiceDeMayorPrioridad()].Item;
    }

    public float PeekPriority()
    {
        ValidarNoVacia();
        return elementos[IndiceDeMayorPrioridad()].Prioridad;
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

    private int IndiceDeMayorPrioridad()
    {
        ValidarNoVacia();

        int mejor = 0;
        for (int i = 1; i < cantidad; i++)
            if (elementos[i].Prioridad > elementos[mejor].Prioridad)
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
