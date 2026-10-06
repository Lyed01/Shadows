using System;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Implementacion estatica del TDA Dictionary: guarda los pares en un arreglo
/// contiguo que se agranda al doble cuando se llena, igual que SimpleArraySet.
///
/// Como no hay orden que respetar, las altas van siempre al final y las bajas
/// no corren el resto del arreglo: traen el ultimo par al hueco que quedo.
///
///   IndexOf / ContainsKey / TryGetValue / this[]   O(n)   recorre buscando la clave
///   Add / TryAdd                                   O(n)   chequean la clave antes de agregar
///   Remove                                         O(n)   busca y tapa el hueco con el ultimo
///   Count                                          O(1)
///
/// Conviene sobre la dinamica cuando el diccionario es chico y se consulta
/// mucho mas de lo que se modifica: no se paga un nodo (ni un objeto para el
/// recolector) por cada par, y los pares quedan uno al lado del otro en
/// memoria. La complejidad es la misma que en la dinamica.
/// </summary>
public class SimpleArrayDictionary<TKey, TValue> : ISimpleDictionary<TKey, TValue>
{
    private const int CapacidadInicial = 4;

    private KeyValuePair<TKey, TValue>[] pares;
    private int cantidad;

    public SimpleArrayDictionary()
    {
        pares = new KeyValuePair<TKey, TValue>[CapacidadInicial];
        cantidad = 0;
    }

    public SimpleArrayDictionary(int capacidad)
    {
        if (capacidad < 1) capacidad = CapacidadInicial;
        pares = new KeyValuePair<TKey, TValue>[capacidad];
        cantidad = 0;
    }

    public int Count => cantidad;

    public bool IsEmpty => cantidad == 0;

    /// <summary>Cuantos pares entran sin volver a agrandar el arreglo.</summary>
    public int Capacidad => pares.Length;

    public TValue this[TKey key]
    {
        get
        {
            int indice = IndexOf(key);

            if (indice < 0)
                throw new KeyNotFoundException($"La clave '{key}' no esta en el diccionario.");

            return pares[indice].Value;
        }
        set
        {
            int indice = IndexOf(key);

            // KeyValuePair es un struct inmutable: para cambiar el valor se
            // arma un par nuevo en el mismo lugar.
            if (indice >= 0)
                pares[indice] = new KeyValuePair<TKey, TValue>(key, value);
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
        int indice = IndexOf(key);
        if (indice < 0) return false;

        // Sin orden que respetar, alcanza con traer el ultimo a este lugar en
        // vez de correr todo lo que viene despues.
        cantidad--;
        pares[indice] = pares[cantidad];
        pares[cantidad] = default;

        return true;
    }

    public bool ContainsKey(TKey key) => IndexOf(key) >= 0;

    public bool TryGetValue(TKey key, out TValue value)
    {
        int indice = IndexOf(key);

        if (indice < 0)
        {
            value = default;
            return false;
        }

        value = pares[indice].Value;
        return true;
    }

    public void Clear()
    {
        for (int i = 0; i < cantidad; i++)
            pares[i] = default;

        cantidad = 0;
    }

    public TKey[] Keys()
    {
        TKey[] claves = new TKey[cantidad];

        for (int i = 0; i < cantidad; i++)
            claves[i] = pares[i].Key;

        return claves;
    }

    public TValue[] Values()
    {
        TValue[] valores = new TValue[cantidad];

        for (int i = 0; i < cantidad; i++)
            valores[i] = pares[i].Value;

        return valores;
    }

    /// <summary>
    /// Posicion de la clave, o -1 si no esta. Lo reutilizan el indexador,
    /// ContainsKey, TryGetValue y Remove, y es donde se valida la clave nula.
    /// </summary>
    private int IndexOf(TKey key)
    {
        if (key == null)
            throw new ArgumentNullException(nameof(key));

        var comparador = EqualityComparer<TKey>.Default;

        for (int i = 0; i < cantidad; i++)
            if (comparador.Equals(pares[i].Key, key))
                return i;

        return -1;
    }

    /// <summary>
    /// Agrega al final sin chequear la clave. Las tres formas de agregar
    /// (indexador, Add y TryAdd) difieren en como tratan a una clave repetida,
    /// pero una vez que decidieron agregar hacen lo mismo.
    /// </summary>
    private void EjecutarAlta(TKey key, TValue value)
    {
        AsegurarEspacio(cantidad + 1);
        pares[cantidad] = new KeyValuePair<TKey, TValue>(key, value);
        cantidad++;
    }

    /// <summary>
    /// Duplica la capacidad cuando hace falta, igual que SimpleArrayList: el
    /// costo de copiar se reparte entre todas las altas que entran en el
    /// espacio nuevo.
    /// </summary>
    private void AsegurarEspacio(int necesaria)
    {
        if (necesaria <= pares.Length) return;

        int nueva = pares.Length * 2;
        while (nueva < necesaria) nueva *= 2;

        var copia = new KeyValuePair<TKey, TValue>[nueva];
        for (int i = 0; i < cantidad; i++)
            copia[i] = pares[i];

        pares = copia;
    }

    public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
    {
        for (int i = 0; i < cantidad; i++)
            yield return pares[i];
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
