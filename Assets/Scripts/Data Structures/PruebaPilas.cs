using System;
using System.Linq;
using System.Text;
using UnityEngine;

/// <summary>
/// Ejercita las dos implementaciones del TDA Stack y deja el resultado en la
/// consola (y en UltimaSalida, que lo muestra el panel de la escena).
/// </summary>
public class PruebaPilas : MonoBehaviour
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

        Titulo("SimpleArrayStack");
        Probar(new SimpleArrayStack<int>());

        Titulo("SimpleLinkedStack");
        Probar(new SimpleLinkedStack<int>());

        salida.AppendLine();
        salida.AppendLine($"{pruebasCorridas - pruebasFalladas} de {pruebasCorridas} comprobaciones pasaron.");

        if (pruebasFalladas > 0)
            Debug.LogError(salida.ToString());
        else
            Debug.Log(salida.ToString());
    }

    private void Probar(ISimpleStack<int> pila)
    {
        Verificar("Arranca vacia", pila.Count == 0 && pila.IsEmpty);
        Verificar("Pop de pila vacia tira InvalidOperationException", Tira<InvalidOperationException>(() => pila.Pop()));
        Verificar("Peek de pila vacia tira InvalidOperationException", Tira<InvalidOperationException>(() => pila.Peek()));
        Verificar("ToArray de pila vacia devuelve vacio", pila.ToArray().Length == 0);

        pila.Push(1);
        pila.Push(2);
        pila.Push(3);
        Verificar("Push suma elementos", pila.Count == 3 && !pila.IsEmpty);
        Verificar("Peek devuelve la cima", pila.Peek() == 3);
        Verificar("Peek no saca nada", pila.Count == 3);
        Verificar("ToArray va de la cima al fondo", pila.ToArray().SequenceEqual(new[] { 3, 2, 1 }));
        Verificar("foreach va de la cima al fondo", pila.SequenceEqual(new[] { 3, 2, 1 }));

        Verificar("Pop devuelve el ultimo que entro (LIFO)", pila.Pop() == 3);
        Verificar("Pop acorta la pila", pila.Count == 2 && pila.Peek() == 2);

        // Crecer pasa la capacidad inicial de la estatica
        for (int i = 10; i < 30; i++) pila.Push(i);
        Verificar("Crece mas alla de la capacidad inicial", pila.Count == 22 && pila.Peek() == 29);

        bool ordenOk = true;
        for (int i = 29; i >= 10; i--) if (pila.Pop() != i) ordenOk = false;
        Verificar("Sale todo en orden inverso al de entrada", ordenOk && pila.Pop() == 2 && pila.Pop() == 1);
        Verificar("Se vacia sacando todo", pila.IsEmpty);

        pila.Push(5);
        pila.Clear();
        Verificar("Clear vacia", pila.Count == 0 && pila.IsEmpty && !pila.Any());

        pila.Push(7);
        Verificar("Vuelve a aceptar altas despues de Clear", pila.Peek() == 7 && pila.Count == 1);
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
