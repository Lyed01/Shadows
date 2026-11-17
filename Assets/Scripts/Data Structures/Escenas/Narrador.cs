using System.Text;
using UnityEngine;

/// <summary>
/// Relato en vivo de lo que hacen las estructuras mientras se juega. El codigo del
/// juego avisa con Decir cuando usa una estructura propia; el panel del costado
/// muestra el ultimo mensaje de cada TP (una fila fija por estructura) y, aparte,
/// los ultimos eventos en orden. El historial se guarda en una cola propia
/// (SimpleLinkedQueue): entran al final y, al llenarse, sale el mas viejo.
/// Si no hay una demo activa, Decir no guarda nada.
/// </summary>
public static class Narrador
{
    private const int Capacidad = 24;

    private static readonly ISimpleQueue<string> lineas = new SimpleLinkedQueue<string>();
    private static readonly string[] ultimoPorTp = new string[11];
    private static readonly float[] cuandoPorTp = new float[11];

    public static bool Activo { get; set; }

    /// <summary>Anota un evento de la estructura del TP indicado (1 a 10).</summary>
    public static void Decir(int tp, string texto)
    {
        if (!Activo) return;

        // "Pila: Push..." -> la fila del panel ya dice "Pila", se guarda solo lo que sigue
        int corte = texto.IndexOf(": ");
        string sinPrefijo = corte > 0 && corte < 30 ? texto.Substring(corte + 2) : texto;

        if (tp >= 1 && tp <= 10)
        {
            ultimoPorTp[tp] = sinPrefijo;
            cuandoPorTp[tp] = Time.time;
        }

        lineas.Enqueue($"<color=#8a93a6>{Time.time,5:0.0}s</color> TP{tp:00}  {sinPrefijo}");
        while (lineas.Count > Capacidad) lineas.Dequeue();
    }

    public static void Limpiar()
    {
        lineas.Clear();
        System.Array.Clear(ultimoPorTp, 0, ultimoPorTp.Length);
        System.Array.Clear(cuandoPorTp, 0, cuandoPorTp.Length);
    }

    /// <summary>Ultimo mensaje del TP, o null si todavia no hizo nada.</summary>
    public static string Ultimo(int tp) => ultimoPorTp[tp];

    /// <summary>Hace cuantos segundos fue el ultimo mensaje del TP.</summary>
    public static float HaceCuanto(int tp) => Time.time - cuandoPorTp[tp];

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
