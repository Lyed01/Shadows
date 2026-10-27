using System;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Implementacion dinamica del TDA Stack: cada elemento vive en un nodo que
/// apunta al que quedo debajo. Solo se guarda la cima, asi que apilar y
/// desapilar es mover un puntero (la optimizacion de enlazar unicamente hacia
/// abajo que sugiere la clase).
///
///   Push   O(1)
///   Pop    O(1)
///   Peek   O(1)
///
/// No reserva capacidad de mas ni copia nada al crecer, pero paga un nodo por
/// elemento.
/// </summary>
public class SimpleLinkedStack<T> : ISimpleStack<T>
{
    private class StackNode
    {
        public T Valor;
        public StackNode Debajo;

        public StackNode(T valor, StackNode debajo)
        {
            Valor = valor;
            Debajo = debajo;
        }
    }

    private StackNode cima;
    private int cantidad;

    public int Count => cantidad;

    public bool IsEmpty => cantidad == 0;

    public void Push(T item)
    {
        cima = new StackNode(item, cima);
        cantidad++;
    }

    public T Pop()
    {
        ValidarNoVacia();

        StackNode viejo = cima;
        cima = viejo.Debajo;
        viejo.Debajo = null;
        cantidad--;

        return viejo.Valor;
    }

    public T Peek()
    {
        ValidarNoVacia();
        return cima.Valor;
    }

    public void Clear()
    {
        cima = null;
        cantidad = 0;
    }

    public T[] ToArray()
    {
        T[] copia = new T[cantidad];
        StackNode actual = cima;

        for (int i = 0; i < cantidad; i++)
        {
            copia[i] = actual.Valor;
            actual = actual.Debajo;
        }

        return copia;
    }

    private void ValidarNoVacia()
    {
        if (cantidad == 0)
            throw new InvalidOperationException("La pila esta vacia.");
    }

    public IEnumerator<T> GetEnumerator()
    {
        StackNode actual = cima;

        while (actual != null)
        {
            yield return actual.Valor;
            actual = actual.Debajo;
        }
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
