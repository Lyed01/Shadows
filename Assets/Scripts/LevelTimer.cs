using UnityEngine;
using TMPro;

public class LevelTimer : MonoBehaviour
{
    [Header("Referencias")]
    public TextMeshProUGUI textoTimer; // Asignar desde el Canvas (si está en escena)

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

        // === Suscripción a eventos globales ===
        GameManager.OnPause += Pausar;
        GameManager.OnResume += Reanudar;
        GameManager.OnPlayerDeath += Pausar;
        GameManager.OnLevelRestart += Reanudar;
        LevelScoreManager.OnNivelCompletado += DetenerTimer;
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

    private void DetenerTimer(int estrellas) => DetenerTimer();

    public float GetTiempoActual() => tiempoActual;

    // === UI ===
    private void ActualizarTexto()
    {
        // 🔹 Seguridad: si no hay referencia, intenta encontrarlo y salir
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
        // 🔎 Busca un TMP dentro de un HUD persistente
        var textos = FindObjectsByType<TextMeshProUGUI>(FindObjectsSortMode.None);
        foreach (var t in textos)
        {
            if (t.name.ToLower().Contains("timer") || t.name.ToLower().Contains("tiempo"))
                return t;
        }

        Debug.LogWarning("⚠️ LevelTimer no encontró ningún TextMeshProUGUI con 'Timer' o 'Tiempo' en el nombre.");
        return null;
    }

    private void OnDisable()
    {
        GameManager.OnPause -= Pausar;
        GameManager.OnResume -= Reanudar;
        GameManager.OnPlayerDeath -= Pausar;
        GameManager.OnLevelRestart -= Reanudar;
        LevelScoreManager.OnNivelCompletado -= DetenerTimer;
    }
}
