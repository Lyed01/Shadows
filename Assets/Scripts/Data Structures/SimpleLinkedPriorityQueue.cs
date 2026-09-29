using System;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Implementacion dinamica del TDA Priority Queue: cada elemento vive en su
/// propio nodo, y los nodos se mantienen siempre ordenados por prioridad. La
/// cabeza es, por construccion, el mas prioritario.
///
/// El numero de prioridad mas chico es el que sale primero.
///
/// Conviene cuando lo que mas se hace es sacar el mas prioritario
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
        public int Prioridad;
        public Nodo Siguiente;

        public Nodo(T item, int prioridad)
        {
            Item = item;
            Prioridad = prioridad;
            Siguiente = null;
        }
    }

    private Nodo cabeza;
    private int cantidad;

    public int Count => cantidad;

    public bool IsEmpty => cantidad == 0;

    public void Enqueue(T item, int prioridad)
    {
        Nodo nuevo = new Nodo(item, prioridad);

        if (cabeza == null || prioridad < cabeza.Prioridad)
        {
            nuevo.Siguiente = cabeza;
            cabeza = nuevo;
        }
        else
        {
            // Se avanza mientras el siguiente sea igual de prioritario o mas,
            // para insertar despues de todos ellos: asi un empate respeta el
            // orden de llegada.
            Nodo actual = cabeza;
            while (actual.Siguiente != null && actual.Siguiente.Prioridad <= prioridad)
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

    public int GetHighestPriority()
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

    /// <summary>
    /// Copia los items pendientes. Como los nodos se mantienen ordenados, el
    /// arreglo sale en orden de prioridad.
    /// </summary>
    public T[] ToArray()
    {
        T[] copia = new T[cantidad];
        Nodo actual = cabeza;

        for (int i = 0; i < cantidad; i++)
        {
            copia[i] = actual.Item;
            actual = actual.Siguiente;
        }

        return copia;
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
