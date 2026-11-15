using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Pestaña General de opciones. HUD y timer se aplican al tocarlos; la
/// resolucion y la pantalla completa quedan pendientes hasta 'Aplicar', igual
/// que la pestaña de sonido, para que un clic de mas no cambie la ventana.
/// </summary>
public class OpcionesGeneralController : MonoBehaviour
{
    // El juego se dibuja a 512x288: solo sus multiplos enteros se ven sin
    // bordes ni pixeles desparejos.
    private const int AnchoBase = EscalaPixelUI.AnchoJuego;
    private const int AltoBase = EscalaPixelUI.AltoJuego;
    private const float SegundosParaConfirmar = 3f;

    [Header("Referencias UI")]
    public Toggle mostrarHUDToggle;
    public Toggle mostrarTimerToggle;
    public Toggle pantallaCompletaToggle;
    public SelectorOpciones resolucionSelector;
    public Button botonAplicar;
    public Button reiniciarProgresoButton;

    private readonly List<Vector2Int> resoluciones = new();
    private bool pantallaCompletaPendiente;
    private int resolucionPendiente;

    private TMP_Text textoReset;
    private string textoResetOriginal;
    private bool esperandoConfirmacion;

    void Start()
    {
        if (mostrarHUDToggle != null)
        {
            mostrarHUDToggle.SetIsOnWithoutNotify(SaveSystem.MostrarHUD);
            mostrarHUDToggle.onValueChanged.AddListener(ToggleHUD);
        }

        if (mostrarTimerToggle != null)
        {
            mostrarTimerToggle.SetIsOnWithoutNotify(SaveSystem.MostrarTimer);
            mostrarTimerToggle.onValueChanged.AddListener(ToggleTimer);
        }

        if (pantallaCompletaToggle != null)
        {
            pantallaCompletaPendiente = Screen.fullScreen;
            pantallaCompletaToggle.SetIsOnWithoutNotify(pantallaCompletaPendiente);
            pantallaCompletaToggle.onValueChanged.AddListener(v => pantallaCompletaPendiente = v);
        }

        if (resolucionSelector != null)
        {
            CargarResoluciones();
            resolucionSelector.alCambiar.AddListener(i => resolucionPendiente = i);
        }

        if (botonAplicar != null)
            botonAplicar.onClick.AddListener(AplicarPantalla);

        if (reiniciarProgresoButton != null)
        {
            textoReset = reiniciarProgresoButton.GetComponentInChildren<TMP_Text>(true);
            if (textoReset != null) textoResetOriginal = textoReset.text;
            reiniciarProgresoButton.onClick.AddListener(ReiniciarProgreso);
        }
    }

    // Borrar el progreso solo se ofrece desde el menu principal: en la pausa
    // hay una partida en curso.
    void OnEnable()
    {
        if (reiniciarProgresoButton != null)
            reiniciarProgresoButton.gameObject.SetActive(UIManager.Instance != null && UIManager.Instance.OpcionesDesdeMenu);
    }

    void OnDisable() => CancelarConfirmacion();

    // HUD Y TIMER

    private void ToggleHUD(bool mostrar)
    {
        SaveSystem.MostrarHUD = mostrar;

        // En el menu principal el HUD esta apagado: solo se guarda la preferencia.
        var ui = UIManager.Instance;
        if (ui != null && ui.panelHUD != null && SceneManager.GetActiveScene().name != Escenas.MainMenu)
            ui.panelHUD.SetActive(mostrar);
    }

    private void ToggleTimer(bool mostrar)
    {
        SaveSystem.MostrarTimer = mostrar;
        FindFirstObjectByType<LevelTimer>()?.AplicarVisibilidad();
    }

    // PANTALLA

    private void CargarResoluciones()
    {
        resoluciones.Clear();
        Resolution monitor = Screen.currentResolution;

        for (int k = 1; AnchoBase * k <= monitor.width && AltoBase * k <= monitor.height; k++)
            resoluciones.Add(new Vector2Int(AnchoBase * k, AltoBase * k));

        // La nativa del monitor siempre, para pantalla completa sin bordes.
        AgregarSiFalta(new Vector2Int(monitor.width, monitor.height));
        // Y la actual, para que el selector muestre donde esta la ventana.
        AgregarSiFalta(new Vector2Int(Screen.width, Screen.height));
        // De menor a mayor: izquierda achica, derecha agranda.
        resoluciones.Sort((a, b) => a.x != b.x ? a.x.CompareTo(b.x) : a.y.CompareTo(b.y));

        var opciones = new List<string>();
        foreach (Vector2Int r in resoluciones)
            opciones.Add($"{r.x} x {r.y}");

        resolucionPendiente = resoluciones.IndexOf(new Vector2Int(Screen.width, Screen.height));
        resolucionSelector.CargarOpciones(opciones, resolucionPendiente);
    }

    private void AgregarSiFalta(Vector2Int resolucion)
    {
        if (!resoluciones.Contains(resolucion))
            resoluciones.Add(resolucion);
    }

    private void AplicarPantalla()
    {
        if (resolucionPendiente < 0 || resolucionPendiente >= resoluciones.Count) return;

        Vector2Int r = resoluciones[resolucionPendiente];
        Screen.SetResolution(r.x, r.y,
            pantallaCompletaPendiente ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed);
    }

    // REINICIAR PROGRESO

    /// <summary>
    /// El primer clic pide confirmacion; el segundo, dentro de unos segundos,
    /// borra. Borrar el progreso no se puede deshacer.
    /// </summary>
    private void ReiniciarProgreso()
    {
        if (!esperandoConfirmacion)
        {
            esperandoConfirmacion = true;
            // rainyhearts no tiene '¿': lo dibuja como un corazon.
            if (textoReset != null) textoReset.text = "CONFIRMAR";
            StartCoroutine(VencerConfirmacion());
            return;
        }

        CancelarConfirmacion();
        SaveSystem.BorrarProgreso();
        AbilityManager.Instance?.ReiniciarProgreso();
        Log.Info(this, "Progreso reiniciado; las opciones se conservan.");
    }

    private IEnumerator VencerConfirmacion()
    {
        yield return new WaitForSecondsRealtime(SegundosParaConfirmar);
        CancelarConfirmacion();
    }

    private void CancelarConfirmacion()
    {
        StopAllCoroutines();
        esperandoConfirmacion = false;
        if (textoReset != null && textoResetOriginal != null) textoReset.text = textoResetOriginal;
    }
}
