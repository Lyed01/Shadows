using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;

public class PopupNivelUI : MonoBehaviour
{
    public static PopupNivelUI Instance { get; private set; }

    [Header("Referencias UI")]
    public Canvas canvas;                 // tu canvas existente (Screen Space Overlay/Camera)
    public CanvasGroup canvasGroup;       // del panel del popup
    public RectTransform panel;           // raíz del popup
    public TextMeshProUGUI textoTitulo;
    public TextMeshProUGUI textoDescripcion;
    public Image[] estrellasUI;           // 3 imágenes de estrellas
    public Button botonJugar;

    [Header("Sprites de estrellas")]
    public Sprite spriteEstrellaLlena;
    public Sprite spriteEstrellaVacia;

    [Header("Animación")]
    public float duracionFade = 0.2f;
    public Vector3 escalaOculto = new Vector3(0.9f, 0.9f, 1f);
    public Vector3 escalaVisible = Vector3.one;

    [Header("Posicionamiento")]
    public bool posicionarCercaDeLaPuerta = true;
    public Vector2 offsetPantalla = new Vector2(0f, 60f);

    private DoorHub puertaActual;
    private Coroutine animCoroutine;
    private Camera cam;
    private bool visible;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        cam = Camera.main;
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }
        if (panel != null) panel.localScale = escalaOculto;
        visible = false;
    }

    void Start()
    {
        if (botonJugar != null)
            botonJugar.onClick.AddListener(OnClickJugar);
    }

    public void Mostrar(DoorHub puerta, Vector3 worldAnchor, string titulo, string descripcion, int estrellas)
    {
        puertaActual = puerta;
        visible = true;

        if (textoTitulo) textoTitulo.text = titulo;
        if (textoDescripcion) textoDescripcion.text = descripcion;
        ActualizarEstrellas(estrellas);

        if (posicionarCercaDeLaPuerta)
            ActualizarPosicion(worldAnchor);

        if (animCoroutine != null) StopCoroutine(animCoroutine);
        animCoroutine = StartCoroutine(Fade(true));

        AudioManager.Instance?.ReproducirUIHover();
    }

    public void Ocultar()
    {
        if (!visible) return;
        visible = false;

        if (animCoroutine != null) StopCoroutine(animCoroutine);
        animCoroutine = StartCoroutine(Fade(false));
    }

    public void ActualizarPosicion(Vector3 worldAnchor)
    {
        if (!posicionarCercaDeLaPuerta || panel == null || canvas == null) return;

        Vector2 pantalla = RectTransformUtility.WorldToScreenPoint(cam, worldAnchor);
        pantalla += offsetPantalla;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvas.transform as RectTransform,
            pantalla,
            canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : cam,
            out Vector2 local
        );
        panel.anchoredPosition = local;
    }

    private IEnumerator Fade(bool mostrar)
    {
        float t = 0f;
        float ini = canvasGroup.alpha;
        float fin = mostrar ? 1f : 0f;
        Vector3 sIni = panel.localScale;
        Vector3 sFin = mostrar ? escalaVisible : escalaOculto;

        if (mostrar)
        {
            canvasGroup.blocksRaycasts = true;
            canvasGroup.interactable = true;
        }

        while (t < duracionFade)
        {
            t += Time.unscaledDeltaTime;
            float k = t / duracionFade;
            canvasGroup.alpha = Mathf.Lerp(ini, fin, k);
            panel.localScale = Vector3.Lerp(sIni, sFin, k);
            yield return null;
        }

        canvasGroup.alpha = fin;
        panel.localScale = sFin;

        if (!mostrar)
        {
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;
        }
    }

    // 🟡 NUEVO SISTEMA DE ESTRELLAS
    private void ActualizarEstrellas(int cantidad)
    {
        if (estrellasUI == null || estrellasUI.Length == 0) return;

        for (int i = 0; i < estrellasUI.Length; i++)
        {
            if (estrellasUI[i] == null) continue;

            if (spriteEstrellaLlena == null || spriteEstrellaVacia == null)
            {
                // Si no hay sprites asignados, usar fallback de color
                estrellasUI[i].color = i < cantidad
                    ? new Color(1f, 0.9f, 0.4f, 1f)
                    : new Color(0.35f, 0.35f, 0.35f, 0.35f);
            }
            else
            {
                // Usa sprites de “llena” o “vacía”
                estrellasUI[i].sprite = i < cantidad ? spriteEstrellaLlena : spriteEstrellaVacia;
                estrellasUI[i].enabled = true;
            }
        }
    }

    private void OnClickJugar()
    {
        if (puertaActual == null) return;
        AudioManager.Instance?.ReproducirUIClick();
        puertaActual.Entrar();
    }
}
