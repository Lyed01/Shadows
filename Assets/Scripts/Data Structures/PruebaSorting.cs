using System;
using System.Diagnostics;
using System.Text;
using UnityEngine;

/// <summary>
/// Ejercita CollectionSort y el Sort de las dos listas propias y deja el
/// resultado en la consola. Sirve para la demostracion del TP: prueba los tres
/// algoritmos de clase en ambos sentidos, con Comparison y con IComparable, y
/// compara cuanto trabajo hace cada uno.
///
/// Se ejecuta solo al arrancar la escena, o a pedido desde el menu contextual
/// del componente.
/// </summary>
public class PruebaSorting : MonoBehaviour
{
    /// <summary>
    /// Resultado de una partida de un nivel. Su orden natural es por tiempo:
    /// el mas rapido va primero, que es como se arma un ranking de tiempos.
    /// </summary>
    private struct ResultadoDeNivel : IComparable<ResultadoDeNivel>
    {
        public string Nivel;
        public int Estrellas;
        public float Tiempo;

        public ResultadoDeNivel(string nivel, int estrellas, float tiempo)
        {
            Nivel = nivel;
            Estrellas = estrellas;
            Tiempo = tiempo;
        }

        public int CompareTo(ResultadoDeNivel otro) => Tiempo.CompareTo(otro.Tiempo);

        public override string ToString() => $"{Nivel} ({Estrellas}*, {Tiempo}s)";
    }

    private static readonly AlgoritmoDeOrden[] Algoritmos =
        { AlgoritmoDeOrden.Bubble, AlgoritmoDeOrden.Selection, AlgoritmoDeOrden.Quick };

    [Header("Medicion de tiempos")]
    [Tooltip("Cantidad de elementos con la que se comparan los tres algoritmos.")]
    public int elementosDeLaMedicion = 2000;

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

        Titulo("Ejemplo de clase { 9, 5, 7, 1 }");
        ProbarEjemploDeClase();

        Titulo("SortArray");
        foreach (var algoritmo in Algoritmos)
            ProbarArreglo(algoritmo);

        Titulo("SimpleArrayList.Sort");
        foreach (var algoritmo in Algoritmos)
            ProbarLista(new SimpleArrayList<int>(), algoritmo);

        Titulo("SimpleLinkedList.Sort");
        foreach (var algoritmo in Algoritmos)
            ProbarLista(new SimpleLinkedList<int>(), algoritmo);

        Titulo("Datos no numericos");
        ProbarDatosDelJuego();

        Titulo("Casos borde");
        ProbarCasosBorde();

        Titulo("Comparacion de algoritmos");
        CompararAlgoritmos();

        salida.AppendLine();
        salida.AppendLine($"{pruebasCorridas - pruebasFalladas} de {pruebasCorridas} comprobaciones pasaron.");

        if (pruebasFalladas > 0)
            UnityEngine.Debug.LogError(salida.ToString());
        else
            UnityEngine.Debug.Log(salida.ToString());
    }

    /// <summary>El mismo arreglo que se simula paso a paso en la clase.</summary>
    private void ProbarEjemploDeClase()
    {
        foreach (var algoritmo in Algoritmos)
        {
            int[] col = { 9, 5, 7, 1 };
            CollectionSort.SortArray(col, SentidoDeOrden.Ascendente, algoritmo);
            salida.AppendLine($"  {algoritmo,-9} ascendente   {Mostrar(col)}");
            Verificar($"{algoritmo}: queda 1, 5, 7, 9", Iguales(col, 1, 5, 7, 9));

            col = new[] { 9, 5, 7, 1 };
            CollectionSort.SortArray(col, SentidoDeOrden.Descendente, algoritmo);
            salida.AppendLine($"  {algoritmo,-9} descendente  {Mostrar(col)}");
            Verificar($"{algoritmo}: queda 9, 7, 5, 1", Iguales(col, 9, 7, 5, 1));
        }
    }

    private void ProbarArreglo(AlgoritmoDeOrden algoritmo)
    {
        int[] col = Desordenado(50, 1);
        CollectionSort.SortArray(col, SentidoDeOrden.Ascendente, algoritmo);
        Verificar($"{algoritmo}: IComparable ascendente", EstaOrdenado(col, false));

        col = Desordenado(50, 2);
        CollectionSort.SortArray(col, SentidoDeOrden.Descendente, algoritmo);
        Verificar($"{algoritmo}: IComparable descendente", EstaOrdenado(col, true));

        // Una Comparison propia: pares antes que impares, y por valor dentro de cada grupo
        col = new[] { 4, 7, 1, 8, 2, 5 };
        CollectionSort.SortArray(col, ParesPrimero, SentidoDeOrden.Ascendente, algoritmo);
        Verificar($"{algoritmo}: Comparison propia", Iguales(col, 2, 4, 8, 1, 5, 7));

        col = new[] { 3, 1, 3, 2, 1, 3 };
        CollectionSort.SortArray(col, SentidoDeOrden.Ascendente, algoritmo);
        Verificar($"{algoritmo}: respeta repetidos", Iguales(col, 1, 1, 2, 3, 3, 3));
    }

    private void ProbarLista(ISimpleList<int> lista, AlgoritmoDeOrden algoritmo)
    {
        lista.AddRange(Desordenado(50, 3));
        CollectionSort.SortList(lista, SentidoDeOrden.Ascendente, algoritmo);
        Verificar($"{algoritmo}: SortList ascendente", EstaOrdenado(lista.ToArray(), false));
        Verificar($"{algoritmo}: no pierde elementos", lista.Count == 50);

        lista.Sort((a, b) => a.CompareTo(b), SentidoDeOrden.Descendente, algoritmo);
        Verificar($"{algoritmo}: Sort descendente", EstaOrdenado(lista.ToArray(), true));

        // Despues de ordenar la lista tiene que seguir funcionando igual
        lista.Add(-1);
        Verificar($"{algoritmo}: acepta altas despues de ordenar",
            lista.Count == 51 && lista[50] == -1);

        int recorridos = 0;
        foreach (var _ in lista) recorridos++;
        Verificar($"{algoritmo}: foreach recorre todo", recorridos == 51);
    }

    /// <summary>
    /// Al ser generico, el ordenamiento sirve para datos que no son numeros:
    /// textos y resultados de niveles del juego.
    /// </summary>
    private void ProbarDatosDelJuego()
    {
        var niveles = new SimpleLinkedList<string>();
        niveles.AddRange(new[] { "Nivel3", "Nivel1", "Tutorial", "Nivel2" });
        CollectionSort.SortList(niveles);
        Verificar("Textos en orden alfabetico",
            Iguales(niveles.ToArray(), "Nivel1", "Nivel2", "Nivel3", "Tutorial"));

        var resultados = new SimpleArrayList<ResultadoDeNivel>();
        resultados.AddRange(new[]
        {
            new ResultadoDeNivel("Nivel1", 2, 95f),
            new ResultadoDeNivel("Nivel2", 3, 140f),
            new ResultadoDeNivel("Nivel3", 3, 62f),
            new ResultadoDeNivel("Nivel4", 1, 210f),
        });

        // IComparable: el orden natural del resultado es por tiempo
        CollectionSort.SortList(resultados);
        salida.AppendLine($"  Ranking por tiempo:    {Mostrar(resultados.ToArray())}");
        Verificar("IComparable: el mas rapido primero",
            resultados[0].Nivel == "Nivel3" && resultados[3].Nivel == "Nivel4");

        // Comparison: mas estrellas primero y, a igual estrellas, el mas rapido
        resultados.Sort(PorEstrellasYTiempo, SentidoDeOrden.Descendente);
        salida.AppendLine($"  Ranking por estrellas: {Mostrar(resultados.ToArray())}");
        Verificar("Comparison: mas estrellas primero, desempata por tiempo",
            resultados[0].Nivel == "Nivel3" && resultados[1].Nivel == "Nivel2"
            && resultados[2].Nivel == "Nivel1" && resultados[3].Nivel == "Nivel4");

        string[] conNulos = { "b", null, "a" };
        CollectionSort.SortArray(conNulos);
        Verificar("Un null queda primero y no rompe", Iguales(conNulos, null, "a", "b"));
    }

    private void ProbarCasosBorde()
    {
        foreach (var algoritmo in Algoritmos)
        {
            int[] vacio = new int[0];
            CollectionSort.SortArray(vacio, SentidoDeOrden.Ascendente, algoritmo);
            Verificar($"{algoritmo}: arreglo vacio no falla", vacio.Length == 0);

            int[] uno = { 42 };
            CollectionSort.SortArray(uno, SentidoDeOrden.Ascendente, algoritmo);
            Verificar($"{algoritmo}: un elemento queda igual", uno[0] == 42);

            int[] ordenado = { 1, 2, 3, 4, 5 };
            CollectionSort.SortArray(ordenado, SentidoDeOrden.Ascendente, algoritmo);
            Verificar($"{algoritmo}: ya ordenado queda igual", Iguales(ordenado, 1, 2, 3, 4, 5));

            int[] invertido = { 5, 4, 3, 2, 1 };
            CollectionSort.SortArray(invertido, SentidoDeOrden.Ascendente, algoritmo);
            Verificar($"{algoritmo}: invertido se da vuelta", Iguales(invertido, 1, 2, 3, 4, 5));
        }

        var listaVacia = new SimpleLinkedList<int>();
        CollectionSort.SortList(listaVacia);
        Verificar("Lista vacia no falla", listaVacia.Count == 0);

        // Una SimpleArrayList con capacidad de sobra: las celdas libres no se ordenan
        var conHuecos = new SimpleArrayList<int>(16);
        conHuecos.AddRange(new[] { 3, 1, 2 });
        CollectionSort.SortList(conHuecos);
        Verificar("SimpleArrayList ordena solo lo ocupado",
            conHuecos.Count == 3 && Iguales(conHuecos.ToArray(), 1, 2, 3));

        Verificar("SortArray de null tira excepcion",
            Tira<ArgumentNullException>(() => CollectionSort.SortArray<int>(null)));

        Verificar("SortList de null tira excepcion",
            Tira<ArgumentNullException>(() => CollectionSort.SortList<int>(null)));

        Verificar("Comparison null tira excepcion",
            Tira<ArgumentNullException>(() => CollectionSort.SortArray(new[] { 2, 1 }, (Comparison<int>)null)));
    }

    /// <summary>
    /// Ordena el mismo arreglo desordenado con los tres algoritmos y cuenta
    /// cuantas comparaciones hace cada uno, ademas del tiempo.
    /// </summary>
    private void CompararAlgoritmos()
    {
        int n = Mathf.Max(100, elementosDeLaMedicion);
        int[] original = Desordenado(n, 7);

        salida.AppendLine($"Con {n} elementos desordenados:");
        salida.AppendLine();

        foreach (var algoritmo in Algoritmos)
        {
            int[] copia = (int[])original.Clone();
            long comparaciones = 0;

            long ms = Medir(() => CollectionSort.SortArray(copia, (a, b) =>
            {
                comparaciones++;
                return a.CompareTo(b);
            }, SentidoDeOrden.Ascendente, algoritmo));

            Verificar($"{algoritmo}: ordena los {n}", EstaOrdenado(copia, false));
            salida.AppendLine($"    {algoritmo,-9} {comparaciones,10} comparaciones {ms,6} ms");
        }

        salida.AppendLine();
        salida.AppendLine("  Bubble y Selection hacen las mismas comparaciones, n(n-1) / 2,");
        salida.AppendLine("  pero Bubble intercambia en cada par desordenado y Selection hace a lo");
        salida.AppendLine("  sumo un swap por vuelta. Quick divide el problema y baja a n log n.");
    }

    // === COMPARACIONES DE EJEMPLO ===

    private static int ParesPrimero(int a, int b)
    {
        bool aPar = a % 2 == 0;
        bool bPar = b % 2 == 0;

        if (aPar != bPar) return aPar ? -1 : 1;
        return a.CompareTo(b);
    }

    /// <summary>
    /// Mas estrellas es "mayor". A igual estrellas, el que tardo menos es
    /// "mayor", asi en descendente queda primero el mejor resultado.
    /// </summary>
    private static int PorEstrellasYTiempo(ResultadoDeNivel a, ResultadoDeNivel b)
    {
        if (a.Estrellas != b.Estrellas) return a.Estrellas.CompareTo(b.Estrellas);
        return b.Tiempo.CompareTo(a.Tiempo);
    }

    // === AUXILIARES ===

    /// <summary>Arreglo de n numeros mezclados, siempre igual para la misma semilla.</summary>
    private static int[] Desordenado(int n, int semilla)
    {
        var azar = new System.Random(semilla);
        int[] col = new int[n];

        for (int i = 0; i < n; i++)
            col[i] = azar.Next(0, n);

        return col;
    }

    private static bool EstaOrdenado(int[] col, bool descendente)
    {
        for (int i = 0; i < col.Length - 1; i++)
        {
            int cmp = col[i].CompareTo(col[i + 1]);
            if (descendente ? cmp < 0 : cmp > 0) return false;
        }

        return true;
    }

    private static bool Iguales<T>(T[] col, params T[] esperado)
    {
        if (col.Length != esperado.Length) return false;

        for (int i = 0; i < col.Length; i++)
            if (!Equals(col[i], esperado[i])) return false;

        return true;
    }

    private static string Mostrar<T>(T[] col) => "{ " + string.Join(", ", col) + " }";

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
