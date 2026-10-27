using System;

/// <summary>Los tres algoritmos de ordenamiento vistos en clase.</summary>
public enum AlgoritmoDeOrden
{
    Bubble,
    Selection,
    Quick
}

/// <summary>Sentido del ordenamiento.</summary>
public enum SentidoDeOrden
{
    Ascendente,
    Descendente
}

/// <summary>
/// Ordenamiento generico sobre arreglos y sobre las listas propias del
/// proyecto. No usa List de C# ni Array.Sort: los algoritmos son los de clase,
/// escritos a mano sobre un arreglo con Swap.
///
/// Admite las dos formas de comparar vistas en clase:
///   - Una Comparison(T) recibida por parametro, para cualquier tipo y criterio.
///   - El orden natural de los tipos que implementan IComparable(T).
///
/// En ambos casos se puede pedir orden ascendente o descendente.
///
///   Bubble     O(n^2)  compara pares vecinos y los intercambia si estan mal.
///   Selection  O(n^2)  busca el minimo y lo lleva adelante. Menos swaps que Bubble.
///   Quick      O(n log n) en promedio, O(n^2) en el peor caso. Divide-and-conquer.
/// </summary>
public static class CollectionSort
{
    // === ARREGLOS ===

    /// <summary>Ordena el arreglo completo con el criterio recibido.</summary>
    public static void SortArray<T>(T[] col, Comparison<T> comparacion,
        SentidoDeOrden sentido = SentidoDeOrden.Ascendente,
        AlgoritmoDeOrden algoritmo = AlgoritmoDeOrden.Quick)
    {
        if (col == null)
            throw new ArgumentNullException(nameof(col));

        Ordenar(col, col.Length, comparacion, sentido, algoritmo);
    }

    /// <summary>Ordena el arreglo completo segun el orden natural del tipo.</summary>
    public static void SortArray<T>(T[] col,
        SentidoDeOrden sentido = SentidoDeOrden.Ascendente,
        AlgoritmoDeOrden algoritmo = AlgoritmoDeOrden.Quick) where T : IComparable<T>
    {
        SortArray(col, CompararNatural, sentido, algoritmo);
    }

    // === LISTAS PROPIAS ===

    /// <summary>
    /// Ordena la lista con el criterio recibido. Delega en el Sort de la propia
    /// lista, porque cada implementacion sabe como hacerlo sin pagar el costo
    /// de su indexador.
    /// </summary>
    public static void SortList<T>(ISimpleList<T> lista, Comparison<T> comparacion,
        SentidoDeOrden sentido = SentidoDeOrden.Ascendente,
        AlgoritmoDeOrden algoritmo = AlgoritmoDeOrden.Quick)
    {
        if (lista == null)
            throw new ArgumentNullException(nameof(lista));

        lista.Sort(comparacion, sentido, algoritmo);
    }

    /// <summary>Ordena la lista segun el orden natural del tipo.</summary>
    public static void SortList<T>(ISimpleList<T> lista,
        SentidoDeOrden sentido = SentidoDeOrden.Ascendente,
        AlgoritmoDeOrden algoritmo = AlgoritmoDeOrden.Quick) where T : IComparable<T>
    {
        SortList(lista, CompararNatural, sentido, algoritmo);
    }

    // === NUCLEO ===

    /// <summary>
    /// Ordena las primeras cantidad posiciones del arreglo. Recibe la cantidad
    /// aparte porque SimpleArrayList ordena su arreglo interno, que suele tener
    /// celdas libres al final.
    /// </summary>
    internal static void Ordenar<T>(T[] col, int cantidad, Comparison<T> comparacion,
        SentidoDeOrden sentido, AlgoritmoDeOrden algoritmo)
    {
        if (comparacion == null)
            throw new ArgumentNullException(nameof(comparacion));

        if (cantidad < 2) return;

        // Para ordenar al reves alcanza con invertir los argumentos. No se
        // niega el resultado porque -int.MinValue desborda.
        Comparison<T> cmp = sentido == SentidoDeOrden.Descendente
            ? (a, b) => comparacion(b, a)
            : comparacion;

        switch (algoritmo)
        {
            case AlgoritmoDeOrden.Bubble:
                BubbleSort(col, cantidad, cmp);
                break;
            case AlgoritmoDeOrden.Selection:
                SelectionSort(col, cantidad, cmp);
                break;
            case AlgoritmoDeOrden.Quick:
                QuickSort(col, 0, cantidad - 1, cmp);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(algoritmo));
        }
    }

    /// <summary>
    /// "Las burbujas grandes suben hasta el final". El primer for no llega al
    /// ultimo elemento porque despues se lee col[j + 1]; el segundo evita los
    /// i elementos que ya quedaron ordenados al final.
    /// </summary>
    private static void BubbleSort<T>(T[] col, int cantidad, Comparison<T> cmp)
    {
        for (int i = 0; i < cantidad - 1; i++)
            for (int j = 0; j < cantidad - i - 1; j++)
                if (cmp(col[j], col[j + 1]) > 0)
                    Swap(col, j, j + 1);
    }

    /// <summary>
    /// "Selecciona al mas chico y lo lleva adelante". Hace a lo sumo un swap
    /// por vuelta, mientras que Bubble puede hacer uno por cada comparacion.
    /// </summary>
    private static void SelectionSort<T>(T[] col, int cantidad, Comparison<T> cmp)
    {
        for (int i = 0; i < cantidad - 1; i++)
        {
            int minIndex = i;

            for (int j = i + 1; j < cantidad; j++)
                if (cmp(col[j], col[minIndex]) < 0)
                    minIndex = j;

            Swap(col, i, minIndex);
        }
    }

    /// <summary>
    /// Conquer: ordena cada mitad por separado. El caso base es un tramo de un
    /// solo elemento (o ninguno). El merge es implicito: cuando todas las
    /// llamadas terminan, el arreglo ya esta ordenado.
    /// </summary>
    private static void QuickSort<T>(T[] col, int inicio, int fin, Comparison<T> cmp)
    {
        if (inicio >= fin) return;

        int pivot = Partition(col, inicio, fin, cmp);

        QuickSort(col, inicio, pivot - 1, cmp);
        QuickSort(col, pivot + 1, fin, cmp);
    }

    /// <summary>
    /// Divide: toma el ultimo elemento como pivot y deja a su izquierda todos
    /// los menores y a su derecha los mayores o iguales. Devuelve la posicion
    /// final del pivot, que ya no se vuelve a mover.
    /// </summary>
    private static int Partition<T>(T[] col, int inicio, int fin, Comparison<T> cmp)
    {
        T pivot = col[fin];
        int i = inicio - 1;

        for (int j = inicio; j < fin; j++)
        {
            if (cmp(col[j], pivot) < 0)
            {
                i++;
                Swap(col, i, j);
            }
        }

        Swap(col, i + 1, fin);
        return i + 1;
    }

    private static void Swap<T>(T[] col, int a, int b)
    {
        T aux = col[a];
        col[a] = col[b];
        col[b] = aux;
    }

    /// <summary>
    /// Orden natural de un IComparable. Un null queda antes que cualquier valor,
    /// igual que en el Comparer de C#, para no tirar NullReferenceException.
    /// </summary>
    private static int CompararNatural<T>(T a, T b) where T : IComparable<T>
    {
        if (a == null) return b == null ? 0 : -1;
        if (b == null) return 1;
        return a.CompareTo(b);
    }
}
