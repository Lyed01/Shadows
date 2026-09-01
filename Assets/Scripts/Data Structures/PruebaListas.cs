using System;
using System.Diagnostics;
using System.Text;
using UnityEngine;

/// <summary>
/// Ejercita las dos implementaciones del TDA List y deja el resultado en la
/// consola. Sirve para la demostracion del TP: muestra que ambas cumplen el
/// mismo contrato y mide en que caso conviene cada una.
///
/// Se ejecuta solo al arrancar la escena, o a pedido desde el menu contextual
/// del componente.
/// </summary>
public class PruebaListas : MonoBehaviour
{
    [Header("Medicion de tiempos")]
    [Tooltip("Cantidad de elementos con la que se comparan las dos implementaciones.")]
    public int elementosDeLaMedicion = 5000;

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

        Titulo("SimpleArrayList");
        ProbarContrato(new SimpleArrayList<string>());
        ProbarCasosBorde(new SimpleArrayList<string>());

        Titulo("SimpleLinkedList");
        ProbarContrato(new SimpleLinkedList<string>());
        ProbarCasosBorde(new SimpleLinkedList<string>());

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

    private void ProbarContrato(ISimpleList<string> lista)
    {
        Verificar("Arranca vacia", lista.Count == 0);

        lista.Add("a");
        lista.Add("b");
        lista.Add("c");
        Verificar("Add deja tres elementos", lista.Count == 3);
        Verificar("Respeta el orden de alta", lista[0] == "a" && lista[2] == "c");

        lista.Insert(1, "x");
        Verificar("Insert corre el resto", lista[1] == "x" && lista[2] == "b" && lista.Count == 4);

        lista.Insert(lista.Count, "z");
        Verificar("Insert al final agrega", lista[lista.Count - 1] == "z");

        Verificar("IndexOf encuentra", lista.IndexOf("b") == 2);
        Verificar("IndexOf devuelve -1 si no esta", lista.IndexOf("no existe") == -1);
        Verificar("Contains encuentra", lista.Contains("x"));
        Verificar("Contains niega lo ausente", !lista.Contains("no existe"));

        Verificar("Remove devuelve true", lista.Remove("x"));
        Verificar("Remove saca el elemento", !lista.Contains("x") && lista.Count == 4);
        Verificar("Remove devuelve false si no esta", !lista.Remove("no existe"));

        lista.RemoveAt(0);
        Verificar("RemoveAt saca por posicion", lista[0] == "b" && lista.Count == 3);

        string primero = lista.RemoveFirst();
        Verificar("RemoveFirst devuelve el que saca", primero == "b");
        Verificar("RemoveFirst acorta la lista", lista.Count == 2);

        lista[0] = "modificado";
        Verificar("El indexador escribe", lista[0] == "modificado");

        int recorridos = 0;
        foreach (var _ in lista) recorridos++;
        Verificar("foreach recorre todo", recorridos == lista.Count);

        lista.Clear();
        Verificar("Clear vacia", lista.Count == 0);
    }

    private void ProbarCasosBorde(ISimpleList<string> lista)
    {
        Verificar("Lista vacia: RemoveFirst tira excepcion",
            Tira<InvalidOperationException>(() => lista.RemoveFirst()));

        Verificar("Lista vacia: leer indice 0 tira excepcion",
            Tira<ArgumentOutOfRangeException>(() => { var _ = lista[0]; }));

        Verificar("Lista vacia: IndexOf devuelve -1", lista.IndexOf("nada") == -1);

        int recorridos = 0;
        foreach (var _ in lista) recorridos++;
        Verificar("Lista vacia: foreach no itera", recorridos == 0);

        lista.Add("unico");
        Verificar("Un elemento: es el primero y el ultimo",
            lista[0] == "unico" && lista.Count == 1);

        Verificar("Un elemento: indice 1 tira excepcion",
            Tira<ArgumentOutOfRangeException>(() => { var _ = lista[1]; }));

        Verificar("Un elemento: indice negativo tira excepcion",
            Tira<ArgumentOutOfRangeException>(() => { var _ = lista[-1]; }));

        Verificar("Un elemento: Insert fuera de rango tira excepcion",
            Tira<ArgumentOutOfRangeException>(() => lista.Insert(5, "x")));

        lista.RemoveFirst();
        Verificar("Sacar el unico deja la lista vacia", lista.Count == 0);

        // Agregar despues de vaciar valida que los punteros internos quedaron bien
        lista.Add("despues de vaciar");
        Verificar("Vuelve a aceptar altas despues de vaciarse",
            lista.Count == 1 && lista[0] == "despues de vaciar");
    }

    /// <summary>
    /// Mide los dos casos donde la eleccion de implementacion cambia el costo:
    /// leer por indice y sacar del frente.
    /// </summary>
    private void CompararTiempos()
    {
        int n = Mathf.Max(100, elementosDeLaMedicion);

        var arreglo = new SimpleArrayList<int>();
        var enlazada = new SimpleLinkedList<int>();

        for (int i = 0; i < n; i++)
        {
            arreglo.Add(i);
            enlazada.Add(i);
        }

        salida.AppendLine($"Con {n} elementos:");
        salida.AppendLine();

        long porIndiceArreglo = Medir(() =>
        {
            long suma = 0;
            for (int i = 0; i < arreglo.Count; i++) suma += arreglo[i];
        });

        long porIndiceEnlazada = Medir(() =>
        {
            long suma = 0;
            for (int i = 0; i < enlazada.Count; i++) suma += enlazada[i];
        });

        salida.AppendLine("  Recorrer por indice          arreglo O(1) por acceso, enlazada O(n)");
        salida.AppendLine($"    SimpleArrayList   {porIndiceArreglo,8} ms");
        salida.AppendLine($"    SimpleLinkedList  {porIndiceEnlazada,8} ms");
        salida.AppendLine();

        long sacarArreglo = Medir(() =>
        {
            var copia = new SimpleArrayList<int>();
            for (int i = 0; i < n; i++) copia.Add(i);
            while (copia.Count > 0) copia.RemoveFirst();
        });

        long sacarEnlazada = Medir(() =>
        {
            var copia = new SimpleLinkedList<int>();
            for (int i = 0; i < n; i++) copia.Add(i);
            while (copia.Count > 0) copia.RemoveFirst();
        });

        salida.AppendLine("  Consumir desde el frente     arreglo O(n) por baja, enlazada O(1)");
        salida.AppendLine($"    SimpleArrayList   {sacarArreglo,8} ms");
        salida.AppendLine($"    SimpleLinkedList  {sacarEnlazada,8} ms");
        salida.AppendLine();
        salida.AppendLine("  Por eso el registro de puertas del Hub, que se lee por indice en");
        salida.AppendLine("  cada frame, usa la estatica; y la cola de pasos de los NPC, que se");
        salida.AppendLine("  consume siempre desde el frente, usa la dinamica.");
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
