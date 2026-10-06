using System.Collections.Generic;

/// <summary>
/// Contrato del TDA Dictionary: una coleccion de pares clave-valor donde la
/// clave identifica al valor. Las claves no se repiten; los valores si. No se
/// garantiza ningun orden. Lo cumplen las dos implementaciones propias del
/// proyecto: SimpleArrayDictionary, sobre un arreglo, y SimpleLinkedDictionary,
/// sobre nodos enlazados.
///
/// Es un Set donde cada elemento trae un valor asociado, asi que la complejidad
/// es la misma: todas las operaciones con clave son O(n), porque para saber si
/// la clave existe hay que recorrer. Elegir entre una y otra no se decide por
/// tiempo sino por memoria y por como se usa.
///
/// Cualquier funcion que reciba una clave tira ArgumentNullException si es null.
/// </summary>
public interface ISimpleDictionary<TKey, TValue> : IEnumerable<KeyValuePair<TKey, TValue>>
{
    /// <summary>
    /// Lee o escribe por clave. Leer una clave que no esta tira
    /// KeyNotFoundException. Escribir agrega el par si la clave no existia y
    /// pisa el valor si existia.
    /// </summary>
    TValue this[TKey key] { get; set; }

    /// <summary>Cantidad de pares guardados.</summary>
    int Count { get; }

    /// <summary>Si el diccionario no tiene ningun par.</summary>
    bool IsEmpty { get; }

    /// <summary>
    /// Agrega el par. Si la clave ya existe tira ArgumentException: se usa
    /// cuando repetirla es un error.
    /// </summary>
    void Add(TKey key, TValue value);

    /// <summary>
    /// Agrega el par si la clave no existia. Devuelve false, sin tocar nada, si
    /// ya estaba: se usa cuando ambos casos son esperables.
    /// </summary>
    bool TryAdd(TKey key, TValue value);

    /// <summary>Quita la clave con su valor. Devuelve si la encontro.</summary>
    bool Remove(TKey key);

    /// <summary>Si la clave existe.</summary>
    bool ContainsKey(TKey key);

    /// <summary>
    /// Busca el valor de la clave sin tirar excepcion: devuelve si existia y,
    /// en ese caso, deja el valor en value.
    /// </summary>
    bool TryGetValue(TKey key, out TValue value);

    /// <summary>Vacia el diccionario.</summary>
    void Clear();

    /// <summary>Copia las claves a un arreglo nuevo, sin orden garantizado.</summary>
    TKey[] Keys();

    /// <summary>Copia los valores a un arreglo nuevo, en el mismo orden que Keys().</summary>
    TValue[] Values();
}
