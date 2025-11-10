using UnityEngine;
using UnityEngine.SceneManagement;

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
}
