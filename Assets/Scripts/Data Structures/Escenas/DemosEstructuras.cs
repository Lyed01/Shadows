using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

/// <summary>Un boton del panel: lo que dice y lo que hace con el texto escrito.</summary>
public class AccionDemo
{
    public readonly string Nombre;
    public readonly Func<string, string> Ejecutar;

    public AccionDemo(string nombre, Func<string, string> ejecutar)
    {
        Nombre = nombre;
        Ejecutar = ejecutar;
    }
}

/// <summary>
/// Una estructura viva para jugar con ella desde el panel de la escena. Cada
/// accion devuelve el mensaje que se muestra; si la estructura tira una
/// excepcion (leer de una pila vacia, por ejemplo) el panel la muestra tal cual.
/// </summary>
public abstract class DemoEstructura
{
    public bool Enlazada;

    /// <summary>Si tiene version estatica y dinamica entre las que alternar.</summary>
    public virtual bool TieneDosImplementaciones => true;

    public abstract string Ayuda { get; }
    public abstract string Estado();
    public abstract IEnumerable<AccionDemo> Acciones { get; }

    /// <summary>Crea la estructura vacia con la implementacion elegida.</summary>
    public abstract void Crear();

    protected static string Formato<T>(IEnumerable<T> items) => "[" + string.Join(", ", items) + "]";

    // Los tipos genericos se llaman "AVL`2": se corta en el acento grave
    protected static string Implementacion(object estructura) => estructura.GetType().Name.Split('`')[0];

    public static DemoEstructura Para(TemaEstructura tema)
    {
        DemoEstructura demo = tema switch
        {
            TemaEstructura.List => new DemoLista(),
            TemaEstructura.Stack => new DemoPila(),
            TemaEstructura.Queue => new DemoCola(),
            TemaEstructura.PriorityQueue => new DemoColaDePrioridad(),
            TemaEstructura.Set => new DemoConjunto(),
            TemaEstructura.Dictionary => new DemoDiccionario(),
            TemaEstructura.Sorting => new DemoOrdenamiento(),
            TemaEstructura.BST => new DemoArbol(false),
            _ => new DemoArbol(true)
        };

        demo.Crear();
        return demo;
    }
}

public class DemoLista : DemoEstructura
{
    private ISimpleList<string> lista;

    public override string Ayuda => "Escribi un texto y toca un boton. Insertar lo pone al principio.";

    public override void Crear() =>
        lista = Enlazada ? new SimpleLinkedList<string>() : new SimpleArrayList<string>();

    public override string Estado() =>
        $"{Implementacion(lista)}\nCount: {lista.Count}\n\n{Formato(lista)}";

    public override IEnumerable<AccionDemo> Acciones => new[]
    {
        new AccionDemo("Add", t => { lista.Add(t); return $"Add('{t}')"; }),
        new AccionDemo("Insert(0)", t => { lista.Insert(0, t); return $"Insert(0, '{t}')"; }),
        new AccionDemo("Remove", t => $"Remove('{t}') = {lista.Remove(t)}"),
        new AccionDemo("RemoveFirst", t => $"RemoveFirst() = '{lista.RemoveFirst()}'"),
        new AccionDemo("IndexOf", t => $"IndexOf('{t}') = {lista.IndexOf(t)}"),
        new AccionDemo("Contains", t => $"Contains('{t}') = {lista.Contains(t)}"),
        new AccionDemo("Clear", t => { lista.Clear(); return "Clear()"; }),
    };
}

public class DemoPila : DemoEstructura
{
    private ISimpleStack<string> pila;

    public override string Ayuda => "LIFO: el ultimo que entra es el primero que sale. Pop o Peek sin elementos tira excepcion.";

    public override void Crear() =>
        pila = Enlazada ? new SimpleLinkedStack<string>() : new SimpleArrayStack<string>();

    public override string Estado() =>
        $"{Implementacion(pila)}\nCount: {pila.Count}\n\nCima -> fondo:\n{Formato(pila)}";

    public override IEnumerable<AccionDemo> Acciones => new[]
    {
        new AccionDemo("Push", t => { pila.Push(t); return $"Push('{t}')"; }),
        new AccionDemo("Pop", t => $"Pop() = '{pila.Pop()}'"),
        new AccionDemo("Peek", t => $"Peek() = '{pila.Peek()}'"),
        new AccionDemo("Clear", t => { pila.Clear(); return "Clear()"; }),
    };
}

public class DemoCola : DemoEstructura
{
    private ISimpleQueue<string> cola;

    public override string Ayuda => "FIFO: el primero que entra es el primero que sale. Dequeue o Peek sin elementos tira excepcion.";

    public override void Crear() =>
        cola = Enlazada ? new SimpleLinkedQueue<string>() : new SimpleArrayQueue<string>();

    public override string Estado() =>
        $"{Implementacion(cola)}\nCount: {cola.Count}\n\nFrente -> final:\n{Formato(cola)}";

    public override IEnumerable<AccionDemo> Acciones => new[]
    {
        new AccionDemo("Enqueue", t => { cola.Enqueue(t); return $"Enqueue('{t}')"; }),
        new AccionDemo("Dequeue", t => $"Dequeue() = '{cola.Dequeue()}'"),
        new AccionDemo("Peek", t => $"Peek() = '{cola.Peek()}'"),
        new AccionDemo("Clear", t => { cola.Clear(); return "Clear()"; }),
    };
}

public class DemoColaDePrioridad : DemoEstructura
{
    private ISimplePriorityQueue<string> cola;

    public override string Ayuda => "Escribi 'nombre prioridad' (ej: espada 2). El numero MAS CHICO sale primero.";

    public override void Crear() =>
        cola = Enlazada ? new SimpleLinkedPriorityQueue<string>() : new SimpleArrayPriorityQueue<string>();

    public override string Estado()
    {
        string proximo = cola.IsEmpty ? "-" : $"'{cola.Peek()}' (prioridad {cola.GetHighestPriority()})";
        return $"{Implementacion(cola)}\nCount: {cola.Count}\nSale primero: {proximo}\n\nElementos:\n{Formato(cola)}";
    }

    public override IEnumerable<AccionDemo> Acciones => new[]
    {
        new AccionDemo("Enqueue", t =>
        {
            string[] partes = t.Trim().Split(' ');

            if (partes.Length < 2 || !int.TryParse(partes[partes.Length - 1], out int prioridad))
                return "Formato: nombre prioridad (ej: espada 2)";

            string item = string.Join(" ", partes.Take(partes.Length - 1));
            cola.Enqueue(item, prioridad);
            return $"Enqueue('{item}', {prioridad})";
        }),
        new AccionDemo("Dequeue", t => $"Dequeue() = '{cola.Dequeue()}'"),
        new AccionDemo("Peek", t => $"Peek() = '{cola.Peek()}'"),
        new AccionDemo("Clear", t => { cola.Clear(); return "Clear()"; }),
    };
}

public class DemoConjunto : DemoEstructura
{
    private ISimpleSet<string> a;
    private ISimpleSet<string> b;
    private string ultimaOperacion = "-";

    public override string Ayuda => "Dos conjuntos, A y B. Agrega elementos a cada uno y combina: union, interseccion, diferencia.";

    public override void Crear()
    {
        a = Enlazada ? new SimpleLinkedSet<string>() : new SimpleArraySet<string>();
        b = Enlazada ? new SimpleLinkedSet<string>() : new SimpleArraySet<string>();
        ultimaOperacion = "-";
    }

    public override string Estado() =>
        $"{Implementacion(a)}\n\nA = {Formato(a)}\nB = {Formato(b)}\n\nUltima operacion:\n{ultimaOperacion}";

    private string Operar(string nombre, ISimpleSet<string> resultado)
    {
        ultimaOperacion = $"{nombre} = {Formato(resultado)}";
        return ultimaOperacion;
    }

    public override IEnumerable<AccionDemo> Acciones => new[]
    {
        new AccionDemo("Add a A", t => $"A.Add('{t}') = {a.Add(t)}"),
        new AccionDemo("Add a B", t => $"B.Add('{t}') = {b.Add(t)}"),
        new AccionDemo("Remove de A", t => $"A.Remove('{t}') = {a.Remove(t)}"),
        new AccionDemo("A U B", t => Operar("A UnionWith B", a.UnionWith(b))),
        new AccionDemo("A n B", t => Operar("A IntersectWith B", a.IntersectWith(b))),
        new AccionDemo("A - B", t => Operar("A DifferenceWith B", a.DifferenceWith(b))),
        new AccionDemo("Clear", t => { a.Clear(); b.Clear(); ultimaOperacion = "-"; return "Clear() de A y B"; }),
    };
}

public class DemoDiccionario : DemoEstructura
{
    private ISimpleDictionary<string, string> dic;

    public override string Ayuda => "Escribi 'clave=valor' para agregar, o solo la clave para buscar o sacar. Add con clave repetida tira excepcion.";

    public override void Crear() =>
        dic = Enlazada ? new SimpleLinkedDictionary<string, string>() : new SimpleArrayDictionary<string, string>();

    public override string Estado() =>
        $"{Implementacion(dic)}\nCount: {dic.Count}\n\n" +
        string.Join("\n", dic.Select(p => $"{p.Key} -> {p.Value}"));

    private static bool Partir(string t, out string clave, out string valor)
    {
        int i = t.IndexOf('=');
        clave = (i < 0 ? t : t.Substring(0, i)).Trim();
        valor = i < 0 ? "" : t.Substring(i + 1).Trim();
        return i >= 0;
    }

    public override IEnumerable<AccionDemo> Acciones => new[]
    {
        new AccionDemo("Add", t =>
        {
            if (!Partir(t, out string k, out string v)) return "Formato: clave=valor";
            dic.Add(k, v);
            return $"Add('{k}', '{v}')";
        }),
        new AccionDemo("TryAdd", t =>
        {
            if (!Partir(t, out string k, out string v)) return "Formato: clave=valor";
            return $"TryAdd('{k}', '{v}') = {dic.TryAdd(k, v)}";
        }),
        new AccionDemo("this[k] = v", t =>
        {
            if (!Partir(t, out string k, out string v)) return "Formato: clave=valor";
            dic[k] = v;
            return $"['{k}'] = '{v}'";
        }),
        new AccionDemo("TryGetValue", t =>
        {
            Partir(t, out string k, out _);
            return dic.TryGetValue(k, out string v) ? $"TryGetValue('{k}') = true, '{v}'" : $"TryGetValue('{k}') = false";
        }),
        new AccionDemo("Remove", t => { Partir(t, out string k, out _); return $"Remove('{k}') = {dic.Remove(k)}"; }),
        new AccionDemo("Clear", t => { dic.Clear(); return "Clear()"; }),
    };
}

public class DemoOrdenamiento : DemoEstructura
{
    private ISimpleList<int> lista;
    private readonly Random azar = new Random();

    public override bool TieneDosImplementaciones => false;

    public override string Ayuda => "Mezcla una lista de enteros y ordenala con cada algoritmo, ascendente o descendente.";

    public override void Crear() => Mezclar();

    private string Mezclar()
    {
        lista = new SimpleArrayList<int>();
        for (int i = 0; i < 15; i++) lista.Add(azar.Next(1, 100));
        return "Lista nueva de 15 numeros al azar";
    }

    public override string Estado() => $"{Implementacion(lista)}\nCount: {lista.Count}\n\n{Formato(lista)}";

    private string Ordenar(AlgoritmoDeOrden algoritmo, SentidoDeOrden sentido)
    {
        lista.Sort((x, y) => x.CompareTo(y), sentido, algoritmo);
        return $"Sort({algoritmo}, {sentido})";
    }

    public override IEnumerable<AccionDemo> Acciones => new[]
    {
        new AccionDemo("Mezclar", t => Mezclar()),
        new AccionDemo("Bubble ↑", t => Ordenar(AlgoritmoDeOrden.Bubble, SentidoDeOrden.Ascendente)),
        new AccionDemo("Selection ↑", t => Ordenar(AlgoritmoDeOrden.Selection, SentidoDeOrden.Ascendente)),
        new AccionDemo("Quick ↑", t => Ordenar(AlgoritmoDeOrden.Quick, SentidoDeOrden.Ascendente)),
        new AccionDemo("Quick ↓", t => Ordenar(AlgoritmoDeOrden.Quick, SentidoDeOrden.Descendente)),
    };
}

/// <summary>BST o AVL de enteros, dibujado de costado. Mismo panel para ver la diferencia.</summary>
public class DemoArbol : DemoEstructura
{
    private readonly bool esAvl;
    private BST<int, int> arbol;

    public DemoArbol(bool esAvl)
    {
        this.esAvl = esAvl;
    }

    public override bool TieneDosImplementaciones => false;

    public override string Ayuda => esAvl
        ? "Escribi un numero. Proba 'Cargar 1..15 en orden': el AVL rota y queda parejo."
        : "Escribi un numero. Proba 'Cargar 1..15 en orden': el BST degenera en una lista.";

    public override void Crear() =>
        arbol = esAvl
            ? new AVL<int, int>((clave, valor) => clave.CompareTo(valor))
            : new BST<int, int>((clave, valor) => clave.CompareTo(valor));

    public override string Estado()
    {
        var sb = new StringBuilder();
        sb.AppendLine($"{Implementacion(arbol)}   Count: {arbol.Count}   Altura: {arbol.Height}");

        if (arbol is AVL<int, int> avl)
            sb.AppendLine($"Balanceado: {avl.EstaBalanceado()}");

        sb.AppendLine();
        sb.AppendLine($"InOrder:   {Formato(arbol.InOrder())}");
        sb.AppendLine($"PreOrder:  {Formato(arbol.PreOrder())}");
        sb.AppendLine($"PostOrder: {Formato(arbol.PostOrder())}");
        sb.AppendLine();
        sb.AppendLine("Arbol (la raiz a la izquierda, hijo derecho arriba):");
        Dibujar(arbol.Raiz, 0, sb);

        return sb.ToString();
    }

    private static void Dibujar(TreeNode<int> nodo, int nivel, StringBuilder sb)
    {
        if (nodo == null) return;

        Dibujar(nodo.Right, nivel + 1, sb);
        sb.AppendLine(new string(' ', nivel * 4) + nodo.Value);
        Dibujar(nodo.Left, nivel + 1, sb);
    }

    private static bool Numero(string t, out int n) => int.TryParse(t.Trim(), out n);

    public override IEnumerable<AccionDemo> Acciones => new[]
    {
        new AccionDemo("Add", t => Numero(t, out int n) ? $"Add({n}) = {arbol.Add(n)}" : "Escribi un numero"),
        new AccionDemo("Remove", t => Numero(t, out int n) ? $"Remove({n}) = {arbol.Remove(n)}" : "Escribi un numero"),
        new AccionDemo("TryGet", t => Numero(t, out int n) ? $"TryGet({n}) = {arbol.TryGet(n, out _)}" : "Escribi un numero"),
        new AccionDemo("Min / Max", t => arbol.IsEmpty ? "Arbol vacio" : $"Min = {arbol.Min()}, Max = {arbol.Max()}"),
        new AccionDemo("Cargar 1..15 en orden", t =>
        {
            Crear();
            for (int i = 1; i <= 15; i++) arbol.Add(i);
            return "Arbol nuevo con 1..15 insertados en orden";
        }),
        new AccionDemo("Clear", t => { arbol.Clear(); return "Clear()"; }),
    };
}
