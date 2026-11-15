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

    [Header("Canvas raíz del sistema de menús")]
    [Tooltip("Cuelga del UIManager y persiste con el.")]
    public Canvas canvasMenues;

    [Header("Escenas donde NO debe mostrarse la UI")]
    public string[] escenasSinUI;

    protected override void OnBoot()
    {
        // Antes de mirar la escena: arrancando en el MainMenu se salia antes de
        // suscribirse y el Canvas no volvia a conectar la camara.
        SceneManager.sceneLoaded += OnSceneLoaded;

        string escenaActual = SceneManager.GetActiveScene().name;
        if (EscenaSinUI(escenaActual))
            OcultarTodo();
        else if (escenaActual != Escenas.MainMenu)
            MostrarHUD();
    }


    protected override void OnDestroy()
    {
        base.OnDestroy();

        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene escena, LoadSceneMode modo)
    {
        // Reconectar cámara si el Canvas usa ScreenSpaceCamera
        if (canvasMenues != null && canvasMenues.renderMode == RenderMode.ScreenSpaceCamera)
        {
            canvasMenues.worldCamera = Camera.main;
            Log.Info(this, $"UIManager: reconectada cámara principal al canvas tras cargar {escena.name}.");
        }
    }

    // === CONTROL DE PANELES ===
    public void MostrarHUD()
    {
        OcultarTodo();
        // El jugador puede apagar el HUD desde opciones; la preferencia se respeta.
        if (panelHUD) panelHUD.SetActive(SaveSystem.MostrarHUD);
        panelActual = panelHUD;
    }

    public void MostrarPausa()
    {
        if (panelPausa == null)
        {
            Log.Aviso(this, "UIManager: panelPausa no asignado.");
            return;
        }

        // Aseguramos que el canvas raíz esté activo
        if (!panelPausa.transform.root.gameObject.activeSelf)
            panelPausa.transform.root.gameObject.SetActive(true);

        panelPausa.SetActive(true);
        panelActual = panelPausa;
        Log.Info(this, "UIManager: mostrando panel de pausa.");
    }

    /// <summary>
    /// Si las opciones se abrieron desde el menu principal. Ahi se ofrece
    /// borrar el progreso; en la pausa no, porque hay una partida en curso.
    /// </summary>
    public bool OpcionesDesdeMenu { get; private set; }
    private System.Action alCerrarOpcionesDelMenu;

    // Las opciones se abren al lado de la barra de pausa, que queda visible:
    // juntas forman el bloque de 480x270 del arte.
    public void MostrarOpciones()
    {
        OpcionesDesdeMenu = false;

        if (panelOpciones) panelOpciones.SetActive(true);
        panelActual = panelOpciones;

        if (panelGeneral) TraerAlFrente(panelGeneral);
    }

    /// <summary>
    /// Abre el mismo panel de opciones que la pausa, desde el menu principal.
    /// alCerrar corre al tocar 'Volver', para que el menu se vuelva a mostrar.
    /// </summary>
    public void AbrirOpcionesDesdeMenu(System.Action alCerrar)
    {
        OpcionesDesdeMenu = true;
        alCerrarOpcionesDelMenu = alCerrar;

        if (panelOpciones) panelOpciones.SetActive(true);
        panelActual = panelOpciones;
    }

    public void OcultarTodo()
    {
        // Si se cierran las opciones del menu por este camino, el menu igual
        // tiene que enterarse: si no, sus botones quedan desactivados.
        if (OpcionesDesdeMenu)
            CerrarOpciones();

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

    /// <summary>Lo que hace 'Volver' en opciones: regresa a la pausa o al menu.</summary>
    public void CerrarOpciones()
    {
        if (OpcionesDesdeMenu)
        {
            if (panelOpciones) panelOpciones.SetActive(false);
            OpcionesDesdeMenu = false;

            var alCerrar = alCerrarOpcionesDelMenu;
            alCerrarOpcionesDelMenu = null;
            alCerrar?.Invoke();
            return;
        }

        if (panelOpciones != null && panelOpciones.activeSelf)
        {
            panelOpciones.SetActive(false);
            if (panelPausa != null && panelPausa.activeSelf)
            {
                panelActual = panelPausa;
                panelPausa.GetComponent<PauseUI>()?.EnfocarOpciones();
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

        AplicarUIDeEscena(SceneManager.GetActiveScene().name);

        // Reactivar raycasts UI en los niveles
        if (EscenaConHUD(SceneManager.GetActiveScene().name) && SceneManager.GetActiveScene().name != Escenas.Hub)
        {
            foreach (var cg in FindObjectsByType<CanvasGroup>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (cg.alpha > 0.9f)
                {
                    cg.blocksRaycasts = true;
                    cg.interactable = true;
                }
            }
        }
    }

    private bool EscenaConHUD(string escena) => escena != Escenas.MainMenu && !EscenaSinUI(escena);

    /// <summary>
    /// Deja la UI como corresponde a la escena: sin nada en el menu y las
    /// cinematicas, HUD en el Hub y los niveles. Lo usan el GameManager al
    /// cargar y el reparador de UI un frame despues.
    /// </summary>
    public void AplicarUIDeEscena(string escena)
    {
        if (EscenaConHUD(escena))
        {
            HUDHabilidad.Instance?.gameObject.SetActive(true);
            MostrarHUD();
            return;
        }

        OcultarTodo();
        HUDHabilidad.Instance?.gameObject.SetActive(false);
    }

    /// <summary>
    /// Escenas sin UI del juego: cinematicas, pantallas negras. El Hub y el
    /// menu tienen la suya aunque alguien los anote en la lista.
    /// </summary>
    public bool EscenaSinUI(string nombre)
    {
        if (nombre == Escenas.Hub || nombre == Escenas.MainMenu) return false;
        return System.Array.IndexOf(escenasSinUI, nombre) >= 0;
    }
}
