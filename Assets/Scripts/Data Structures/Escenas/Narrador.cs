using System.Text;
using UnityEngine;

/// <summary>
/// Relato en vivo de lo que hacen las estructuras mientras se juega. El codigo del
/// juego avisa con Decir cuando usa una estructura propia y el panel del costado
/// muestra las ultimas lineas. Los mensajes se guardan en una cola propia
/// (SimpleLinkedQueue): entran al final y, al llenarse, sale el mas viejo.
/// Si no hay una demo activa, Decir no guarda nada.
/// </summary>
public static class Narrador
{
    private const int Capacidad = 24;

    private static readonly ISimpleQueue<string> lineas = new SimpleLinkedQueue<string>();

    public static bool Activo { get; set; }

    /// <summary>Anota un evento de la estructura del TP indicado.</summary>
    public static void Decir(int tp, string texto)
    {
        if (!Activo) return;

        lineas.Enqueue($"{Time.time,6:0.0}s  [TP{tp:00}] {texto}");
        while (lineas.Count > Capacidad) lineas.Dequeue();
    }

    public static void Limpiar() => lineas.Clear();

    /// <summary>Las ultimas lineas, de la mas nueva a la mas vieja.</summary>
    public static string Reciente(int maximo)
    {
        string[] todas = lineas.ToArray();
        var sb = new StringBuilder();
        for (int i = todas.Length - 1; i >= 0 && i >= todas.Length - maximo; i--)
            sb.AppendLine(todas[i]);
        return sb.ToString();
    }
}
