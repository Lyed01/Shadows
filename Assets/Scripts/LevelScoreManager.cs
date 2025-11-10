using UnityEngine;
using UnityEngine.SceneManagement;
using System;

[Serializable]
public class ScoreThreshold
{
    [Header("Muertes (límites para 3-2-1-0 estrellas)")]
    public int muertes3 = 1;
    public int muertes2 = 2;
    public int muertes1 = 3;

    [Header("Tiempo (segundos límites para 3-2-1-0 estrellas)")]
    public float tiempo3 = 60f;
    public float tiempo2 = 120f;
    public float tiempo1 = 300f;

    [Header("Uso de habilidades (límites para 3-2-1-0 estrellas)")]
    public int habilidades3 = 2;
    public int habilidades2 = 4;
    public int habilidades1 = 6;
}

public class LevelScoreManager : PersistentSingleton<LevelScoreManager>
{
    [Header("Configuración del nivel (runtime)")]
    [Tooltip("ID visible en PlayerPrefs: Nivel_<id>_Estrellas, etc.")]
    public string idNivel = "Nivel1";
    public ScoreThreshold configuracionNivel = new();

    [Header("Estadísticas actuales (read-only)")]
    [SerializeField] private int muertes;
    [SerializeField] private int habilidadesUsadas;
    [SerializeField] private float tiempoActual;

    private bool nivelEnCurso;
    private bool nivelFinalizado;

    // Eventos
    public static Action<int> OnNivelCompletado; // estrellas (0–3)
    public static Action OnNivelComenzo;

    protected override void OnBoot()
    {
        // Reset cuando:
        // - entra una escena de nivel
        // - el GameManager reinicia tras muerte
        SceneManager.sceneLoaded += OnSceneLoaded;
        GameManager.OnPlayerDeath += ReiniciarContadores;
        AbilityManager.OnUsarHabilidad += RegistrarUsoHabilidad;
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        GameManager.OnPlayerDeath -= ReiniciarContadores;
        AbilityManager.OnUsarHabilidad -= RegistrarUsoHabilidad;
    }

    private void OnSceneLoaded(Scene s, LoadSceneMode m)
    {
        // Heurística simple: si no es Hub/Menu, consideramos “nivel”
        bool esNivel = !(s.name.Contains("Hub") || s.name.Contains("Menu"));
        if (esNivel)
        {
            // Si querés IDs por escena: idNivel = s.name;
            ReiniciarContadores();
            nivelEnCurso = true;
            nivelFinalizado = false;
            OnNivelComenzo?.Invoke();
        }
        else
        {
            nivelEnCurso = false;
        }
    }

    private void Update()
    {
        if (!nivelEnCurso || nivelFinalizado) return;
        // Avanza solo cuando estamos jugando (no en pausa ni transición)
        if (GameManager.Instance != null && GameManager.Instance.EstaJugando)
            tiempoActual += Time.deltaTime;
    }

    // ===== API pública =====

    /// <summary> Úsalo si querés forzar ID y thresholds (ej: desde el nivel). </summary>
    public void BeginLevel(string id, ScoreThreshold thresholds = null)
    {
        idNivel = string.IsNullOrEmpty(id) ? "Nivel1" : id;
        if (thresholds != null) configuracionNivel = thresholds;
        ReiniciarContadores();
        nivelEnCurso = true;
        nivelFinalizado = false;
        OnNivelComenzo?.Invoke();
    }

    public void CompletarNivel()
    {
        if (!nivelEnCurso || nivelFinalizado) return;

        nivelFinalizado = true;
        int estrellasFinales = CalcularEstrellasFinales();

        Debug.Log($"🏁 Nivel {idNivel} completado con {estrellasFinales}⭐ " +
                  $"(Muertes: {muertes}, Tiempo: {tiempoActual:F1}s, Habs: {habilidadesUsadas})");

        GuardarResultados(idNivel, estrellasFinales, tiempoActual, muertes, habilidadesUsadas);
        OnNivelCompletado?.Invoke(estrellasFinales);
    }

    public void ReiniciarContadores()
    {
        muertes = 0;
        habilidadesUsadas = 0;
        tiempoActual = 0f;
        nivelFinalizado = false;
    }

    // ===== Registros =====
    public void RegistrarMuerte() => muertes++;
    public void RegistrarUsoHabilidad() => habilidadesUsadas++;

    // ===== Consultas =====
    public float GetTiempoNivel() => tiempoActual;
    public int GetMuertes() => muertes;
    public int GetHabilidadesUsadas() => habilidadesUsadas;

    // ===== Cálculo de estrellas =====
    private int CalcularPorMuertes()
    {
        if (muertes <= configuracionNivel.muertes3) return 3;
        if (muertes <= configuracionNivel.muertes2) return 2;
        if (muertes <= configuracionNivel.muertes1) return 1;
        return 0;
    }

    private int CalcularPorTiempo()
    {
        if (tiempoActual <= configuracionNivel.tiempo3) return 3;
        if (tiempoActual <= configuracionNivel.tiempo2) return 2;
        if (tiempoActual <= configuracionNivel.tiempo1) return 1;
        return 0;
    }

    private int CalcularPorHabilidades()
    {
        if (habilidadesUsadas <= configuracionNivel.habilidades3) return 3;
        if (habilidadesUsadas <= configuracionNivel.habilidades2) return 2;
        if (habilidadesUsadas <= configuracionNivel.habilidades1) return 1;
        return 0;
    }

    public int CalcularEstrellasFinales()
    {
        int eT = CalcularPorTiempo();
        int eM = CalcularPorMuertes();
        int eH = CalcularPorHabilidades();

        // Promedio redondeado (mantengo tu criterio)
        float promedio = (eT + eM + eH) / 3f;
        return Mathf.RoundToInt(promedio);

        // Alternativa más estricta (si querés castigar el peor eje):
        // return Mathf.Min(eT, Mathf.Min(eM, eH));
    }

    // ===== Guardado =====
    public void GuardarResultados(string nivelID, int estrellas, float tiempo, int muertes, int habilidades)
    {
        PlayerPrefs.SetInt($"Nivel_{nivelID}_Estrellas", estrellas);
        PlayerPrefs.SetFloat($"Nivel_{nivelID}_Tiempo", tiempo);
        PlayerPrefs.SetInt($"Nivel_{nivelID}_Muertes", muertes);
        PlayerPrefs.SetInt($"Nivel_{nivelID}_Habilidades", habilidades);
        PlayerPrefs.Save();

        Debug.Log($"💾 Guardado → {nivelID}: {estrellas}⭐ | {tiempo:F1}s | {muertes} muertes | {habilidades} habs");
    }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    [ContextMenu("🧹 Limpiar progreso de niveles (DEBUG)")]
    public void ResetProgresoNiveles()
    {
        int cantidadReseteada = 0;
        for (int i = 1; i <= 50; i++)
        {
            string nivel = $"Nivel{i}";
            string[] claves =
            {
                $"Nivel_{nivel}_Estrellas",
                $"Nivel_{nivel}_Tiempo",
                $"Nivel_{nivel}_Muertes",
                $"Nivel_{nivel}_Habilidades"
            };

            foreach (string clave in claves)
            {
                if (PlayerPrefs.HasKey(clave))
                {
                    PlayerPrefs.DeleteKey(clave);
                    cantidadReseteada++;
                }
            }
        }
        PlayerPrefs.Save();
        Debug.Log($"🧹 Progreso reseteado. Claves eliminadas: {cantidadReseteada}");
    }
#endif
}
