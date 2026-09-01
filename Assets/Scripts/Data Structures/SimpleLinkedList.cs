using System;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Implementacion dinamica del TDA List: cada elemento vive en su propio nodo y
/// apunta al siguiente. Guarda punteros a la cabeza y a la cola.
///
/// Conviene cuando lo que mas se hace es agregar y sacar por los extremos, y se
/// recorre en orden. Sacar el primero no obliga a mover nada: alcanza con
/// correr la cabeza un lugar. En cambio llegar a una posicion del medio cuesta,
/// porque hay que recorrer los nodos uno por uno.
///
///   RemoveFirst  O(1)
///   Add          O(1), por el puntero a la cola
///   this[i]      O(n)
///   Insert       O(n)
///   RemoveAt     O(n)
///   IndexOf      O(n)
/// </summary>
public class SimpleLinkedList<T> : ISimpleList<T>
{
    private class Nodo
    {
        public T Valor;
        public Nodo Siguiente;

        public Nodo(T valor)
        {
            Valor = valor;
            Siguiente = null;
        }
    }

    private Nodo cabeza;
    private Nodo cola;
    private int cantidad;

    public int Count => cantidad;

    public T this[int indice]
    {
        get => NodoEn(indice).Valor;
        set => NodoEn(indice).Valor = value;
    }

    public void Add(T item)
    {
        Nodo nuevo = new Nodo(item);

        if (cabeza == null)
        {
            cabeza = nuevo;
            cola = nuevo;
        }
        else
        {
            // El puntero a la cola evita recorrer la lista entera en cada alta
            cola.Siguiente = nuevo;
            cola = nuevo;
        }

        cantidad++;
    }

    /// <summary>Agrega al principio, sin recorrer nada.</summary>
    public void AddFirst(T item)
    {
        Nodo nuevo = new Nodo(item) { Siguiente = cabeza };
        cabeza = nuevo;

        if (cola == null) cola = nuevo;

        cantidad++;
    }

    public void Insert(int indice, T item)
    {
        if (indice < 0 || indice > cantidad)
            throw new ArgumentOutOfRangeException(nameof(indice));

        if (indice == 0)
        {
            AddFirst(item);
            return;
        }

        if (indice == cantidad)
        {
            Add(item);
            return;
        }

        Nodo anterior = NodoEn(indice - 1);
        Nodo nuevo = new Nodo(item) { Siguiente = anterior.Siguiente };
        anterior.Siguiente = nuevo;
        cantidad++;
    }

    public bool Remove(T item)
    {
        var comparador = EqualityComparer<T>.Default;
        Nodo anterior = null;
        Nodo actual = cabeza;

        while (actual != null)
        {
            if (comparador.Equals(actual.Valor, item))
            {
                Desenlazar(anterior, actual);
                return true;
            }

            anterior = actual;
            actual = actual.Siguiente;
        }

        return false;
    }

    public void RemoveAt(int indice)
    {
        ValidarIndice(indice);

        if (indice == 0)
        {
            Desenlazar(null, cabeza);
            return;
        }

        Nodo anterior = NodoEn(indice - 1);
        Desenlazar(anterior, anterior.Siguiente);
    }

    public T RemoveFirst()
    {
        if (cabeza == null)
            throw new InvalidOperationException("La lista esta vacia.");

        T primero = cabeza.Valor;
        Desenlazar(null, cabeza);
        return primero;
    }

    public int IndexOf(T item)
    {
        var comparador = EqualityComparer<T>.Default;
        Nodo actual = cabeza;
        int i = 0;

        while (actual != null)
        {
            if (comparador.Equals(actual.Valor, item)) return i;
            actual = actual.Siguiente;
            i++;
        }

        return -1;
    }

    public bool Contains(T item) => IndexOf(item) >= 0;

    public void Clear()
    {
        cabeza = null;
        cola = null;
        cantidad = 0;
    }

    /// <summary>Saca un nodo de la cadena y mantiene cabeza, cola y cantidad al dia.</summary>
    private void Desenlazar(Nodo anterior, Nodo aQuitar)
    {
        if (anterior == null)
            cabeza = aQuitar.Siguiente;
        else
            anterior.Siguiente = aQuitar.Siguiente;

        if (aQuitar == cola)
            cola = anterior;

        aQuitar.Siguiente = null;
        cantidad--;
    }

    private Nodo NodoEn(int indice)
    {
        ValidarIndice(indice);

        Nodo actual = cabeza;
        for (int i = 0; i < indice; i++)
            actual = actual.Siguiente;

        return actual;
    }

    private void ValidarIndice(int indice)
    {
        if (indice < 0 || indice >= cantidad)
            throw new ArgumentOutOfRangeException(nameof(indice),
                $"Indice {indice} fuera de rango. La lista tiene {cantidad} elementos.");
    }

    public IEnumerator<T> GetEnumerator()
    {
        Nodo actual = cabeza;

        while (actual != null)
        {
            yield return actual.Valor;
            actual = actual.Siguiente;
        }
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
