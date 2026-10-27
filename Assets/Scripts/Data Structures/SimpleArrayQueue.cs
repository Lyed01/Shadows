using System;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Implementacion estatica del TDA Queue: un arreglo que se duplica al llenarse,
/// con el frente en el indice 0.
///
///   Enqueue   O(1) amortizado
///   Dequeue   O(n)   hay que correr todos un lugar para tapar el hueco
///   Peek      O(1)
///
/// Sacar del frente es el punto debil del arreglo: la clase menciona el arreglo
/// circular como salida, que no se ve en la materia. Si lo que mas se hace es
/// desencolar, conviene la dinamica.
/// </summary>
public class SimpleArrayQueue<T> : ISimpleQueue<T>
{
    private const int CapacidadInicial = 4;

    private T[] elementos = new T[CapacidadInicial];
    private int cantidad;

    public int Count => cantidad;

    public bool IsEmpty => cantidad == 0;

    public void Enqueue(T item)
    {
        if (cantidad == elementos.Length)
            Agrandar();

        elementos[cantidad] = item;
        cantidad++;
    }

    public T Dequeue()
    {
        ValidarNoVacia();

        T item = elementos[0];

        for (int i = 1; i < cantidad; i++)
            elementos[i - 1] = elementos[i];

        cantidad--;
        elementos[cantidad] = default;

        return item;
    }

    public T Peek()
    {
        ValidarNoVacia();
        return elementos[0];
    }

    public void Clear()
    {
        Array.Clear(elementos, 0, cantidad);
        cantidad = 0;
    }

    public T[] ToArray()
    {
        T[] copia = new T[cantidad];

        for (int i = 0; i < cantidad; i++)
            copia[i] = elementos[i];

        return copia;
    }

    private void ValidarNoVacia()
    {
        if (cantidad == 0)
            throw new InvalidOperationException("La cola esta vacia.");
    }

    private void Agrandar()
    {
        T[] copia = new T[elementos.Length * 2];

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
