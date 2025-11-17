using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
public class UIManager : PersistentSingleton<UIManager>
{
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

    protected override void OnBoot()
    {
        string escenaActual = SceneManager.GetActiveScene().name;
        if (escenaActual == "Hub")
        {
            Debug.Log("🏠 UIManager: Hub detectado, no se muestra HUD persistente.");
        }
        else
        {
            MostrarHUD();
        }


        // Registrar evento de escena
        SceneManager.sceneLoaded += OnSceneLoaded;

        // Registrar Canvas como persistente
        if (canvasMenues != null)
        {
            DontDestroyOnLoad(canvasMenues.gameObject);
            canvasPrincipal = canvasMenues;
            Debug.Log($"🟢 Canvas persistente asignado: {canvasMenues.name}");
        }
        else
        {
            Debug.LogWarning("⚠️ UIManager: ningún Canvas asignado en 'canvasMenues'.");
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
        // Esperá un frame para que la escena haya cargado completamente
        yield return null;

        string escenaActual = SceneManager.GetActiveScene().name;
        Debug.Log($"🧩 Reinicializando UI para escena '{escenaActual}'...");

        // 1️⃣ Asegurar que haya un solo EventSystem activo
        var systems = FindObjectsByType<UnityEngine.EventSystems.EventSystem>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);

        if (systems.Length == 0)
        {
            var nuevo = new GameObject("EventSystem").AddComponent<UnityEngine.EventSystems.EventSystem>();
            nuevo.gameObject.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            Debug.Log("🎯 EventSystem recreado automáticamente.");
        }
        else
        {
            bool firstActive = false;
            foreach (var es in systems)
            {
                if (!firstActive)
                {
                    es.gameObject.SetActive(true);
                    es.enabled = true;
                    firstActive = true;
                }
                else
                {
                    Destroy(es.gameObject); // eliminar duplicados
                    Debug.LogWarning("🗑️ EventSystem duplicado eliminado.");
                }
            }
        }

        // 2️⃣ Reactivar CanvasGroups visibles
        foreach (var cg in FindObjectsByType<CanvasGroup>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (cg.alpha > 0.9f)
            {
                cg.blocksRaycasts = true;
                cg.interactable = true;
            }
        }

        // 3️⃣ Reconectar cámara del Canvas principal
        var canvases = FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var c in canvases)
        {
            if (c.isRootCanvas && c.renderMode == RenderMode.ScreenSpaceCamera)
                c.worldCamera = Camera.main;
        }

        // 4️⃣ Reactivar popup del Hub (si existe)
        var popup = FindFirstObjectByType<PopupNivelUI>(FindObjectsInactive.Include);
        if (popup != null)
        {
            popup.canvasGroup.blocksRaycasts = false;
            popup.canvasGroup.interactable = false;
            popup.gameObject.SetActive(true);
            Debug.Log("📦 PopupNivelUI preparado.");
        }

        Debug.Log("✅ UI reactivada correctamente.");
    }



}
