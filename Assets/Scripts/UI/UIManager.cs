using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
public class UIManager : PersistentSingleton<UIManager>
{
    [Header("Paneles principales")]
    public GameObject panelHUD;
    public GameObject panelPausa;
    public GameObject panelOpciones;

    [Header("Overlay de carga")]
    public CanvasGroup loadingOverlay;

    [Header("Subpaneles de opciones (opcional)")]
    public GameObject panelGeneral;
    public GameObject panelControles;
    public GameObject panelSonido;

    private GameObject panelActual;
    private Canvas canvasPrincipal;

    [Header("Canvas raíz del sistema de menús")]
    public Canvas canvasMenues;

    [Header("Escenas donde NO debe mostrarse la UI")]
    public string[] escenasSinUI;
    protected override void OnBoot()
    {
        string escenaActual = SceneManager.GetActiveScene().name;

        // 🚫 Escenas especiales SIN UI
        if (EscenaSinUI(escenaActual))
        {
            OcultarTodo();
        }
        else if (escenaActual == "MainMenu")
        {
            return;
        }
        else
        {
            // En el Hub se debe mostrar el HUD
            if (escenaActual == "Hub")
                MostrarHUD();
            else
                MostrarHUD();
        }

        // ✅ ESTO DEBE ESTAR DENTRO DEL MÉTODO
        SceneManager.sceneLoaded += OnSceneLoaded;

        if (canvasMenues != null)
        {
            DontDestroyOnLoad(canvasMenues.gameObject);
            canvasPrincipal = canvasMenues;
        }
    }



    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene escena, LoadSceneMode modo)
    {
        // Reconectar cámara si el Canvas usa ScreenSpaceCamera
        if (canvasPrincipal != null && canvasPrincipal.renderMode == RenderMode.ScreenSpaceCamera)
        {
            canvasPrincipal.worldCamera = Camera.main;
            Debug.Log($"🎥 UIManager: reconectada cámara principal al canvas tras cargar {escena.name}.");
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
    // === REPARADOR DE UI GLOBAL ===
    public void ReinicializarUI()
    {
        StartCoroutine(ReinicializarUICoroutine());
    }

    private IEnumerator ReinicializarUICoroutine()
    {
        yield return null;

        string escenaActual = SceneManager.GetActiveScene().name;

        // 🚫 Nunca mostrar UI en MainMenu
        if (escenaActual == "MainMenu")
        {
            Debug.Log("🔕 UIManager: MainMenu detectado → UI completamente desactivada.");
            OcultarTodo();
            HUDHabilidad.Instance?.gameObject.SetActive(false);
            yield break;
        }


        // 🚫 Si es escena sin UI → no activar HUD
        if (EscenaSinUI(escenaActual))
        {
            OcultarTodo();
            yield break;
        }
        // 🏠 HUB → sí debe mostrar HUD
        if (escenaActual == "Hub")
        {
            MostrarHUD();
            yield break;
        }


        // ✔ Cualquier otro nivel → mostrar HUD
        MostrarHUD();

        // EventSystem fix
        // Si NO HAY EventSystem → crear uno
        var systems = FindObjectsByType<UnityEngine.EventSystems.EventSystem>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None
        );

        if (systems.Length == 0)
        {
            Debug.Log("⚠ No EventSystem en escena, creando uno temporal.");
            var nuevo = new GameObject("EventSystem")
                .AddComponent<UnityEngine.EventSystems.EventSystem>();
            nuevo.gameObject.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();

            yield break;   // ✔ forma correcta

        }

        // SI EXISTE → dejarlo como está, NO TOCARLO
        systems[0].enabled = true;
        systems[0].gameObject.SetActive(true);


        // Reactivar raycasts UI
        foreach (var cg in FindObjectsByType<CanvasGroup>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (cg.alpha > 0.9f)
            {
                cg.blocksRaycasts = true;
                cg.interactable = true;
            }
        }

        // Reconectar cámara
        var canvases = FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var c in canvases)
        {
            if (c.isRootCanvas && c.renderMode == RenderMode.ScreenSpaceCamera)
                c.worldCamera = Camera.main;
        }
    }


    public bool EscenaSinUI(string nombre)
{
    // ❌ Estas escenas SIEMPRE deben mostrar su UI propia
    if (nombre == "Hub") return false;
    if (nombre == "MainMenu") return false;

    // ✔ Escenas realmente sin UI (cinemáticas, pantallas negras, etc.)
    foreach (var s in escenasSinUI)
        if (s == nombre)
            return true;

    return false;
    }

    public void MostrarOverlay()
    {
        if (loadingOverlay == null) return;

        loadingOverlay.alpha = 1f;
        loadingOverlay.blocksRaycasts = true;
        loadingOverlay.interactable = false;

        loadingOverlay.gameObject.SetActive(true);
    }

    public void OcultarOverlay()
    {
        if (loadingOverlay == null) return;

        StartCoroutine(FadeOutOverlay());
    }

    private IEnumerator FadeOutOverlay()
    {
        float dur = 0.35f;
        float t = 0f;

        while (t < dur)
        {
            t += Time.deltaTime;
            loadingOverlay.alpha = Mathf.Lerp(1f, 0f, t / dur);
            yield return null;
        }

        loadingOverlay.alpha = 0f;
        loadingOverlay.blocksRaycasts = false;
        loadingOverlay.gameObject.SetActive(false);
    }





}
