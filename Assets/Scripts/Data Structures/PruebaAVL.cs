using System;
using System.Linq;
using System.Text;
using UnityEngine;

/// <summary>
/// Ejercita el TDA AVL y deja el resultado en la consola (y en UltimaSalida,
/// que lo muestra el panel de la escena): los cuatro casos de rotacion, que el
/// arbol quede balanceado tras altas y bajas, y la comparacion de altura contra
/// el BST con la misma carga ordenada.
/// </summary>
public class PruebaAVL : MonoBehaviour
{
    [Tooltip("Ejecutar apenas arranca la escena.")]
    public bool correrAlIniciar = true;

    private StringBuilder salida;
    /// <summary>Texto de la ultima corrida, para mostrarlo en el panel de la escena.</summary>
    public string UltimaSalida => salida?.ToString();
    private int pruebasCorridas;
    private int pruebasFalladas;

    private void Start()
    {
        if (correrAlIniciar)
            CorrerTodo();
    }

    [ContextMenu("Correr las pruebas")]
    public void CorrerTodo()
    {
        salida = new StringBuilder();
        pruebasCorridas = 0;
        pruebasFalladas = 0;

        Titulo("Las cuatro rotaciones (el arbol resultante siempre es 20 con hijos 10 y 30)");
        ProbarRotaciones();

        Titulo("Contrato heredado del BST");
        ProbarContrato();

        Titulo("Balance tras muchas altas y bajas");
        ProbarBalance();

        Titulo("AVL contra BST con la misma carga");
        CompararConBST();

        salida.AppendLine();
        salida.AppendLine($"{pruebasCorridas - pruebasFalladas} de {pruebasCorridas} comprobaciones pasaron.");

        if (pruebasFalladas > 0)
            Debug.LogError(salida.ToString());
        else
            Debug.Log(salida.ToString());
    }

    private static AVL<int, int> Nuevo() => new AVL<int, int>((clave, valor) => clave.CompareTo(valor));

    private static AVL<int, int> Con(params int[] valores)
    {
        var arbol = Nuevo();
        foreach (int v in valores) arbol.Add(v);
        return arbol;
    }

    private void ProbarRotaciones()
    {
        var esperado = new[] { 20, 10, 30 };

        Verificar("Izquierda-izquierda (30, 20, 10): rotacion simple a la derecha",
            Con(30, 20, 10).PreOrder().SequenceEqual(esperado));
        Verificar("Derecha-derecha (10, 20, 30): rotacion simple a la izquierda",
            Con(10, 20, 30).PreOrder().SequenceEqual(esperado));
        Verificar("Izquierda-derecha (30, 10, 20): rotacion doble",
            Con(30, 10, 20).PreOrder().SequenceEqual(esperado));
        Verificar("Derecha-izquierda (10, 30, 20): rotacion doble",
            Con(10, 30, 20).PreOrder().SequenceEqual(esperado));

        var cuatro = Con(10, 20, 30, 40, 50, 25);
        Verificar("Una carga mas larga queda balanceada", cuatro.EstaBalanceado() && cuatro.Count == 6);
        Verificar("Y con la raiz esperada", cuatro.PreOrder().First() == 30);
    }

    private void ProbarContrato()
    {
        var arbol = Nuevo();
        Verificar("Arranca vacio", arbol.IsEmpty && arbol.Height == 0 && arbol.EstaBalanceado());

        arbol = Con(50, 30, 70, 20, 40, 60, 80);
        Verificar("Add, Count y altura de un arbol parejo", arbol.Count == 7 && arbol.Height == 3);
        Verificar("Add de un repetido devuelve false", !arbol.Add(40) && arbol.Count == 7);
        Verificar("Contains y TryGet por clave", arbol.Contains(60) && arbol.TryGet(80, out int v) && v == 80);
        Verificar("TryGet devuelve false si no esta", !arbol.TryGet(65, out _));
        Verificar("Min, Max e InOrder", arbol.Min() == 20 && arbol.Max() == 80
            && arbol.InOrder().SequenceEqual(new[] { 20, 30, 40, 50, 60, 70, 80 }));

        Verificar("Remove de una hoja", arbol.Remove(20) && !arbol.Contains(20) && arbol.EstaBalanceado());
        Verificar("Remove de un nodo con dos hijos", arbol.Remove(70) && arbol.EstaBalanceado()
            && arbol.InOrder().SequenceEqual(new[] { 30, 40, 50, 60, 80 }));
        Verificar("Remove de un ausente devuelve false", !arbol.Remove(999));

        arbol.Clear();
        Verificar("Clear vacia", arbol.IsEmpty && arbol.Height == 0);
    }

    private void ProbarBalance()
    {
        const int n = 1000;

        var ordenado = Nuevo();
        for (int i = 0; i < n; i++) ordenado.Add(i);
        Verificar($"{n} valores ordenados: queda balanceado", ordenado.EstaBalanceado());
        Verificar($"{n} valores ordenados: altura <= 1,44 * log2(n + 2)",
            ordenado.Height <= 1.44 * Math.Log(n + 2, 2));

        var azar = new System.Random(7);
        var arbol = Nuevo();
        var presentes = new System.Collections.Generic.HashSet<int>();
        bool siempreBalanceado = true;

        for (int i = 0; i < 2000; i++)
        {
            int valor = azar.Next(0, 300);

            if (azar.Next(0, 2) == 0) { if (arbol.Add(valor)) presentes.Add(valor); }
            else { if (arbol.Remove(valor)) presentes.Remove(valor); }

            if (!arbol.EstaBalanceado()) siempreBalanceado = false;
        }

        Verificar("2000 altas y bajas al azar: balanceado despues de cada operacion", siempreBalanceado);
        Verificar("Count coincide con lo que quedo", arbol.Count == presentes.Count);
        Verificar("InOrder sigue ordenado y completo",
            arbol.InOrder().SequenceEqual(presentes.OrderBy(x => x)));
    }

    private void CompararConBST()
    {
        const int n = 200;
        var avl = Nuevo();
        var bst = new BST<int, int>((clave, valor) => clave.CompareTo(valor));

        for (int i = 0; i < n; i++) { avl.Add(i); bst.Add(i); }

        salida.AppendLine($"  Con {n} valores insertados en orden:");
        salida.AppendLine($"    BST   altura {bst.Height,4}   (una lista: O(n))");
        salida.AppendLine($"    AVL   altura {avl.Height,4}   (log2 n = {Mathf.CeilToInt(Mathf.Log(n, 2))})");

        Verificar("El AVL es mucho mas bajo que el BST con carga ordenada", avl.Height < bst.Height / 10);
    }

    private void Verificar(string descripcion, bool condicion)
    {
        pruebasCorridas++;
        if (!condicion) pruebasFalladas++;

        salida.AppendLine($"  {(condicion ? "OK  " : "FALLA")} {descripcion}");
    }

    private void Titulo(string texto)
    {
        salida.AppendLine();
        salida.AppendLine($"--- {texto} ---");
    }
}
