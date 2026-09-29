using System;
using System.Diagnostics;
using System.Text;
using UnityEngine;

/// <summary>
/// Ejercita las dos implementaciones del TDA Set y deja el resultado en la
/// consola. Sirve para la demostracion del TP: muestra que ambas cumplen el
/// mismo contrato y mide como se comportan al crecer.
///
/// Se ejecuta solo al arrancar la escena, o a pedido desde el menu contextual
/// del componente.
/// </summary>
public class PruebaConjuntos : MonoBehaviour
{
    [Header("Medicion de tiempos")]
    [Tooltip("Cantidad de elementos con la que se comparan las dos implementaciones.")]
    public int elementosDeLaMedicion = 2000;

    [Tooltip("Ejecutar apenas arranca la escena.")]
    public bool correrAlIniciar = true;

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

        Titulo("SimpleArraySet");
        ProbarContrato(new SimpleArraySet<string>());
        ProbarCasosBorde(new SimpleArraySet<string>());
        ProbarOperaciones(new SimpleArraySet<string>(), new SimpleArraySet<string>());

        Titulo("SimpleLinkedSet");
        ProbarContrato(new SimpleLinkedSet<string>());
        ProbarCasosBorde(new SimpleLinkedSet<string>());
        ProbarOperaciones(new SimpleLinkedSet<string>(), new SimpleLinkedSet<string>());

        Titulo("Operaciones entre implementaciones distintas");
        ProbarOperaciones(new SimpleArraySet<string>(), new SimpleLinkedSet<string>());

        Titulo("Comparacion de tiempos");
        CompararTiempos();

        salida.AppendLine();
        salida.AppendLine($"{pruebasCorridas - pruebasFalladas} de {pruebasCorridas} comprobaciones pasaron.");

        if (pruebasFalladas > 0)
            UnityEngine.Debug.LogError(salida.ToString());
        else
            UnityEngine.Debug.Log(salida.ToString());
    }

    // Comportamiento que ambas implementaciones deben cumplir igual

    private void ProbarContrato(ISimpleSet<string> conjunto)
    {
        Verificar("Arranca vacio", conjunto.Count == 0 && conjunto.IsEmpty);

        Verificar("Add devuelve true la primera vez", conjunto.Add("a"));
        Verificar("Add devuelve false si ya estaba", !conjunto.Add("a"));
        Verificar("El repetido no se guarda dos veces", conjunto.Count == 1);

        conjunto.Add("b");
        conjunto.Add("c");
        Verificar("Add suma los distintos", conjunto.Count == 3);
        Verificar("Ya no esta vacio", !conjunto.IsEmpty);

        Verificar("Contains encuentra", conjunto.Contains("b"));
        Verificar("Contains niega lo ausente", !conjunto.Contains("no existe"));

        Verificar("Remove devuelve true", conjunto.Remove("b"));
        Verificar("Remove saca el elemento", !conjunto.Contains("b") && conjunto.Count == 2);
        Verificar("Remove devuelve false si no esta", !conjunto.Remove("no existe"));

        // Sacar del medio no debe perder a los demas: la estatica tapa el hueco
        // con el ultimo, y hay que comprobar que ese ultimo sigue estando.
        Verificar("Los otros sobreviven a un Remove",
            conjunto.Contains("a") && conjunto.Contains("c"));

        string[] copia = conjunto.ToArray();
        Verificar("ToArray devuelve todos", copia.Length == 2);
        Verificar("ToArray trae los que corresponden",
            Array.IndexOf(copia, "a") >= 0 && Array.IndexOf(copia, "c") >= 0);

        int recorridos = 0;
        foreach (var _ in conjunto) recorridos++;
        Verificar("foreach recorre todo", recorridos == conjunto.Count);

        conjunto.Clear();
        Verificar("Clear vacia", conjunto.Count == 0 && conjunto.IsEmpty);

        // Volver a agregar valida que los punteros internos quedaron bien
        Verificar("Vuelve a aceptar altas despues de vaciarse", conjunto.Add("despues"));
        Verificar("Y las guarda", conjunto.Count == 1 && conjunto.Contains("despues"));
    }

    private void ProbarCasosBorde(ISimpleSet<string> conjunto)
    {
        Verificar("Conjunto vacio: Contains devuelve false", !conjunto.Contains("nada"));
        Verificar("Conjunto vacio: Remove devuelve false", !conjunto.Remove("nada"));
        Verificar("Conjunto vacio: ToArray devuelve vacio", conjunto.ToArray().Length == 0);

        int recorridos = 0;
        foreach (var _ in conjunto) recorridos++;
        Verificar("Conjunto vacio: foreach no itera", recorridos == 0);

        Verificar("Add de null tira excepcion",
            Tira<ArgumentNullException>(() => conjunto.Add(null)));
        Verificar("Contains de null tira excepcion",
            Tira<ArgumentNullException>(() => conjunto.Contains(null)));
        Verificar("Remove de null tira excepcion",
            Tira<ArgumentNullException>(() => conjunto.Remove(null)));

        Verificar("Operar contra null tira excepcion",
            Tira<ArgumentNullException>(() => conjunto.UnionWith(null)));

        conjunto.Add("unico");
        Verificar("Sacar el unico deja el conjunto vacio",
            conjunto.Remove("unico") && conjunto.IsEmpty);
    }

    /// <summary>
    /// Comprueba las tres operaciones entre conjuntos con el ejemplo de la
    /// clase: a = {1, 2, 3}, b = {3, 4, 5}.
    /// </summary>
    private void ProbarOperaciones(ISimpleSet<string> a, ISimpleSet<string> b)
    {
        a.Clear();
        b.Clear();

        a.Add("1"); a.Add("2"); a.Add("3");
        b.Add("3"); b.Add("4"); b.Add("5");

        ISimpleSet<string> union = a.UnionWith(b);
        Verificar("Union tiene los cinco", union.Count == 5);
        Verificar("Union no repite el compartido", ContieneTodos(union, "1", "2", "3", "4", "5"));

        ISimpleSet<string> interseccion = a.IntersectWith(b);
        Verificar("Interseccion tiene solo el compartido",
            interseccion.Count == 1 && interseccion.Contains("3"));

        ISimpleSet<string> diferencia = a.DifferenceWith(b);
        Verificar("Diferencia a-b deja los propios de a",
            diferencia.Count == 2 && ContieneTodos(diferencia, "1", "2"));

        ISimpleSet<string> alReves = b.DifferenceWith(a);
        Verificar("Diferencia b-a deja los propios de b",
            alReves.Count == 2 && ContieneTodos(alReves, "4", "5"));

        Verificar("Las operaciones no tocan los originales",
            a.Count == 3 && b.Count == 3);
    }

    /// <summary>
    /// Mide el costo de preguntar por pertenencia, que es la operacion que
    /// define al Set y de la que dependen Add y Remove.
    /// </summary>
    private void CompararTiempos()
    {
        int n = Mathf.Max(100, elementosDeLaMedicion);

        var arreglo = new SimpleArraySet<int>();
        var enlazado = new SimpleLinkedSet<int>();

        salida.AppendLine($"Con {n} elementos:");
        salida.AppendLine();

        long altaArreglo = Medir(() => { for (int i = 0; i < n; i++) arreglo.Add(i); });
        long altaEnlazado = Medir(() => { for (int i = 0; i < n; i++) enlazado.Add(i); });

        salida.AppendLine("  Dar de alta n elementos      ambas O(n) por alta: Add llama a Contains");
        salida.AppendLine($"    SimpleArraySet    {altaArreglo,8} ms");
        salida.AppendLine($"    SimpleLinkedSet   {altaEnlazado,8} ms");
        salida.AppendLine();

        long buscarArreglo = Medir(() => { for (int i = 0; i < n; i++) arreglo.Contains(i); });
        long buscarEnlazado = Medir(() => { for (int i = 0; i < n; i++) enlazado.Contains(i); });

        salida.AppendLine("  Preguntar por pertenencia    ambas O(n)");
        salida.AppendLine($"    SimpleArraySet    {buscarArreglo,8} ms");
        salida.AppendLine($"    SimpleLinkedSet   {buscarEnlazado,8} ms");
        salida.AppendLine();
        salida.AppendLine("  Las dos tienen el mismo costo teorico: hay que recorrer igual. La");
        salida.AppendLine("  estatica suele medir mejor porque los datos estan contiguos en");
        salida.AppendLine("  memoria. Por eso el conjunto de habilidades desbloqueadas, que es");
        salida.AppendLine("  chico y se consulta seguido, usa la estatica.");
    }

    private static bool ContieneTodos(ISimpleSet<string> conjunto, params string[] items)
    {
        foreach (string item in items)
            if (!conjunto.Contains(item)) return false;

        return true;
    }

    private static long Medir(Action accion)
    {
        var reloj = Stopwatch.StartNew();
        accion();
        reloj.Stop();
        return reloj.ElapsedMilliseconds;
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
