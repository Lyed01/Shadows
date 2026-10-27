using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>Un tema por TP de la materia. El orden es el del numero de TP menos 1.</summary>
public enum TemaEstructura
{
    List,
    Stack,
    Queue,
    PriorityQueue,
    Set,
    Dictionary,
    Sorting,
    BST,
    AVL
}

/// <summary>Nombres de escena y titulos del modo de pruebas, uno por TemaEstructura.</summary>
public static class EscenasEstructuras
{
    public const string Menu = "MenuEstructuras";

    public static readonly string[] Escenas =
    {
        "TP01_List", "TP02_Stack", "TP03_Queue", "TP04_PriorityQueue", "TP05_Set",
        "TP06_Dictionary", "TP07_Sorting", "TP08_BST", "TP09_AVL"
    };

    public static readonly string[] Titulos =
    {
        "TP01 · List", "TP02 · Stack", "TP03 · Queue", "TP04 · Priority Queue", "TP05 · Set",
        "TP06 · Dictionary", "TP07 · Sorting", "TP08 · BST", "TP09 · AVL"
    };
}

/// <summary>
/// Panel de una escena de prueba de estructuras. Arma toda la UI por codigo, asi
/// que la escena solo necesita un GameObject con este componente, el componente
/// Prueba* del tema y el tema elegido en el Inspector.
///
/// Izquierda: la estructura viva (texto + botones para operar). Derecha: el
/// estado actual arriba y, abajo, el resultado de las pruebas automaticas.
/// </summary>
public class PanelEstructuras : MonoBehaviour
{
    public TemaEstructura tema;

    private static readonly Color Fondo = new Color(0.09f, 0.1f, 0.13f);
    private static readonly Color Panel = new Color(0.14f, 0.16f, 0.2f);
    private static readonly Color Boton = new Color(0.25f, 0.35f, 0.6f);
    private static readonly Color BotonSecundario = new Color(0.3f, 0.3f, 0.36f);

    private DemoEstructura demo;
    private Text estado;
    private Text resultadoPruebas;
    private Text mensaje;
    private InputField entrada;
    private Font fuente;

    private Action correrPruebas;
    private Func<string> textoPruebas;

    private void Awake()
    {
        fuente = Font.CreateDynamicFontFromOSFont(new[] { "Consolas", "Courier New" }, 18);

        ConfigurarPruebas();
        demo = DemoEstructura.Para(tema);
        ConstruirUI();
    }

    private void Start()
    {
        CorrerPruebas();
        Refrescar("Listo. Probá los botones o corré las pruebas.");
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
            Volver();
    }

    // Pruebas automaticas

    private void ConfigurarPruebas()
    {
        switch (tema)
        {
            case TemaEstructura.List: Enlazar(GetComponent<PruebaListas>(), p => p.CorrerTodo(), p => p.UltimaSalida); break;
            case TemaEstructura.Stack: Enlazar(GetComponent<PruebaPilas>(), p => p.CorrerTodo(), p => p.UltimaSalida); break;
            case TemaEstructura.Queue: Enlazar(GetComponent<PruebaColas>(), p => p.CorrerTodo(), p => p.UltimaSalida); break;
            case TemaEstructura.PriorityQueue: Enlazar(GetComponent<PruebaColasDePrioridad>(), p => p.CorrerTodo(), p => p.UltimaSalida); break;
            case TemaEstructura.Set: Enlazar(GetComponent<PruebaConjuntos>(), p => p.CorrerTodo(), p => p.UltimaSalida); break;
            case TemaEstructura.Dictionary: Enlazar(GetComponent<PruebaDiccionarios>(), p => p.CorrerTodo(), p => p.UltimaSalida); break;
            case TemaEstructura.Sorting: Enlazar(GetComponent<PruebaSorting>(), p => p.CorrerTodo(), p => p.UltimaSalida); break;
            case TemaEstructura.BST: Enlazar(GetComponent<PruebaBST>(), p => p.CorrerTodo(), p => p.UltimaSalida); break;
            default: Enlazar(GetComponent<PruebaAVL>(), p => p.CorrerTodo(), p => p.UltimaSalida); break;
        }
    }

    private void Enlazar<T>(T prueba, Action<T> correr, Func<T, string> texto) where T : MonoBehaviour
    {
        if (prueba == null)
        {
            Debug.LogError($"Falta el componente {typeof(T).Name} en {name}.");
            return;
        }

        prueba.DesactivarAutoarranque();
        correrPruebas = () => correr(prueba);
        textoPruebas = () => texto(prueba);
    }

    private void CorrerPruebas()
    {
        if (correrPruebas == null) return;

        correrPruebas();
        resultadoPruebas.text = textoPruebas();
    }

    // Acciones

    private void Ejecutar(AccionDemo accion)
    {
        string texto;

        try
        {
            texto = accion.Ejecutar(entrada.text);
        }
        catch (Exception e)
        {
            texto = $"{e.GetType().Name}: {e.Message}";
        }

        Refrescar(texto);
    }

    private void Alternar()
    {
        demo.Enlazada = !demo.Enlazada;
        demo.Crear();
        Refrescar(demo.Enlazada ? "Ahora usa la implementación dinámica (nodos)." : "Ahora usa la implementación estática (arreglo).");
    }

    private void Reiniciar()
    {
        demo.Crear();
        Refrescar("Estructura vacía.");
    }

    private void Refrescar(string texto)
    {
        mensaje.text = texto;
        estado.text = demo.Estado();
    }

    private static void Volver() => SceneManager.LoadScene(EscenasEstructuras.Menu);

    // Construccion de la UI

    private void ConstruirUI()
    {
        if (FindFirstObjectByType<EventSystem>() == null)
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));

        var canvasGO = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasGO.transform.SetParent(transform, false);
        canvasGO.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;

        var scaler = canvasGO.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        RectTransform raiz = (RectTransform)canvasGO.transform;
        Rellenar(Crear("Fondo", raiz, Fondo), 0, 0, 1, 1, 0);

        ConstruirColumnaIzquierda(raiz);

        RectTransform arriba = Crear("PanelEstado", raiz, Panel);
        Rellenar(arriba, 0.36f, 0.5f, 0.99f, 0.98f, 8);
        estado = TextoEnScroll(arriba, 20);

        RectTransform abajo = Crear("PanelPruebas", raiz, Panel);
        Rellenar(abajo, 0.36f, 0.02f, 0.99f, 0.49f, 8);
        resultadoPruebas = TextoEnScroll(abajo, 16);
    }

    private void ConstruirColumnaIzquierda(RectTransform raiz)
    {
        RectTransform col = Crear("Columna", raiz, Panel);
        Rellenar(col, 0.01f, 0.02f, 0.35f, 0.98f, 8);

        var layout = col.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(16, 16, 16, 16);
        layout.spacing = 8;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        Etiqueta(col, EscenasEstructuras.Titulos[(int)tema], 34, 50, FontStyle.Bold);
        Etiqueta(col, demo.Ayuda, 17, 70, FontStyle.Normal);

        entrada = CrearInput(col);

        mensaje = Etiqueta(col, "", 17, 70, FontStyle.Italic);
        mensaje.color = new Color(0.6f, 0.9f, 0.6f);

        foreach (AccionDemo accion in demo.Acciones)
        {
            AccionDemo a = accion;
            CrearBoton(col, a.Nombre, Boton, () => Ejecutar(a));
        }

        if (demo.TieneDosImplementaciones)
            CrearBoton(col, "Cambiar estática / dinámica", BotonSecundario, Alternar);

        CrearBoton(col, "Vaciar y reiniciar", BotonSecundario, Reiniciar);
        CrearBoton(col, "Correr pruebas automáticas", BotonSecundario, () =>
        {
            CorrerPruebas();
            Refrescar("Pruebas corridas, mirá el panel de abajo.");
        });
        CrearBoton(col, "Volver al menú (Esc)", BotonSecundario, Volver);
    }

    private static RectTransform Crear(string nombre, RectTransform padre, Color color)
    {
        var go = new GameObject(nombre, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(padre, false);
        go.GetComponent<Image>().color = color;
        return (RectTransform)go.transform;
    }

    private static void Rellenar(RectTransform rt, float xMin, float yMin, float xMax, float yMax, float margen)
    {
        rt.anchorMin = new Vector2(xMin, yMin);
        rt.anchorMax = new Vector2(xMax, yMax);
        rt.offsetMin = new Vector2(margen, margen);
        rt.offsetMax = new Vector2(-margen, -margen);
    }

    private Text Etiqueta(RectTransform padre, string texto, int tamano, float alto, FontStyle estilo)
    {
        var go = new GameObject("Texto", typeof(RectTransform), typeof(Text), typeof(LayoutElement));
        go.transform.SetParent(padre, false);
        go.GetComponent<LayoutElement>().preferredHeight = alto;

        Text t = go.GetComponent<Text>();
        t.font = fuente;
        t.fontSize = tamano;
        t.fontStyle = estilo;
        t.color = Color.white;
        t.text = texto;
        t.horizontalOverflow = HorizontalWrapMode.Wrap;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        return t;
    }

    private void CrearBoton(RectTransform padre, string texto, Color color, Action alClick)
    {
        RectTransform rt = Crear("Boton " + texto, padre, color);
        rt.gameObject.AddComponent<LayoutElement>().preferredHeight = 38;
        rt.gameObject.AddComponent<Button>().onClick.AddListener(() => alClick());

        Text t = Etiqueta(rt, texto, 19, 38, FontStyle.Normal);
        Destroy(t.GetComponent<LayoutElement>());
        t.alignment = TextAnchor.MiddleCenter;
        t.rectTransform.anchorMin = Vector2.zero;
        t.rectTransform.anchorMax = Vector2.one;
        t.rectTransform.offsetMin = t.rectTransform.offsetMax = Vector2.zero;
        t.raycastTarget = false;
    }

    private InputField CrearInput(RectTransform padre)
    {
        RectTransform rt = Crear("Entrada", padre, new Color(0.05f, 0.06f, 0.08f));
        rt.gameObject.AddComponent<LayoutElement>().preferredHeight = 44;
        var campo = rt.gameObject.AddComponent<InputField>();

        Text texto = Etiqueta(rt, "", 22, 44, FontStyle.Normal);
        Text placeholder = Etiqueta(rt, "escribí acá...", 22, 44, FontStyle.Italic);
        placeholder.color = new Color(1f, 1f, 1f, 0.35f);

        foreach (Text t in new[] { texto, placeholder })
        {
            Destroy(t.GetComponent<LayoutElement>());
            t.alignment = TextAnchor.MiddleLeft;
            t.rectTransform.anchorMin = Vector2.zero;
            t.rectTransform.anchorMax = Vector2.one;
            t.rectTransform.offsetMin = new Vector2(10, 4);
            t.rectTransform.offsetMax = new Vector2(-10, -4);
        }

        campo.textComponent = texto;
        campo.placeholder = placeholder;
        return campo;
    }

    /// <summary>Un texto largo con barra de scroll vertical (rueda del mouse incluida).</summary>
    private Text TextoEnScroll(RectTransform panel, int tamano)
    {
        var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D));
        viewport.transform.SetParent(panel, false);
        Rellenar((RectTransform)viewport.transform, 0, 0, 1, 1, 10);

        var contenido = new GameObject("Contenido", typeof(RectTransform), typeof(Text), typeof(ContentSizeFitter));
        contenido.transform.SetParent(viewport.transform, false);

        var rt = (RectTransform)contenido.transform;
        rt.anchorMin = new Vector2(0, 1);
        rt.anchorMax = new Vector2(1, 1);
        rt.pivot = new Vector2(0.5f, 1);
        rt.offsetMin = rt.offsetMax = Vector2.zero;

        contenido.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        Text t = contenido.GetComponent<Text>();
        t.font = fuente;
        t.fontSize = tamano;
        t.color = Color.white;
        t.horizontalOverflow = HorizontalWrapMode.Wrap;

        var scroll = panel.gameObject.AddComponent<ScrollRect>();
        scroll.viewport = (RectTransform)viewport.transform;
        scroll.content = rt;
        scroll.horizontal = false;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 40;

        return t;
    }
}

internal static class PruebaExtensiones
{
    /// <summary>
    /// Las pruebas corren cuando el panel lo pide, no solas al arrancar: si lo
    /// hicieran las dos cosas, la consola recibiria el resultado duplicado.
    /// </summary>
    public static void DesactivarAutoarranque(this MonoBehaviour prueba)
    {
        var campo = prueba.GetType().GetField("correrAlIniciar");

        if (campo != null && campo.FieldType == typeof(bool))
            campo.SetValue(prueba, false);
    }
}
