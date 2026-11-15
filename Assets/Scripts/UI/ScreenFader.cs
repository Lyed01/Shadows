using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;

/// <summary>
/// Transicion entre escenas: una sombra tramada cubre la pantalla desde Shawy
/// y se retira desde donde aparece en la escena nueva. Mientras cubre puede
/// mostrar la pantalla de carga (Shawy caminando y un consejo).
/// </summary>
[RequireComponent(typeof(CanvasGroup))]
public class ScreenFader : MonoBehaviour
{
    private CanvasGroup canvasGroup;
    private Coroutine currentFade;

    [Header("Configuración")]
    public float duracion = 1f;
    public Color color = Color.black;

    [Header("Sombra")]
    [Tooltip("Material con el shader Custom/SombraTransicion.")]
    public Material materialSombra;
    [Tooltip("Pixeles del juego por punto del tramado.")]
    public int tamanoPunto = 2;
    [Range(0.05f, 0.5f)] public float grano = 0.25f;

    [Header("Pantalla de carga")]
    public CanvasGroup carga;
    public Image shawy;
    public Sprite[] cuadrosShawy;
    public float cuadrosPorSegundo = 10f;
    public TextMeshProUGUI consejo;
    public float fundidoCarga = 0.25f;

    private Image fondo;
    private Material mat;
    private Canvas canvas;
    private float desde, hasta;
    private string[] consejos;
    private int ultimoConsejo = -1;
    private Coroutine cargaCoroutine;

    void Awake()
    {
        // Persiste con el UIManager, del que cuelga.
        canvasGroup = GetComponent<CanvasGroup>();
        canvas = GetComponentInParent<Canvas>();

        fondo = gameObject.AddComponent<Image>();
        fondo.rectTransform.anchorMin = Vector2.zero;
        fondo.rectTransform.anchorMax = Vector2.one;
        fondo.rectTransform.offsetMin = Vector2.zero;
        fondo.rectTransform.offsetMax = Vector2.zero;
        // Copia: el material del proyecto no se toca al animarlo.
        mat = new Material(materialSombra);
        mat.SetColor("_Color", color);
        fondo.material = mat;

        canvasGroup.alpha = 1f;
        if (carga != null) carga.alpha = 0f;
        Aplicar(0f, 0f);

        TextAsset archivo = Resources.Load<TextAsset>("Consejos");
        consejos = archivo != null
            ? System.Array.FindAll(archivo.text.Split('\n'), l => l.Trim().Length > 0 && !l.TrimStart().StartsWith("#"))
            : new string[0];
    }

    void Update()
    {
        if (carga == null || carga.alpha <= 0f || cuadrosShawy.Length == 0) return;
        shawy.sprite = cuadrosShawy[(int)(Time.unscaledTime * cuadrosPorSegundo) % cuadrosShawy.Length];
    }

    // Aparece negro inmediatamente (sin animación)
    public void InstantBlack()
    {
        if (currentFade != null) StopCoroutine(currentFade);
        Aplicar(0f, 1f);
    }

    public void InstantClear()
    {
        if (currentFade != null) StopCoroutine(currentFade);
        Aplicar(0f, 0f);
    }

    /// <summary>La sombra cubre la pantalla desde Shawy. Se puede esperar con yield return.</summary>
    public Coroutine FadeIn(float duracionPersonalizada = -1)
    {
        if (desde <= 0f && hasta >= 1f) return null; // ya cubierta
        if (currentFade != null) StopCoroutine(currentFade);
        currentFade = StartCoroutine(Animar(true, duracionPersonalizada));
        return currentFade;
    }

    /// <summary>La sombra se retira desde Shawy hacia los bordes.</summary>
    public Coroutine FadeOut(float duracionPersonalizada = -1)
    {
        if (currentFade != null) StopCoroutine(currentFade);
        currentFade = StartCoroutine(Animar(false, duracionPersonalizada));
        return currentFade;
    }

    private IEnumerator Animar(bool cubrir, float duracionPersonalizada)
    {
        float duracionUsada = (duracionPersonalizada > 0) ? duracionPersonalizada : duracion;
        UbicarCentro();

        // Sin tiempo escalado: se usa saliendo de la pausa.
        for (float t = 0f; t < duracionUsada; t += Time.unscaledDeltaTime)
        {
            float p = t / duracionUsada;
            if (cubrir) Aplicar(0f, p); else Aplicar(p, 1f);
            yield return null;
        }

        Aplicar(0f, cubrir ? 1f : 0f);
        currentFade = null;
    }

    // Desde Shawy si esta en escena; si no (menus), desde el centro.
    private void UbicarCentro()
    {
        float k = canvas.scaleFactor;
        Vector2 centro = new Vector2(Screen.width, Screen.height) * 0.5f;
        Jugador jugador = FindAnyObjectByType<Jugador>();
        Camera cam = Camera.main;
        if (jugador != null && cam != null)
            centro = cam.WorldToScreenPoint(jugador.transform.position + Vector3.up * 0.5f);

        float dx = Mathf.Max(centro.x, Screen.width - centro.x);
        float dy = Mathf.Max(centro.y, Screen.height - centro.y);
        mat.SetVector("_Centro", centro);
        mat.SetVector("_Pantalla", new Vector4(Screen.width, Screen.height, 0f, 0f));
        mat.SetFloat("_RadioMax", Mathf.Sqrt(dx * dx + dy * dy));
        mat.SetFloat("_TamPunto", tamanoPunto * k);
        mat.SetFloat("_Grano", grano);
    }

    private void Aplicar(float nuevoDesde, float nuevoHasta)
    {
        desde = nuevoDesde;
        hasta = nuevoHasta;
        mat.SetFloat("_Desde", desde);
        mat.SetFloat("_Hasta", hasta);
        bool cubre = hasta > desde;
        fondo.enabled = cubre;
        canvasGroup.blocksRaycasts = cubre;
    }

    /// <summary>Muestra u oculta a Shawy caminando y un consejo al azar.</summary>
    public void MostrarCarga(bool mostrar)
    {
        if (carga == null) return;
        if (mostrar && consejo != null && consejos.Length > 0)
        {
            int i = Random.Range(0, consejos.Length);
            if (i == ultimoConsejo) i = (i + 1) % consejos.Length;
            ultimoConsejo = i;
            consejo.text = consejos[i].Trim();
        }
        if (cargaCoroutine != null) StopCoroutine(cargaCoroutine);
        cargaCoroutine = StartCoroutine(FundirCarga(mostrar ? 1f : 0f));
    }

    private IEnumerator FundirCarga(float objetivo)
    {
        while (!Mathf.Approximately(carga.alpha, objetivo))
        {
            carga.alpha = Mathf.MoveTowards(carga.alpha, objetivo, Time.unscaledDeltaTime / fundidoCarga);
            yield return null;
        }
        cargaCoroutine = null;
    }
}
