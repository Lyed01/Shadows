using UnityEngine;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("Paneles principales")]
    public GameObject panelHUD;
    public GameObject panelPausa;
    public GameObject panelOpciones;

    [Header("Subpaneles de opciones (opcional)")]
    public GameObject panelGeneral;
    public GameObject panelControles;
    public GameObject panelSonido;

    private GameObject panelActual;
    private Canvas canvasPrincipal;
    [Header("Canvas raíz del sistema de menús")]
    public Canvas canvasMenues;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        // 🔹 Hacer persistentes UIManager y su Canvas principal
        DontDestroyOnLoad(gameObject);

        if (canvasMenues != null)
        {
            DontDestroyOnLoad(canvasMenues.gameObject);
            Debug.Log($"🟢 Canvas persistente asignado: {canvasMenues.name}");
        }
        else
        {
            Debug.LogWarning("⚠️ UIManager: ningún Canvas asignado en 'canvasMenues'.");
        }
    }


    void Start()
    {
        MostrarHUD();
    }

    void OnEnable()
    {
        // 🔹 Cada vez que se carga una nueva escena, actualizamos la cámara del canvas
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(UnityEngine.SceneManagement.Scene escena, UnityEngine.SceneManagement.LoadSceneMode modo)
    {
        // Reconectar cámara si el Canvas usa ScreenSpaceCamera
        if (canvasPrincipal != null && canvasPrincipal.renderMode == RenderMode.ScreenSpaceCamera)
        {
            canvasPrincipal.worldCamera = Camera.main;
            Debug.Log("🎥 UIManager: reconectada cámara principal al canvas tras cambiar escena.");
        }
    }

    // === CONTROL DE PANELES ===
    public void MostrarHUD()
    {
        OcultarTodo();
        if (panelHUD) panelHUD.SetActive(true);
        panelActual = panelHUD;
    }

    public void MostrarPausa()
    {
        if (panelPausa == null)
        {
            Debug.LogWarning("⚠️ UIManager: panelPausa no asignado.");
            return;
        }

        // Aseguramos que el canvas raíz esté activo
        if (!panelPausa.transform.root.gameObject.activeSelf)
            panelPausa.transform.root.gameObject.SetActive(true);

        panelPausa.SetActive(true);
        panelActual = panelPausa;

        Debug.Log("🟡 UIManager: mostrando panel de pausa.");
    }

    // 🟢 Mantiene pausa abierta al abrir opciones
    public void MostrarOpciones()
    {
        if (panelPausa && !panelPausa.activeSelf)
            panelPausa.SetActive(true);

        if (panelOpciones) panelOpciones.SetActive(true);
        panelActual = panelOpciones;

        if (panelGeneral) TraerAlFrente(panelGeneral);
    }

    public void VolverAPausa()
    {
        if (panelOpciones) panelOpciones.SetActive(false);
        if (panelPausa) panelPausa.SetActive(true);
        panelActual = panelPausa;
    }

    public void OcultarTodo()
    {
        if (panelHUD) panelHUD.SetActive(false);
        if (panelPausa) panelPausa.SetActive(false);
        if (panelOpciones) panelOpciones.SetActive(false);
    }

    // === SUBPANELES (opciones) ===
    public void TraerAlFrente(GameObject panel)
    {
        if (panel == null) return;
        panel.transform.SetAsLastSibling();
    }

    // === UTILES ===
    public void TogglePausa()
    {
        if (panelPausa != null && panelPausa.activeSelf)
            MostrarHUD();
        else
            MostrarPausa();
    }
    public void CerrarOpciones()
    {
        if (panelOpciones != null && panelOpciones.activeSelf)
        {
            panelOpciones.SetActive(false);
            if (panelPausa != null)
            {
                panelPausa.SetActive(true);
                panelActual = panelPausa;
            }
            else
            {
                MostrarHUD();
            }
        }
    }
}
