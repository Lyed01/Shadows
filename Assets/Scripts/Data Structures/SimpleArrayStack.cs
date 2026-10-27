using System;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Implementacion estatica del TDA Stack: un arreglo que se duplica al llenarse,
/// con la cima en el ultimo lugar ocupado.
///
///   Push   O(1) amortizado
///   Pop    O(1)
///   Peek   O(1)
///
/// Todo se hace por indice, sin correr elementos: la cima siempre es
/// elementos[cantidad - 1]. Empata en costo con la dinamica; cambia la memoria
/// (sin un nodo por elemento, pero con capacidad de sobra).
/// </summary>
public class SimpleArrayStack<T> : ISimpleStack<T>
{
    private const int CapacidadInicial = 4;

    private T[] elementos = new T[CapacidadInicial];
    private int cantidad;

    public int Count => cantidad;

    public bool IsEmpty => cantidad == 0;

    public void Push(T item)
    {
        if (cantidad == elementos.Length)
            Agrandar();

        elementos[cantidad] = item;
        cantidad++;
    }

    public T Pop()
    {
        ValidarNoVacia();

        cantidad--;
        T item = elementos[cantidad];
        elementos[cantidad] = default;

        return item;
    }

    public T Peek()
    {
        ValidarNoVacia();
        return elementos[cantidad - 1];
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
            copia[i] = elementos[cantidad - 1 - i];

        return copia;
    }

    private void ValidarNoVacia()
    {
        if (cantidad == 0)
            throw new InvalidOperationException("La pila esta vacia.");
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
        for (int i = cantidad - 1; i >= 0; i--)
            yield return elementos[i];
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
