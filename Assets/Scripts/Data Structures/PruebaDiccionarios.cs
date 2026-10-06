using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using UnityEngine;

/// <summary>
/// Ejercita las dos implementaciones del TDA Dictionary y deja el resultado en
/// la consola. Sirve para la demostracion del TP: muestra que ambas cumplen el
/// mismo contrato, que las tres formas de agregar se comportan distinto ante
/// una clave repetida, y mide como se comportan al crecer.
///
/// Se ejecuta solo al arrancar la escena, o a pedido desde el menu contextual
/// del componente.
/// </summary>
public class PruebaDiccionarios : MonoBehaviour
{
    [Header("Medicion de tiempos")]
    [Tooltip("Cantidad de pares con la que se comparan las dos implementaciones.")]
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

        Titulo("SimpleArrayDictionary");
        ProbarContrato(new SimpleArrayDictionary<string, int>());
        ProbarCasosBorde(new SimpleArrayDictionary<string, int>());
        ProbarRemoveEnCadena(new SimpleArrayDictionary<string, int>());

        Titulo("SimpleLinkedDictionary");
        ProbarContrato(new SimpleLinkedDictionary<string, int>());
        ProbarCasosBorde(new SimpleLinkedDictionary<string, int>());
        ProbarRemoveEnCadena(new SimpleLinkedDictionary<string, int>());

        Titulo("El caso del juego: tipo de habilidad -> habilidad");
        ProbarComoEnElJuego();

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

    private void ProbarContrato(ISimpleDictionary<string, int> dic)
    {
        Verificar("Arranca vacio", dic.Count == 0 && dic.IsEmpty);

        dic.Add("a", 1);
        dic.Add("b", 2);
        dic.Add("c", 3);
        Verificar("Add suma pares", dic.Count == 3 && !dic.IsEmpty);
        Verificar("El indexador lee por clave", dic["b"] == 2);
        Verificar("ContainsKey encuentra", dic.ContainsKey("c"));
        Verificar("ContainsKey niega lo ausente", !dic.ContainsKey("z"));

        // Las tres formas de agregar difieren solo ante una clave repetida
        Verificar("Add con clave repetida tira ArgumentException",
            Tira<ArgumentException>(() => dic.Add("a", 99)) && dic["a"] == 1);

        Verificar("TryAdd con clave repetida devuelve false y no toca el valor",
            !dic.TryAdd("a", 99) && dic["a"] == 1 && dic.Count == 3);

        Verificar("TryAdd con clave nueva devuelve true", dic.TryAdd("d", 4) && dic["d"] == 4);

        dic["a"] = 10;
        Verificar("El indexador pisa el valor de una clave existente", dic["a"] == 10 && dic.Count == 4);

        dic["e"] = 5;
        Verificar("El indexador agrega si la clave no existia", dic["e"] == 5 && dic.Count == 5);

        Verificar("Los valores se pueden repetir", dic.TryAdd("f", 5) && dic["f"] == dic["e"]);

        Verificar("El indexador tira KeyNotFoundException si no esta",
            Tira<KeyNotFoundException>(() => { int _ = dic["no existe"]; }));

        Verificar("TryGetValue devuelve true y el valor", dic.TryGetValue("b", out int valor) && valor == 2);
        Verificar("TryGetValue devuelve false si no esta", !dic.TryGetValue("no existe", out int ausente) && ausente == 0);

        Verificar("Remove devuelve true", dic.Remove("b"));
        Verificar("Remove saca la clave", !dic.ContainsKey("b") && dic.Count == 5);
        Verificar("Remove devuelve false si no esta", !dic.Remove("b"));

        // Sacar del medio no debe perder a los demas: la estatica tapa el hueco
        // con el ultimo, y hay que comprobar que ese ultimo sigue estando.
        Verificar("Los otros sobreviven a un Remove",
            dic["a"] == 10 && dic["c"] == 3 && dic["d"] == 4 && dic["e"] == 5 && dic["f"] == 5);

        string[] claves = dic.Keys();
        int[] valores = dic.Values();
        Verificar("Keys y Values devuelven todos", claves.Length == 5 && valores.Length == 5);

        bool alineados = true;
        for (int i = 0; i < claves.Length; i++)
            if (dic[claves[i]] != valores[i]) alineados = false;
        Verificar("Values sigue el mismo orden que Keys", alineados);

        int recorridos = 0;
        foreach (var par in dic)
            if (dic[par.Key] == par.Value) recorridos++;
        Verificar("foreach recorre todos los pares", recorridos == dic.Count);

        dic.Clear();
        Verificar("Clear vacia", dic.Count == 0 && dic.IsEmpty && !dic.ContainsKey("a"));

        // Volver a agregar valida que los punteros internos quedaron bien
        dic.Add("despues", 1);
        Verificar("Vuelve a aceptar altas despues de vaciarse", dic.Count == 1 && dic["despues"] == 1);
    }

    private void ProbarCasosBorde(ISimpleDictionary<string, int> dic)
    {
        Verificar("Vacio: ContainsKey devuelve false", !dic.ContainsKey("nada"));
        Verificar("Vacio: Remove devuelve false", !dic.Remove("nada"));
        Verificar("Vacio: TryGetValue devuelve false", !dic.TryGetValue("nada", out _));
        Verificar("Vacio: Keys y Values devuelven vacio", dic.Keys().Length == 0 && dic.Values().Length == 0);

        int recorridos = 0;
        foreach (var _ in dic) recorridos++;
        Verificar("Vacio: foreach no itera", recorridos == 0);

        Verificar("Add de clave null tira ArgumentNullException",
            Tira<ArgumentNullException>(() => dic.Add(null, 1)));
        Verificar("TryAdd de clave null tira ArgumentNullException",
            Tira<ArgumentNullException>(() => dic.TryAdd(null, 1)));
        Verificar("Remove de clave null tira ArgumentNullException",
            Tira<ArgumentNullException>(() => dic.Remove(null)));
        Verificar("ContainsKey de clave null tira ArgumentNullException",
            Tira<ArgumentNullException>(() => dic.ContainsKey(null)));
        Verificar("TryGetValue de clave null tira ArgumentNullException",
            Tira<ArgumentNullException>(() => dic.TryGetValue(null, out _)));
        Verificar("El indexador con clave null tira ArgumentNullException al leer",
            Tira<ArgumentNullException>(() => { int _ = dic[null]; }));
        Verificar("El indexador con clave null tira ArgumentNullException al escribir",
            Tira<ArgumentNullException>(() => dic[null] = 1));

        dic.Add("unico", 1);
        Verificar("Sacar el unico deja el diccionario vacio", dic.Remove("unico") && dic.IsEmpty);
    }

    /// <summary>
    /// Saca primero, medio y ultimo y despues vuelve a agregar. En la dinamica
    /// es donde se rompe la cola si Remove no la actualiza.
    /// </summary>
    private void ProbarRemoveEnCadena(ISimpleDictionary<string, int> dic)
    {
        dic.Clear();
        for (int i = 1; i <= 5; i++) dic.Add("k" + i, i);

        Verificar("Remove del primero", dic.Remove("k1") && dic.Count == 4 && !dic.ContainsKey("k1"));
        Verificar("Remove del ultimo", dic.Remove("k5") && dic.Count == 3 && !dic.ContainsKey("k5"));
        Verificar("Remove del medio", dic.Remove("k3") && dic.Count == 2);

        dic.Add("k6", 6);
        Verificar("Agregar despues de sacar el ultimo no pierde nada",
            dic.Count == 3 && dic["k2"] == 2 && dic["k4"] == 4 && dic["k6"] == 6);

        dic.Remove("k2"); dic.Remove("k4"); dic.Remove("k6");
        Verificar("Se puede vaciar sacando de a uno", dic.IsEmpty && dic.Keys().Length == 0);

        dic.Add("otra", 7);
        Verificar("Y volver a usar", dic["otra"] == 7 && dic.Count == 1);
    }

    /// <summary>
    /// Reproduce el uso real del juego: el controlador de habilidades guarda un
    /// valor por tipo y se queda con el primero si un tipo se repite.
    /// </summary>
    private void ProbarComoEnElJuego()
    {
        var habilidades = new SimpleArrayDictionary<AbilityType, string>();
        Array tipos = Enum.GetValues(typeof(AbilityType));

        foreach (AbilityType tipo in tipos)
            habilidades.TryAdd(tipo, "habilidad " + tipo);

        Verificar("Una entrada por cada AbilityType", habilidades.Count == tipos.Length);

        AbilityType primero = (AbilityType)tipos.GetValue(0);
        Verificar("Registrar un tipo repetido deja la primera",
            !habilidades.TryAdd(primero, "intrusa") && habilidades[primero] == "habilidad " + primero);

        Verificar("Un tipo sin habilidad no rompe: TryGetValue devuelve false",
            !habilidades.TryGetValue((AbilityType)999, out _));
    }

    /// <summary>
    /// Mide el costo de agregar y de buscar por clave, que son las operaciones
    /// que definen al Dictionary.
    /// </summary>
    private void CompararTiempos()
    {
        int n = Mathf.Max(100, elementosDeLaMedicion);

        var arreglo = new SimpleArrayDictionary<int, int>();
        var enlazado = new SimpleLinkedDictionary<int, int>();

        salida.AppendLine($"Con {n} pares:");
        salida.AppendLine();

        long altaArreglo = Medir(() => { for (int i = 0; i < n; i++) arreglo.Add(i, i); });
        long altaEnlazado = Medir(() => { for (int i = 0; i < n; i++) enlazado.Add(i, i); });

        salida.AppendLine("  Dar de alta n pares          ambas O(n) por alta: Add chequea la clave");
        salida.AppendLine($"    SimpleArrayDictionary    {altaArreglo,8} ms");
        salida.AppendLine($"    SimpleLinkedDictionary   {altaEnlazado,8} ms");
        salida.AppendLine();

        long buscarArreglo = Medir(() => { for (int i = 0; i < n; i++) arreglo.TryGetValue(i, out _); });
        long buscarEnlazado = Medir(() => { for (int i = 0; i < n; i++) enlazado.TryGetValue(i, out _); });

        salida.AppendLine("  Buscar por clave             ambas O(n)");
        salida.AppendLine($"    SimpleArrayDictionary    {buscarArreglo,8} ms");
        salida.AppendLine($"    SimpleLinkedDictionary   {buscarEnlazado,8} ms");
        salida.AppendLine();
        salida.AppendLine("  Las dos tienen el mismo costo teorico: hay que recorrer igual, y la");
        salida.AppendLine("  diferencia medida cambia segun la maquina, asi que el tiempo no decide.");
        salida.AppendLine("  Decide la memoria y el uso: las habilidades del jugador, el catalogo de");
        salida.AppendLine("  bloques y el pool son chicos, se cargan una vez y casi no se modifican,");
        salida.AppendLine("  asi que usan la estatica: un solo arreglo, sin un nodo por par.");
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
