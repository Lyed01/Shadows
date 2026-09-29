using System;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Implementacion dinamica del TDA Set: cada elemento vive en su propio nodo y
/// apunta al siguiente.
///
/// Como el conjunto no garantiza orden, las altas van al frente, que es donde
/// no hay que recorrer nada, y las bajas desenlazan el nodo encontrado.
///
///   Contains  O(n)   hay que recorrer para saber si ya esta
///   Add       O(n)   llama a Contains antes de agregar
///   Remove    O(n)   recorre buscando el nodo
///   Count     O(1)
///
/// Los costos son los mismos que en la estatica, porque el que manda es el
/// recorrido. La diferencia esta en la memoria: no reserva capacidad de mas ni
/// copia el arreglo al crecer, pero paga un nodo (y un puntero) por elemento.
/// Conviene cuando el conjunto crece mucho o es de tamano imprevisible.
/// </summary>
public class SimpleLinkedSet<T> : ISimpleSet<T>
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
    private int cantidad;

    public int Count => cantidad;

    public bool IsEmpty => cantidad == 0;

    public bool Add(T item)
    {
        if (item == null)
            throw new ArgumentNullException(nameof(item));

        if (Contains(item)) return false;

        // Sin orden que respetar, la cabeza es el lugar mas barato para entrar.
        Nodo nuevo = new Nodo(item) { Siguiente = cabeza };
        cabeza = nuevo;
        cantidad++;

        return true;
    }

    public bool Remove(T item)
    {
        if (item == null)
            throw new ArgumentNullException(nameof(item));

        var comparador = EqualityComparer<T>.Default;
        Nodo anterior = null;
        Nodo actual = cabeza;

        while (actual != null)
        {
            if (comparador.Equals(actual.Valor, item))
            {
                if (anterior == null)
                    cabeza = actual.Siguiente;
                else
                    anterior.Siguiente = actual.Siguiente;

                actual.Siguiente = null;
                cantidad--;

                return true;
            }

            anterior = actual;
            actual = actual.Siguiente;
        }

        return false;
    }

    public bool Contains(T item)
    {
        if (item == null)
            throw new ArgumentNullException(nameof(item));

        var comparador = EqualityComparer<T>.Default;
        Nodo actual = cabeza;

        while (actual != null)
        {
            if (comparador.Equals(actual.Valor, item)) return true;
            actual = actual.Siguiente;
        }

        return false;
    }

    public void Clear()
    {
        cabeza = null;
        cantidad = 0;
    }

    public T[] ToArray()
    {
        T[] copia = new T[cantidad];
        Nodo actual = cabeza;

        for (int i = 0; i < cantidad; i++)
        {
            copia[i] = actual.Valor;
            actual = actual.Siguiente;
        }

        return copia;
    }

    public ISimpleSet<T> UnionWith(ISimpleSet<T> other)
    {
        ValidarOtro(other);

        var resultado = new SimpleLinkedSet<T>();

        foreach (T item in this)
            resultado.Add(item);

        foreach (T item in other)
            resultado.Add(item);

        return resultado;
    }

    public ISimpleSet<T> IntersectWith(ISimpleSet<T> other)
    {
        ValidarOtro(other);

        var resultado = new SimpleLinkedSet<T>();

        foreach (T item in this)
            if (other.Contains(item))
                resultado.Add(item);

        return resultado;
    }

    public ISimpleSet<T> DifferenceWith(ISimpleSet<T> other)
    {
        ValidarOtro(other);

        var resultado = new SimpleLinkedSet<T>();

        foreach (T item in this)
            if (!other.Contains(item))
                resultado.Add(item);

        return resultado;
    }

    private static void ValidarOtro(ISimpleSet<T> other)
    {
        if (other == null)
            throw new ArgumentNullException(nameof(other));
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
