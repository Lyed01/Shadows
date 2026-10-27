using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

/// <summary>
/// Ejercita el TDA BST y deja el resultado en la consola. Sirve para la
/// demostracion del TP08: inserta, busca por valor y por clave, saca los tres
/// tipos de nodo (hoja, un hijo, dos hijos), recorre en los tres ordenes y
/// muestra el caso malo: insertar ya ordenado deja una lista.
///
/// El uso en el juego es el catalogo de FabricaDeBloques, que busca el prefab
/// de cada bloque por su id.
///
/// Se ejecuta solo al arrancar la escena, o a pedido desde el menu contextual
/// del componente.
/// </summary>
public class PruebaBST : MonoBehaviour
{
    [Tooltip("Ejecutar apenas arranca la escena.")]
    public bool correrAlIniciar = true;

    private class Item : IComparable<Item>
    {
        public string Nombre;
        public int Precio;

        public Item(string nombre, int precio)
        {
            Nombre = nombre;
            Precio = precio;
        }

        public int CompareTo(Item otro) => string.CompareOrdinal(Nombre, otro.Nombre);
    }

    private StringBuilder salida;
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

        Titulo("BST de enteros");
        ProbarContrato();
        ProbarRemove();
        ProbarCasosBorde();

        Titulo("Buscar por una clave distinta del valor: Item por nombre");
        ProbarBusquedaPorClave();

        Titulo("El peor caso");
        MostrarDegeneracion();

        salida.AppendLine();
        salida.AppendLine($"{pruebasCorridas - pruebasFalladas} de {pruebasCorridas} comprobaciones pasaron.");

        if (pruebasFalladas > 0)
            Debug.LogError(salida.ToString());
        else
            Debug.Log(salida.ToString());
    }

    private static BST<int, int> NuevoArbolDeEnteros() => new BST<int, int>((clave, valor) => clave.CompareTo(valor));

    //        50
    //      /    \
    //    30      70
    //   /  \    /  \
    //  20  40  60  80
    private static BST<int, int> ArbolCompleto()
    {
        var arbol = NuevoArbolDeEnteros();
        foreach (int v in new[] { 50, 30, 70, 20, 40, 60, 80 }) arbol.Add(v);
        return arbol;
    }

    private void ProbarContrato()
    {
        var arbol = NuevoArbolDeEnteros();
        Verificar("Arranca vacio", arbol.Count == 0 && arbol.IsEmpty && arbol.Height == 0);

        arbol = ArbolCompleto();
        Verificar("Add suma los valores", arbol.Count == 7 && !arbol.IsEmpty);
        Verificar("Un arbol parejo de 7 tiene 3 niveles", arbol.Height == 3);
        Verificar("Add de un repetido devuelve false y no cambia nada", !arbol.Add(40) && arbol.Count == 7);
        Verificar("Contains encuentra", arbol.Contains(60) && arbol.Contains(20));
        Verificar("Contains niega lo ausente", !arbol.Contains(65));
        Verificar("Min y Max", arbol.Min() == 20 && arbol.Max() == 80);

        Verificar("InOrder devuelve de menor a mayor",
            arbol.InOrder().SequenceEqual(new[] { 20, 30, 40, 50, 60, 70, 80 }));
        Verificar("PreOrder: el padre antes que los hijos",
            arbol.PreOrder().SequenceEqual(new[] { 50, 30, 20, 40, 70, 60, 80 }));
        Verificar("PostOrder: los hijos antes que el padre",
            arbol.PostOrder().SequenceEqual(new[] { 20, 40, 30, 60, 80, 70, 50 }));

        arbol.Clear();
        Verificar("Clear vacia", arbol.Count == 0 && !arbol.Contains(50) && !arbol.InOrder().Any());
    }

    private void ProbarRemove()
    {
        var arbol = ArbolCompleto();
        Verificar("Remove de una hoja", arbol.Remove(20) && !arbol.Contains(20) && arbol.Count == 6);

        // 30 quedo con un solo hijo (40): 40 ocupa su lugar
        Verificar("Remove de un nodo con un hijo", arbol.Remove(30) && arbol.Contains(40) && arbol.Count == 5);

        // 70 tiene dos hijos: lo reemplaza su sucesor (80)
        Verificar("Remove de un nodo con dos hijos", arbol.Remove(70) && arbol.Contains(60) && arbol.Contains(80));
        Verificar("Despues de sacar sigue ordenado", arbol.InOrder().SequenceEqual(new[] { 40, 50, 60, 80 }));

        Verificar("Remove de la raiz", arbol.Remove(50) && arbol.InOrder().SequenceEqual(new[] { 40, 60, 80 }));
        Verificar("Remove de un ausente devuelve false", !arbol.Remove(999) && arbol.Count == 3);

        arbol.Remove(40); arbol.Remove(60); arbol.Remove(80);
        Verificar("Se puede vaciar sacando de a uno", arbol.IsEmpty && arbol.Height == 0);

        arbol.Add(1);
        Verificar("Y volver a usar", arbol.Contains(1) && arbol.Count == 1);
    }

    private void ProbarCasosBorde()
    {
        var arbol = NuevoArbolDeEnteros();
        Verificar("Vacio: Contains devuelve false", !arbol.Contains(1));
        Verificar("Vacio: Remove devuelve false", !arbol.Remove(1));
        Verificar("Vacio: TryGet devuelve false", !arbol.TryGet(1, out _));
        Verificar("Vacio: Min tira InvalidOperationException", Tira<InvalidOperationException>(() => arbol.Min()));
        Verificar("Vacio: Max tira InvalidOperationException", Tira<InvalidOperationException>(() => arbol.Max()));

        var textos = new BST<string, string>(string.CompareOrdinal);
        Verificar("Add de null tira ArgumentNullException", Tira<ArgumentNullException>(() => textos.Add(null)));
        Verificar("TryGet de clave null tira ArgumentNullException",
            Tira<ArgumentNullException>(() => textos.TryGet(null, out _)));
        Verificar("El constructor rechaza un Func null",
            Tira<ArgumentNullException>(() => new BST<int, int>(null)));
    }

    private void ProbarBusquedaPorClave()
    {
        var items = new BST<string, Item>((nombre, item) => string.CompareOrdinal(nombre, item.Nombre));
        items.Add(new Item("pocion", 10));
        items.Add(new Item("espada", 50));
        items.Add(new Item("escudo", 40));
        items.Add(new Item("llave", 5));

        Verificar("TryGet encuentra por nombre y devuelve el item",
            items.TryGet("escudo", out Item escudo) && escudo.Precio == 40);
        Verificar("TryGet devuelve false si no esta", !items.TryGet("arco", out Item arco) && arco == null);
        Verificar("ContainsKey", items.ContainsKey("llave") && !items.ContainsKey("arco"));
        Verificar("El mismo nombre no se repite", !items.Add(new Item("espada", 999)) && items.Count == 4);
        Verificar("InOrder los da ordenados por nombre",
            items.InOrder().Select(i => i.Nombre).SequenceEqual(new[] { "escudo", "espada", "llave", "pocion" }));
    }

    private void MostrarDegeneracion()
    {
        const int n = 200;
        var ordenado = NuevoArbolDeEnteros();
        for (int i = 0; i < n; i++) ordenado.Add(i);

        var mezclado = NuevoArbolDeEnteros();
        var azar = new System.Random(1);
        foreach (int v in Enumerable.Range(0, n).OrderBy(_ => azar.Next())) mezclado.Add(v);

        salida.AppendLine($"  Con {n} valores:");
        salida.AppendLine($"    insertados en orden    altura {ordenado.Height,4}   (una lista: O(n))");
        salida.AppendLine($"    insertados al azar     altura {mezclado.Height,4}   (cerca de log2 n = {Mathf.CeilToInt(Mathf.Log(n, 2))})");
        salida.AppendLine("  El BST no se defiende de un orden de carga malo. El AVL (TP09) rota para");
        salida.AppendLine("  que la altura quede siempre cerca de log2 n.");

        Verificar("En orden el arbol degenera a altura n", ordenado.Height == n);
        Verificar("Al azar la altura es mucho menor que n", mezclado.Height < n / 4);
    }

    private static bool Tira<TExcepcion>(Action accion) where TExcepcion : Exception
    {
        try
        {
            accion();
            return false;
        }
        catch (TExcepcion)
        {
            return true;
        }
        catch
        {
            return false;
        }
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
