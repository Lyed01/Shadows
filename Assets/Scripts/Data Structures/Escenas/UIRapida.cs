using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Lo minimo para armar UI por codigo en los paneles de depuracion de las
/// estructuras, sin prefabs ni escenas con Canvas armado a mano. Usa uGUI con
/// la fuente del sistema, asi no depende de assets de TextMeshPro.
/// </summary>
public static class UIRapida
{
    public static Font Fuente(int tamano) =>
        Font.CreateDynamicFontFromOSFont(new[] { "Consolas", "Courier New" }, tamano);

    /// <summary>Crea un Canvas overlay de 1920x1080 de referencia, con EventSystem si falta.</summary>
    public static RectTransform CrearCanvas(Transform padre, int orden, bool conRaycaster)
    {
        if (conRaycaster && Object.FindFirstObjectByType<EventSystem>() == null)
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));

        var go = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler));
        go.transform.SetParent(padre, false);

        var canvas = go.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = orden;

        var scaler = go.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        if (conRaycaster)
            go.AddComponent<GraphicRaycaster>();

        return (RectTransform)go.transform;
    }

    public static RectTransform Rect(string nombre, RectTransform padre, Color color)
    {
        var go = new GameObject(nombre, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(padre, false);
        go.GetComponent<Image>().color = color;
        return (RectTransform)go.transform;
    }

    public static void Rellenar(RectTransform rt, float xMin, float yMin, float xMax, float yMax, float margen)
    {
        rt.anchorMin = new Vector2(xMin, yMin);
        rt.anchorMax = new Vector2(xMax, yMax);
        rt.offsetMin = new Vector2(margen, margen);
        rt.offsetMax = new Vector2(-margen, -margen);
    }

    public static Text Etiqueta(RectTransform padre, Font fuente, string texto, int tamano)
    {
        var go = new GameObject("Texto", typeof(RectTransform), typeof(Text));
        go.transform.SetParent(padre, false);

        Text t = go.GetComponent<Text>();
        t.font = fuente;
        t.fontSize = tamano;
        t.color = Color.white;
        t.text = texto;
        t.horizontalOverflow = HorizontalWrapMode.Wrap;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        t.raycastTarget = false;
        return t;
    }

    /// <summary>Texto que ocupa todo su padre.</summary>
    public static Text EtiquetaLlena(RectTransform padre, Font fuente, string texto, int tamano, float margen)
    {
        Text t = Etiqueta(padre, fuente, texto, tamano);
        Rellenar(t.rectTransform, 0, 0, 1, 1, margen);
        return t;
    }

    public static Button Boton(RectTransform padre, Font fuente, string texto, Color color, int tamano,
        UnityEngine.Events.UnityAction alClick, out Text etiqueta)
    {
        RectTransform rt = Rect("Boton " + texto, padre, color);
        Button boton = rt.gameObject.AddComponent<Button>();
        boton.onClick.AddListener(alClick);

        etiqueta = EtiquetaLlena(rt, fuente, texto, tamano, 8);
        etiqueta.alignment = TextAnchor.MiddleLeft;
        return boton;
    }

    /// <summary>Un texto largo con scroll vertical (rueda del mouse incluida).</summary>
    public static Text TextoEnScroll(RectTransform panel, Font fuente, int tamano)
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
