using System;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Implementacion dinamica del TDA Dictionary: cada par vive en su propio nodo
/// y apunta al siguiente. Se guarda la cabeza y la cola, asi que dar de alta al
/// final no obliga a recorrer.
///
///   BuscarNodo / ContainsKey / TryGetValue / this[]   O(n)   recorre buscando la clave
///   Add / TryAdd                                      O(n)   chequean la clave antes de agregar
///   Remove                                            O(n)   recorre buscando el nodo
///   Count                                             O(1)
///
/// Los costos son los mismos que en la estatica, porque el que manda es el
/// recorrido. La diferencia esta en la memoria: no reserva capacidad de mas ni
/// copia el arreglo al crecer, pero paga un nodo (y un puntero) por par.
/// Conviene cuando el diccionario crece mucho o es de tamano imprevisible.
/// </summary>
public class SimpleLinkedDictionary<TKey, TValue> : ISimpleDictionary<TKey, TValue>
{
    /// <summary>
    /// Nodo propio con la clave y el valor sueltos, en vez de un KeyValuePair:
    /// asi se escribe nodo.Valor y no nodo.Par.Value, y el valor se puede
    /// cambiar sin reemplazar el nodo.
    /// </summary>
    private class DictionaryNode
    {
        public TKey Clave;
        public TValue Valor;
        public DictionaryNode Siguiente;

        public DictionaryNode(TKey clave, TValue valor)
        {
            Clave = clave;
            Valor = valor;
            Siguiente = null;
        }
    }

    private DictionaryNode primero;
    private DictionaryNode ultimo;
    private int cantidad;

    public int Count => cantidad;

    public bool IsEmpty => cantidad == 0;

    public TValue this[TKey key]
    {
        get
        {
            DictionaryNode nodo = BuscarNodo(key);

            if (nodo == null)
                throw new KeyNotFoundException($"La clave '{key}' no esta en el diccionario.");

            return nodo.Valor;
        }
        set
        {
            DictionaryNode nodo = BuscarNodo(key);

            if (nodo != null)
                nodo.Valor = value;
            else
                EjecutarAlta(key, value);
        }
    }

    public void Add(TKey key, TValue value)
    {
        if (ContainsKey(key))
            throw new ArgumentException($"La clave '{key}' ya esta en el diccionario.", nameof(key));

        EjecutarAlta(key, value);
    }

    public bool TryAdd(TKey key, TValue value)
    {
        if (ContainsKey(key)) return false;

        EjecutarAlta(key, value);
        return true;
    }

    public bool Remove(TKey key)
    {
        if (key == null)
            throw new ArgumentNullException(nameof(key));

        if (primero == null) return false;

        var comparador = EqualityComparer<TKey>.Default;

        // Caso especial: el que se va es el primero, que no tiene anterior.
        if (comparador.Equals(primero.Clave, key))
        {
            DictionaryNode viejo = primero;
            primero = primero.Siguiente;

            if (primero == null)
                ultimo = null;

            viejo.Siguiente = null;
            cantidad--;

            return true;
        }

        // La lista es simplemente enlazada: no hay forma de volver al anterior
        // desde el nodo encontrado. Por eso se mira al siguiente de actual en
        // vez de a actual, y asi todavia se lo tiene a mano para reconectar.
        DictionaryNode actual = primero;

        while (actual.Siguiente != null)
        {
            if (comparador.Equals(actual.Siguiente.Clave, key))
            {
                DictionaryNode viejo = actual.Siguiente;
                actual.Siguiente = viejo.Siguiente;

                if (viejo == ultimo)
                    ultimo = actual;

                viejo.Siguiente = null;
                cantidad--;

                return true;
            }

            actual = actual.Siguiente;
        }

        return false;
    }

    public bool ContainsKey(TKey key) => BuscarNodo(key) != null;

    public bool TryGetValue(TKey key, out TValue value)
    {
        DictionaryNode nodo = BuscarNodo(key);

        if (nodo == null)
        {
            value = default;
            return false;
        }

        value = nodo.Valor;
        return true;
    }

    public void Clear()
    {
        primero = null;
        ultimo = null;
        cantidad = 0;
    }

    public TKey[] Keys()
    {
        TKey[] claves = new TKey[cantidad];
        DictionaryNode actual = primero;

        for (int i = 0; i < cantidad; i++)
        {
            claves[i] = actual.Clave;
            actual = actual.Siguiente;
        }

        return claves;
    }

    public TValue[] Values()
    {
        TValue[] valores = new TValue[cantidad];
        DictionaryNode actual = primero;

        for (int i = 0; i < cantidad; i++)
        {
            valores[i] = actual.Valor;
            actual = actual.Siguiente;
        }

        return valores;
    }

    /// <summary>
    /// Nodo de la clave, o null si no esta. Lo reutilizan el indexador,
    /// ContainsKey y TryGetValue, y es donde se valida la clave nula.
    /// </summary>
    private DictionaryNode BuscarNodo(TKey key)
    {
        if (key == null)
            throw new ArgumentNullException(nameof(key));

        var comparador = EqualityComparer<TKey>.Default;
        DictionaryNode actual = primero;

        while (actual != null)
        {
            if (comparador.Equals(actual.Clave, key)) return actual;
            actual = actual.Siguiente;
        }

        return null;
    }

    /// <summary>
    /// Enlaza un nodo nuevo al final sin chequear la clave: si esta vacio pasa
    /// a ser el primero, y si no, el siguiente del ultimo. En ambos casos queda
    /// como ultimo.
    /// </summary>
    private void EjecutarAlta(TKey key, TValue value)
    {
        DictionaryNode nuevo = new DictionaryNode(key, value);

        if (primero == null)
            primero = nuevo;
        else
            ultimo.Siguiente = nuevo;

        ultimo = nuevo;
        cantidad++;
    }

    public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
    {
        DictionaryNode actual = primero;

        while (actual != null)
        {
            yield return new KeyValuePair<TKey, TValue>(actual.Clave, actual.Valor);
            actual = actual.Siguiente;
        }
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
