using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Indice del modo de pruebas de estructuras: un boton por TP que carga su
/// escena. Arma la UI por codigo, asi que la escena solo necesita un
/// GameObject con este componente.
/// </summary>
public class MenuEstructuras : MonoBehaviour
{
    [Tooltip("Escena a la que vuelve el boton de salida (el menu principal del juego).")]
    public string escenaDeSalida = "MainMenu";

    private void Start()
    {
        Font fuente = Font.CreateDynamicFontFromOSFont(new[] { "Consolas", "Courier New" }, 18);

        if (FindFirstObjectByType<EventSystem>() == null)
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));

        var canvasGO = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasGO.transform.SetParent(transform, false);
        canvasGO.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;

        var scaler = canvasGO.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        var raiz = (RectTransform)canvasGO.transform;

        var fondo = Rect("Fondo", raiz, new Color(0.09f, 0.1f, 0.13f));
        fondo.anchorMin = Vector2.zero;
        fondo.anchorMax = Vector2.one;
        fondo.offsetMin = fondo.offsetMax = Vector2.zero;

        Etiqueta(raiz, fuente, "Estructuras de Datos", 56, new Vector2(0.5f, 0.9f));
        Etiqueta(raiz, fuente, "Una escena por TP. Cada una tiene la estructura para jugar y las pruebas automáticas.", 22, new Vector2(0.5f, 0.82f));

        for (int i = 0; i < EscenasEstructuras.Escenas.Length; i++)
        {
            string escena = EscenasEstructuras.Escenas[i];
            float x = 0.22f + (i % 3) * 0.28f;
            float y = 0.66f - (i / 3) * 0.17f;

            Boton(raiz, fuente, EscenasEstructuras.Titulos[i], new Vector2(x, y), new Vector2(520, 110),
                new Color(0.25f, 0.35f, 0.6f), () => SceneManager.LoadScene(escena));
        }

        Boton(raiz, fuente, "Volver al juego", new Vector2(0.5f, 0.1f), new Vector2(520, 80),
            new Color(0.3f, 0.3f, 0.36f), () => SceneManager.LoadScene(escenaDeSalida));
    }

    private static RectTransform Rect(string nombre, RectTransform padre, Color color)
    {
        var go = new GameObject(nombre, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(padre, false);
        go.GetComponent<Image>().color = color;
        return (RectTransform)go.transform;
    }

    private static Text Etiqueta(RectTransform padre, Font fuente, string texto, int tamano, Vector2 ancla)
    {
        var go = new GameObject("Texto", typeof(RectTransform), typeof(Text));
        go.transform.SetParent(padre, false);

        var rt = (RectTransform)go.transform;
        rt.anchorMin = rt.anchorMax = ancla;
        rt.sizeDelta = new Vector2(1600, tamano * 1.6f);

        Text t = go.GetComponent<Text>();
        t.font = fuente;
        t.fontSize = tamano;
        t.color = Color.white;
        t.alignment = TextAnchor.MiddleCenter;
        t.text = texto;
        return t;
    }

    private static void Boton(RectTransform padre, Font fuente, string texto, Vector2 ancla, Vector2 tamano,
        Color color, UnityEngine.Events.UnityAction alClick)
    {
        RectTransform rt = Rect("Boton " + texto, padre, color);
        rt.anchorMin = rt.anchorMax = ancla;
        rt.sizeDelta = tamano;
        rt.gameObject.AddComponent<Button>().onClick.AddListener(alClick);

        Text t = Etiqueta(rt, fuente, texto, 30, new Vector2(0.5f, 0.5f));
        t.rectTransform.anchorMin = Vector2.zero;
        t.rectTransform.anchorMax = Vector2.one;
        t.rectTransform.offsetMin = t.rectTransform.offsetMax = Vector2.zero;
        t.raycastTarget = false;
    }
}
