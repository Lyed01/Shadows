using System;
using System.Diagnostics;
using System.Linq;
using System.Text;
using UnityEngine;
using Debug = UnityEngine.Debug;

/// <summary>
/// Ejercita las dos implementaciones del TDA Queue y deja el resultado en la
/// consola (y en UltimaSalida, que lo muestra el panel de la escena). Incluye
/// la medicion que justifica elegir la dinamica: desencolar en el arreglo
/// corre todos los elementos.
/// </summary>
public class PruebaColas : MonoBehaviour
{
    [Tooltip("Cantidad de elementos con la que se comparan las dos implementaciones.")]
    public int elementosDeLaMedicion = 5000;

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

        Titulo("SimpleArrayQueue");
        Probar(new SimpleArrayQueue<int>());

        Titulo("SimpleLinkedQueue");
        Probar(new SimpleLinkedQueue<int>());

        Titulo("Comparacion de tiempos");
        CompararTiempos();

        salida.AppendLine();
        salida.AppendLine($"{pruebasCorridas - pruebasFalladas} de {pruebasCorridas} comprobaciones pasaron.");

        if (pruebasFalladas > 0)
            Debug.LogError(salida.ToString());
        else
            Debug.Log(salida.ToString());
    }

    private void Probar(ISimpleQueue<int> cola)
    {
        Verificar("Arranca vacia", cola.Count == 0 && cola.IsEmpty);
        Verificar("Dequeue de cola vacia tira InvalidOperationException", Tira<InvalidOperationException>(() => cola.Dequeue()));
        Verificar("Peek de cola vacia tira InvalidOperationException", Tira<InvalidOperationException>(() => cola.Peek()));
        Verificar("ToArray de cola vacia devuelve vacio", cola.ToArray().Length == 0);

        cola.Enqueue(1);
        cola.Enqueue(2);
        cola.Enqueue(3);
        Verificar("Enqueue suma elementos", cola.Count == 3 && !cola.IsEmpty);
        Verificar("Peek devuelve el frente", cola.Peek() == 1);
        Verificar("Peek no saca nada", cola.Count == 3);
        Verificar("ToArray va del frente al final", cola.ToArray().SequenceEqual(new[] { 1, 2, 3 }));
        Verificar("foreach va del frente al final", cola.SequenceEqual(new[] { 1, 2, 3 }));

        Verificar("Dequeue devuelve el primero que entro (FIFO)", cola.Dequeue() == 1);
        Verificar("Dequeue acorta la cola", cola.Count == 2 && cola.Peek() == 2);

        for (int i = 10; i < 30; i++) cola.Enqueue(i);
        Verificar("Crece mas alla de la capacidad inicial", cola.Count == 22 && cola.Peek() == 2);

        bool ordenOk = cola.Dequeue() == 2 && cola.Dequeue() == 3;
        for (int i = 10; i < 30; i++) if (cola.Dequeue() != i) ordenOk = false;
        Verificar("Sale todo en el orden de entrada", ordenOk && cola.IsEmpty);

        // En la dinamica es donde se rompe el puntero al ultimo si Dequeue no lo limpia
        cola.Enqueue(5);
        cola.Dequeue();
        cola.Enqueue(6);
        Verificar("Encolar despues de vaciarla no pierde el elemento", cola.Count == 1 && cola.Peek() == 6);

        cola.Clear();
        Verificar("Clear vacia", cola.Count == 0 && cola.IsEmpty && !cola.Any());

        cola.Enqueue(7);
        Verificar("Vuelve a aceptar altas despues de Clear", cola.Peek() == 7 && cola.Count == 1);
    }

    private void CompararTiempos()
    {
        int n = Mathf.Max(100, elementosDeLaMedicion);
        var arreglo = new SimpleArrayQueue<int>();
        var enlazada = new SimpleLinkedQueue<int>();

        for (int i = 0; i < n; i++) { arreglo.Enqueue(i); enlazada.Enqueue(i); }

        long sacarArreglo = Medir(() => { while (!arreglo.IsEmpty) arreglo.Dequeue(); });
        long sacarEnlazada = Medir(() => { while (!enlazada.IsEmpty) enlazada.Dequeue(); });

        salida.AppendLine($"Con {n} elementos:");
        salida.AppendLine();
        salida.AppendLine("  Desencolar todo     arreglo O(n) por baja, enlazada O(1)");
        salida.AppendLine($"    SimpleArrayQueue    {sacarArreglo,8} ms");
        salida.AppendLine($"    SimpleLinkedQueue   {sacarEnlazada,8} ms");
        salida.AppendLine();
        salida.AppendLine("  Para una cola que se consume siempre desde el frente conviene la dinamica.");
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
        try { accion(); return false; }
        catch (TExcepcion) { return true; }
        catch { return false; }
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
