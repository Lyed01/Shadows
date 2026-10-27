using System;
using System.Collections.Generic;

/// <summary>
/// TDA Binary Search Tree. Cada nodo guarda un valor; todo lo que cae a su
/// izquierda es menor y todo lo que cae a su derecha es mayor. Por eso buscar,
/// agregar y sacar descienden una sola rama en vez de recorrer todo.
///
///   Add / Contains / TryGet / Remove    O(h)    h = altura del arbol
///   Min / Max                           O(h)
///   InOrder / PreOrder / PostOrder      O(n)
///   Count                               O(1)
///
/// h vale log n si el arbol queda parejo, pero n si se insertan los valores ya
/// ordenados (el arbol degenera en una lista). Ese es el problema que resuelve
/// el AVL, que hereda de esta clase y rebalancea despues de cada alta y baja.
///
/// Dos criterios de comparacion, los dos vistos en clase:
///  - Entre valores (para ubicar uno nuevo): IComparable de T.
///  - Contra una clave (para buscar): el Func que se pasa al constructor,
///    asi se puede guardar un Item y encontrarlo por su nombre. El criterio de
///    busqueda queda fijo por arbol, en vez de depender de quien llama.
///
/// Los valores no se repiten: dos valores que se comparan iguales son el mismo.
/// </summary>
public class BST<TKey, T> where T : IComparable<T>
{
    protected TreeNode<T> raiz;
    private readonly Func<TKey, T, int> compararClave;

    /// <param name="compararClave">
    /// Compara la clave buscada contra un valor guardado: negativo si la clave
    /// va antes, 0 si es la del valor, positivo si va despues.
    /// </param>
    public BST(Func<TKey, T, int> compararClave)
    {
        this.compararClave = compararClave ?? throw new ArgumentNullException(nameof(compararClave));
    }

    public int Count { get; private set; }

    public bool IsEmpty => Count == 0;

    /// <summary>Raiz, solo para leer (por ejemplo para dibujar el arbol).</summary>
    public TreeNode<T> Raiz => raiz;

    /// <summary>Cantidad de niveles. Un arbol vacio tiene altura 0.</summary>
    public virtual int Height => AlturaDe(raiz);

    /// <summary>Agrega el valor. Devuelve false, sin tocar nada, si ya estaba.</summary>
    public bool Add(T value)
    {
        if (value == null)
            throw new ArgumentNullException(nameof(value));

        raiz = Insertar(raiz, value, out bool agregado);

        if (agregado) Count++;
        return agregado;
    }

    /// <summary>Saca el valor. Devuelve false si no estaba.</summary>
    public bool Remove(T value)
    {
        if (value == null)
            throw new ArgumentNullException(nameof(value));

        raiz = Quitar(raiz, value, out bool quitado);

        if (quitado) Count--;
        return quitado;
    }

    public bool Contains(T value)
    {
        if (value == null)
            throw new ArgumentNullException(nameof(value));

        TreeNode<T> actual = raiz;

        while (actual != null)
        {
            int cmp = value.CompareTo(actual.Value);
            if (cmp == 0) return true;
            actual = cmp < 0 ? actual.Left : actual.Right;
        }

        return false;
    }

    /// <summary>
    /// Busca por clave. Devuelve true y el valor si lo encontro, o false y el
    /// default si no.
    /// </summary>
    public bool TryGet(TKey key, out T value)
    {
        if (key == null)
            throw new ArgumentNullException(nameof(key));

        TreeNode<T> actual = raiz;

        while (actual != null)
        {
            int cmp = compararClave(key, actual.Value);

            if (cmp == 0)
            {
                value = actual.Value;
                return true;
            }

            actual = cmp < 0 ? actual.Left : actual.Right;
        }

        value = default;
        return false;
    }

    public bool ContainsKey(TKey key) => TryGet(key, out _);

    public void Clear()
    {
        raiz = null;
        Count = 0;
    }

    /// <summary>El menor valor: el hijo izquierdo de mas abajo.</summary>
    public T Min()
    {
        if (raiz == null)
            throw new InvalidOperationException("El arbol esta vacio.");

        return MenorDe(raiz).Value;
    }

    /// <summary>El mayor valor: el hijo derecho de mas abajo.</summary>
    public T Max()
    {
        if (raiz == null)
            throw new InvalidOperationException("El arbol esta vacio.");

        TreeNode<T> actual = raiz;
        while (actual.Right != null) actual = actual.Right;

        return actual.Value;
    }

    /// <summary>Izquierda, nodo, derecha: devuelve los valores de menor a mayor.</summary>
    public IEnumerable<T> InOrder() => InOrder(raiz);

    /// <summary>Nodo, izquierda, derecha: el padre siempre antes que sus hijos.</summary>
    public IEnumerable<T> PreOrder() => PreOrder(raiz);

    /// <summary>Izquierda, derecha, nodo: los hijos siempre antes que el padre.</summary>
    public IEnumerable<T> PostOrder() => PostOrder(raiz);

    // Se separan Insertar y Quitar (y se hacen virtual) para que el AVL los
    // extienda: cada uno devuelve la raiz del subarbol ya arreglado, y el AVL
    // aprovecha ese retorno para rotar al volver de la recursion.

    protected virtual TreeNode<T> Insertar(TreeNode<T> nodo, T value, out bool agregado)
    {
        if (nodo == null)
        {
            agregado = true;
            return new TreeNode<T>(value);
        }

        int cmp = value.CompareTo(nodo.Value);

        if (cmp < 0)
            nodo.Left = Insertar(nodo.Left, value, out agregado);
        else if (cmp > 0)
            nodo.Right = Insertar(nodo.Right, value, out agregado);
        else
            agregado = false;

        return nodo;
    }

    protected virtual TreeNode<T> Quitar(TreeNode<T> nodo, T value, out bool quitado)
    {
        if (nodo == null)
        {
            quitado = false;
            return null;
        }

        int cmp = value.CompareTo(nodo.Value);

        if (cmp < 0)
        {
            nodo.Left = Quitar(nodo.Left, value, out quitado);
            return nodo;
        }

        if (cmp > 0)
        {
            nodo.Right = Quitar(nodo.Right, value, out quitado);
            return nodo;
        }

        quitado = true;

        // Sin hijo, o con uno solo: el hijo (o null) ocupa su lugar.
        if (nodo.Left == null) return nodo.Right;
        if (nodo.Right == null) return nodo.Left;

        // Con dos hijos: lo reemplaza su sucesor, el menor de la derecha, que
        // es el valor que sigue en orden y por eso mantiene valido al arbol.
        nodo.Value = MenorDe(nodo.Right).Value;
        nodo.Right = Quitar(nodo.Right, nodo.Value, out _);

        return nodo;
    }

    protected static int AlturaDe(TreeNode<T> nodo)
    {
        if (nodo == null) return 0;

        return 1 + Math.Max(AlturaDe(nodo.Left), AlturaDe(nodo.Right));
    }

    private static TreeNode<T> MenorDe(TreeNode<T> nodo)
    {
        while (nodo.Left != null) nodo = nodo.Left;
        return nodo;
    }

    private static IEnumerable<T> InOrder(TreeNode<T> nodo)
    {
        if (nodo == null) yield break;

        foreach (T v in InOrder(nodo.Left)) yield return v;
        yield return nodo.Value;
        foreach (T v in InOrder(nodo.Right)) yield return v;
    }

    private static IEnumerable<T> PreOrder(TreeNode<T> nodo)
    {
        if (nodo == null) yield break;

        yield return nodo.Value;
        foreach (T v in PreOrder(nodo.Left)) yield return v;
        foreach (T v in PreOrder(nodo.Right)) yield return v;
    }

    private static IEnumerable<T> PostOrder(TreeNode<T> nodo)
    {
        if (nodo == null) yield break;

        foreach (T v in PostOrder(nodo.Left)) yield return v;
        foreach (T v in PostOrder(nodo.Right)) yield return v;
        yield return nodo.Value;
    }
}
