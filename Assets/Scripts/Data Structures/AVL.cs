using System;

/// <summary>
/// TDA AVL: un BST que se rebalancea solo. Despues de cada alta y cada baja,
/// en cada nodo del camino recorrido, compara la altura de sus dos hijos: si
/// difieren en mas de 1, rota para emparejarlos. Asi la altura queda siempre
/// cerca de log2 n, aunque se inserten los valores ya ordenados (el caso que
/// vuelve al BST una lista).
///
///   Add / Remove / Contains / TryGet    O(log n)   garantizado
///
/// Hereda todo de BST y solo sobrescribe Insertar y Quitar: llama a la version
/// del BST (que hace la insercion o el borrado comun) y despues arregla el
/// nodo con Balancear a la vuelta de la recursion.
///
/// Los cuatro casos de desbalance:
///   izquierda-izquierda    una rotacion a la derecha
///   derecha-derecha        una rotacion a la izquierda
///   izquierda-derecha      rotacion izquierda del hijo y despues derecha
///   derecha-izquierda      rotacion derecha del hijo y despues izquierda
/// </summary>
public class AVL<TKey, T> : BST<TKey, T> where T : IComparable<T>
{
    public AVL(Func<TKey, T, int> compararClave) : base(compararClave) { }

    /// <summary>Altura en O(1): la raiz la guarda.</summary>
    public override int Height => Alt(raiz);

    /// <summary>
    /// Comprueba que en todos los nodos la diferencia de alturas entre hijos
    /// sea de 1 como maximo y que las alturas guardadas sean las reales.
    /// </summary>
    public bool EstaBalanceado() => Verificar(raiz);

    protected override TreeNode<T> Insertar(TreeNode<T> nodo, T value, out bool agregado) =>
        Balancear(base.Insertar(nodo, value, out agregado));

    protected override TreeNode<T> Quitar(TreeNode<T> nodo, T value, out bool quitado) =>
        Balancear(base.Quitar(nodo, value, out quitado));

    private static int Alt(TreeNode<T> nodo) => nodo == null ? 0 : nodo.Height;

    private static void ActualizarAltura(TreeNode<T> nodo) =>
        nodo.Height = 1 + Math.Max(Alt(nodo.Left), Alt(nodo.Right));

    private static int FactorDeBalance(TreeNode<T> nodo) =>
        nodo == null ? 0 : Alt(nodo.Left) - Alt(nodo.Right);

    private static TreeNode<T> Balancear(TreeNode<T> nodo)
    {
        if (nodo == null) return null;

        ActualizarAltura(nodo);
        int factor = FactorDeBalance(nodo);

        // Pesado a la izquierda
        if (factor > 1)
        {
            // Si el hijo pesa a la derecha, primero hay que enderezarlo
            if (FactorDeBalance(nodo.Left) < 0)
                nodo.Left = RotarIzquierda(nodo.Left);

            return RotarDerecha(nodo);
        }

        // Pesado a la derecha
        if (factor < -1)
        {
            if (FactorDeBalance(nodo.Right) > 0)
                nodo.Right = RotarDerecha(nodo.Right);

            return RotarIzquierda(nodo);
        }

        return nodo;
    }

    //      y                x
    //     / \              / \
    //    x   C    -->     A   y
    //   / \                  / \
    //  A   B                B   C
    private static TreeNode<T> RotarDerecha(TreeNode<T> y)
    {
        TreeNode<T> x = y.Left;

        y.Left = x.Right;
        x.Right = y;

        ActualizarAltura(y);
        ActualizarAltura(x);

        return x;
    }

    private static TreeNode<T> RotarIzquierda(TreeNode<T> x)
    {
        TreeNode<T> y = x.Right;

        x.Right = y.Left;
        y.Left = x;

        ActualizarAltura(x);
        ActualizarAltura(y);

        return y;
    }

    private static bool Verificar(TreeNode<T> nodo)
    {
        if (nodo == null) return true;

        int esperada = 1 + Math.Max(Alt(nodo.Left), Alt(nodo.Right));

        return nodo.Height == esperada
            && Math.Abs(FactorDeBalance(nodo)) <= 1
            && Verificar(nodo.Left)
            && Verificar(nodo.Right);
    }
}
