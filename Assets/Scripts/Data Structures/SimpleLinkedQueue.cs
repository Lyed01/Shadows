using System;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Implementacion dinamica del TDA Queue: nodos enlazados con referencia al
/// primero y al ultimo, asi que ambos extremos se tocan sin recorrer.
///
///   Enqueue   O(1)   engancha detras del ultimo
///   Dequeue   O(1)   suelta el primero
///   Peek      O(1)
///
/// Es la eleccion cuando se desencola seguido (por ejemplo la cola de pasos de
/// un NPC): evita correr todo el arreglo en cada baja.
/// </summary>
public class SimpleLinkedQueue<T> : ISimpleQueue<T>
{
    private class QueueNode
    {
        public T Valor;
        public QueueNode Siguiente;

        public QueueNode(T valor)
        {
            Valor = valor;
        }
    }

    private QueueNode primero;
    private QueueNode ultimo;
    private int cantidad;

    public int Count => cantidad;

    public bool IsEmpty => cantidad == 0;

    public void Enqueue(T item)
    {
        QueueNode nuevo = new QueueNode(item);

        if (ultimo == null)
            primero = nuevo;
        else
            ultimo.Siguiente = nuevo;

        ultimo = nuevo;
        cantidad++;
    }

    public T Dequeue()
    {
        ValidarNoVacia();

        QueueNode viejo = primero;
        primero = viejo.Siguiente;

        if (primero == null)
            ultimo = null;

        viejo.Siguiente = null;
        cantidad--;

        return viejo.Valor;
    }

    public T Peek()
    {
        ValidarNoVacia();
        return primero.Valor;
    }

    public void Clear()
    {
        primero = null;
        ultimo = null;
        cantidad = 0;
    }

    public T[] ToArray()
    {
        T[] copia = new T[cantidad];
        QueueNode actual = primero;

        for (int i = 0; i < cantidad; i++)
        {
            copia[i] = actual.Valor;
            actual = actual.Siguiente;
        }

        return copia;
    }

    private void ValidarNoVacia()
    {
        if (cantidad == 0)
            throw new InvalidOperationException("La cola esta vacia.");
    }

    public IEnumerator<T> GetEnumerator()
    {
        QueueNode actual = primero;

        while (actual != null)
        {
            yield return actual.Valor;
            actual = actual.Siguiente;
        }
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
