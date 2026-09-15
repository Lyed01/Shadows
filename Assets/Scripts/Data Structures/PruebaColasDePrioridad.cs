using System;
using System.Diagnostics;
using System.Text;
using UnityEngine;

/// <summary>
/// Ejercita las dos implementaciones del TDA Priority Queue y deja el
/// resultado en la consola. Sirve para la demostracion del TP: muestra que
/// ambas cumplen el mismo contrato y mide en que caso conviene cada una.
///
/// Se ejecuta solo al arrancar la escena, o a pedido desde el menu contextual
/// del componente.
/// </summary>
public class PruebaColasDePrioridad : MonoBehaviour
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

        Titulo("SimpleArrayPriorityQueue");
        ProbarContrato(new SimpleArrayPriorityQueue<string>());
        ProbarCasosBorde(new SimpleArrayPriorityQueue<string>());

        Titulo("SimpleLinkedPriorityQueue");
        ProbarContrato(new SimpleLinkedPriorityQueue<string>());
        ProbarCasosBorde(new SimpleLinkedPriorityQueue<string>());

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

    private void ProbarContrato(ISimplePriorityQueue<string> cola)
    {
        Verificar("Arranca vacia", cola.Count == 0);

        cola.Enqueue("tarea comun", 1f);
        cola.Enqueue("tarea urgente", 10f);
        cola.Enqueue("tarea media", 5f);
        Verificar("Enqueue deja tres elementos", cola.Count == 3);

        Verificar("Peek devuelve el de mayor prioridad", cola.Peek() == "tarea urgente");
        Verificar("PeekPriority devuelve su prioridad", cola.PeekPriority() == 10f);
        Verificar("Peek no saca el elemento", cola.Count == 3);

        Verificar("Contains encuentra", cola.Contains("tarea media"));
        Verificar("Contains niega lo ausente", !cola.Contains("no existe"));

        string primero = cola.Dequeue();
        Verificar("Dequeue devuelve el de mayor prioridad", primero == "tarea urgente");
        Verificar("Dequeue acorta la cola", cola.Count == 2);

        string segundo = cola.Dequeue();
        Verificar("Dequeue respeta el orden de prioridad", segundo == "tarea media");

        cola.Enqueue("otra urgente", 10f);
        cola.Enqueue("otra comun", 1f);
        Verificar("Sigue aceptando altas despues de sacar elementos", cola.Count == 3);

        int recorridos = 0;
        foreach (var _ in cola) recorridos++;
        Verificar("foreach recorre todo", recorridos == cola.Count);

        cola.Clear();
        Verificar("Clear vacia", cola.Count == 0);
    }

    private void ProbarCasosBorde(ISimplePriorityQueue<string> cola)
    {
        Verificar("Cola vacia: Dequeue tira excepcion",
            Tira<InvalidOperationException>(() => cola.Dequeue()));

        Verificar("Cola vacia: Peek tira excepcion",
            Tira<InvalidOperationException>(() => cola.Peek()));

        Verificar("Cola vacia: Contains devuelve false", !cola.Contains("nada"));

        int recorridos = 0;
        foreach (var _ in cola) recorridos++;
        Verificar("Cola vacia: foreach no itera", recorridos == 0);

        cola.Enqueue("unico", 3f);
        Verificar("Un elemento: es el primero", cola.Peek() == "unico" && cola.Count == 1);

        // Empate de prioridad: alcanza con que ambos salgan, sin importar el orden
        cola.Enqueue("empate a", 7f);
        cola.Enqueue("empate b", 7f);
        Verificar("Prioridades empatadas: entran las dos", cola.Count == 3);
        cola.Dequeue();
        cola.Dequeue();
        Verificar("Prioridades empatadas: ambas salen antes que 'unico'", cola.Peek() == "unico");

        cola.Dequeue();
        Verificar("Sacar el unico deja la cola vacia", cola.Count == 0);

        // Encolar despues de vaciar valida que los punteros internos quedaron bien
        cola.Enqueue("despues de vaciar", 1f);
        Verificar("Vuelve a aceptar altas despues de vaciarse",
            cola.Count == 1 && cola.Peek() == "despues de vaciar");
    }

    /// <summary>
    /// Mide los dos casos donde la eleccion de implementacion cambia el
    /// costo: encolar muchos elementos y consumirlos todos por prioridad.
    /// </summary>
    private void CompararTiempos()
    {
        int n = Mathf.Max(100, elementosDeLaMedicion);
        var rng = new System.Random(12345);

        salida.AppendLine($"Con {n} elementos:");
        salida.AppendLine();

        long encolarArreglo = Medir(() =>
        {
            var cola = new SimpleArrayPriorityQueue<int>();
            for (int i = 0; i < n; i++) cola.Enqueue(i, rng.Next());
        });

        long encolarEnlazada = Medir(() =>
        {
            var cola = new SimpleLinkedPriorityQueue<int>();
            for (int i = 0; i < n; i++) cola.Enqueue(i, rng.Next());
        });

        salida.AppendLine("  Encolar n elementos          arreglo O(1) amortizado por alta, enlazada O(n)");
        salida.AppendLine($"    SimpleArrayPriorityQueue   {encolarArreglo,8} ms");
        salida.AppendLine($"    SimpleLinkedPriorityQueue  {encolarEnlazada,8} ms");
        salida.AppendLine();

        var arregloLleno = new SimpleArrayPriorityQueue<int>();
        var enlazadaLlena = new SimpleLinkedPriorityQueue<int>();
        for (int i = 0; i < n; i++)
        {
            int prioridad = rng.Next();
            arregloLleno.Enqueue(i, prioridad);
            enlazadaLlena.Enqueue(i, prioridad);
        }

        long consumirArreglo = Medir(() =>
        {
            while (arregloLleno.Count > 0) arregloLleno.Dequeue();
        });

        long consumirEnlazada = Medir(() =>
        {
            while (enlazadaLlena.Count > 0) enlazadaLlena.Dequeue();
        });

        salida.AppendLine("  Consumir todo por prioridad  arreglo O(n) por baja, enlazada O(1)");
        salida.AppendLine($"    SimpleArrayPriorityQueue   {consumirArreglo,8} ms");
        salida.AppendLine($"    SimpleLinkedPriorityQueue  {consumirEnlazada,8} ms");
        salida.AppendLine();
        salida.AppendLine("  Por eso la cola de sonidos del AudioManager, que encola un pedido de FX");
        salida.AppendLine("  por cada evento del juego y solo extrae unos pocos por frame, usa la");
        salida.AppendLine("  implementacion estatica: lo que mas se repite es el alta, no la baja.");
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
