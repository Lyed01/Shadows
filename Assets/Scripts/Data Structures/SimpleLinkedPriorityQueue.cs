using System;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Implementacion dinamica del TDA Priority Queue: cada elemento vive en su
/// propio nodo, y los nodos se mantienen siempre ordenados de mayor a menor
/// prioridad. La cabeza es, por construccion, el de mayor prioridad.
///
/// Conviene cuando lo que mas se hace es sacar el de mayor prioridad
/// (Dequeue/Peek), porque ya esta ubicado en la cabeza y no hay nada que
/// buscar. El costo se paga al encolar (Enqueue), que tiene que recorrer la
/// cadena para encontrar el lugar donde insertar y mantener el orden.
///
///   Enqueue       O(n)
///   Dequeue       O(1)
///   Peek          O(1)
///   Contains      O(n)
/// </summary>
public class SimpleLinkedPriorityQueue<T> : ISimplePriorityQueue<T>
{
    private class Nodo
    {
        public T Item;
        public float Prioridad;
        public Nodo Siguiente;

        public Nodo(T item, float prioridad)
        {
            Item = item;
            Prioridad = prioridad;
            Siguiente = null;
        }
    }

    private Nodo cabeza;
    private int cantidad;

    public int Count => cantidad;

    public void Enqueue(T item, float prioridad)
    {
        Nodo nuevo = new Nodo(item, prioridad);

        if (cabeza == null || prioridad > cabeza.Prioridad)
        {
            nuevo.Siguiente = cabeza;
            cabeza = nuevo;
        }
        else
        {
            // Se busca el ultimo nodo cuya prioridad todavia es mayor o igual
            // a la nueva, para insertar justo despues y no romper el orden.
            Nodo actual = cabeza;
            while (actual.Siguiente != null && actual.Siguiente.Prioridad >= prioridad)
                actual = actual.Siguiente;

            nuevo.Siguiente = actual.Siguiente;
            actual.Siguiente = nuevo;
        }

        cantidad++;
    }

    public T Dequeue()
    {
        ValidarNoVacia();

        T item = cabeza.Item;
        cabeza = cabeza.Siguiente;
        cantidad--;

        return item;
    }

    public T Peek()
    {
        ValidarNoVacia();
        return cabeza.Item;
    }

    public float PeekPriority()
    {
        ValidarNoVacia();
        return cabeza.Prioridad;
    }

    public bool Contains(T item)
    {
        var comparador = EqualityComparer<T>.Default;
        Nodo actual = cabeza;

        while (actual != null)
        {
            if (comparador.Equals(actual.Item, item)) return true;
            actual = actual.Siguiente;
        }

        return false;
    }

    public void Clear()
    {
        cabeza = null;
        cantidad = 0;
    }

    private void ValidarNoVacia()
    {
        if (cabeza == null)
            throw new InvalidOperationException("La cola esta vacia.");
    }

    public IEnumerator<T> GetEnumerator()
    {
        Nodo actual = cabeza;

        while (actual != null)
        {
            yield return actual.Item;
            actual = actual.Siguiente;
        }
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
