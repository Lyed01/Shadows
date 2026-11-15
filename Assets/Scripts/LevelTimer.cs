using UnityEngine;
using TMPro;

public class LevelTimer : MonoBehaviour
{
    [Header("Referencias")]
    public TextMeshProUGUI textoTimer; // Asignar desde el Canvas (si está en escena)

    [Tooltip("Lo que se muestra y se esconde junto con el timer. Si queda vacio, el padre del texto.")]
    public GameObject contenedorTimer;

    [Header("Configuración")]
    public bool iniciarAutomaticamente = true;

    [Header("Colores según rendimiento")]
    public Color colorBueno = new Color(0.4f, 1f, 0.4f);   // Verde
    public Color colorMedio = new Color(1f, 0.9f, 0.4f);   // Amarillo
    public Color colorMalo = new Color(1f, 0.4f, 0.4f);    // Rojo

    private float tiempoActual = 0f;
    private bool activo = false;
    private ScoreThreshold configNivel;

    void Start()
    {
        if (iniciarAutomaticamente)
            IniciarTimer();

        if (LevelScoreManager.Instance != null)
            configNivel = LevelScoreManager.Instance.configuracionNivel;

        // Si el texto no está asignado (HUD persistente, por ejemplo), lo busca
        if (textoTimer == null)
            textoTimer = FindTextoTimer();

        AplicarVisibilidad();
    }

    /// <summary>
    /// El timer del HUD es persistente y solo tiene sentido dentro de un nivel:
    /// lo muestra el LevelTimer del nivel, segun la opcion del jugador, y lo
    /// esconde al irse. Asi no queda en 00:00 en el Hub ni en el menu.
    /// </summary>
    public void AplicarVisibilidad()
    {
        GameObject contenedor = Contenedor();
        if (contenedor != null) contenedor.SetActive(SaveSystem.MostrarTimer);
    }

    private GameObject Contenedor()
    {
        if (contenedorTimer != null) return contenedorTimer;
        return textoTimer != null ? textoTimer.transform.parent.gameObject : null;
    }

    private void OnEnable()
    {
        GameManager.OnPause += Pausar;
        GameManager.OnResume += Reanudar;
        GameManager.OnPlayerDeath += Pausar;
        GameManager.OnLevelRestart += Reanudar;
    }

    void Update()
    {
        if (!activo) return;

        tiempoActual += Time.deltaTime;
        ActualizarTexto();
        ActualizarColor();
    }

    // === CONTROL DEL TIMER ===
    public void IniciarTimer()
    {
        tiempoActual = 0f;
        activo = true;
    }

    public void Pausar() => activo = false;
    public void Reanudar() => activo = true;
    public void DetenerTimer() => activo = false;

    public float GetTiempoActual() => tiempoActual;

    // === UI ===
    private void ActualizarTexto()
    {
        // Seguridad: si no hay referencia, intenta encontrarlo y salir
        if (textoTimer == null)
        {
            textoTimer = FindTextoTimer();
            if (textoTimer == null) return;
        }

        int minutos = Mathf.FloorToInt(tiempoActual / 60f);
        int segundos = Mathf.FloorToInt(tiempoActual % 60f);
        textoTimer.text = $"{minutos:00}:{segundos:00}";
    }

    private void ActualizarColor()
    {
        if (textoTimer == null || configNivel == null) return;

        if (tiempoActual <= configNivel.tiempo3)
            textoTimer.color = colorBueno;
        else if (tiempoActual <= configNivel.tiempo2)
            textoTimer.color = colorMedio;
        else
            textoTimer.color = colorMalo;
    }

    private TextMeshProUGUI FindTextoTimer()
    {
        // Busca un TMP dentro de un HUD persistente
        // Incluye inactivos: fuera de los niveles el timer queda apagado.
        var textos = FindObjectsByType<TextMeshProUGUI>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var t in textos)
        {
            if (t.name.ToLower().Contains("timer") || t.name.ToLower().Contains("tiempo"))
                return t;
        }

        Log.Aviso(this, "LevelTimer no encontró ningún TextMeshProUGUI con 'Timer' o 'Tiempo' en el nombre.");
        return null;
    }

    private void OnDisable()
    {
        GameObject contenedor = Contenedor();
        if (contenedor != null) contenedor.SetActive(false);

        GameManager.OnPause -= Pausar;
        GameManager.OnResume -= Reanudar;
        GameManager.OnPlayerDeath -= Pausar;
        GameManager.OnLevelRestart -= Reanudar;
    }
}
