using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using System.Collections.Generic;

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
    [Header("ID actual del nivel")]
    [Tooltip("ID visible en los PlayerPrefs (Nivel_<id>_...)")]
    public string idNivel = "Nivel1";

    [Header("Configuración cargada (runtime)")]
    public ScoreThreshold configuracionNivel = new();

    [Header("Configuraciones por nivel (ScriptableObjects)")]
    public List<ScoreThresholdSO> configuracionesSO = new();

    [Header("Estadísticas actuales (solo lectura)")]
    [SerializeField] private int muertes;
    [SerializeField] private int habilidadesUsadas;
    [SerializeField] private float tiempoActual;

    private bool nivelEnCurso;
    private bool nivelFinalizado;

    // Eventos
    public static Action<int> OnNivelCompletado; // estrellas (0–3)
    public static Action OnNivelComenzo;

    // ============================================================
    // BOOT
    // ============================================================
    protected override void OnBoot()
    {
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

    // ============================================================
    // SCENE LOAD
    // ============================================================
    private void OnSceneLoaded(Scene s, LoadSceneMode m)
    {
        bool esNivel = !(s.name.Contains("Hub") || s.name.Contains("Menu"));

        if (esNivel)
        {
            CargarConfiguracionDelNivel(s.name);

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

    // ============================================================
    // UPDATE (TIEMPO)
    // ============================================================
    private void Update()
    {
        if (!nivelEnCurso || nivelFinalizado) return;

        if (GameManager.Instance != null && GameManager.Instance.EstaJugando)
            tiempoActual += Time.deltaTime;
    }

    // ============================================================
    // CARGA DE CONFIGURACIÓN POR SCRIPTABLEOBJECT
    // ============================================================
    private void CargarConfiguracionDelNivel(string escena)
    {
        foreach (var so in configuracionesSO)
        {
            if (so != null && so.idNivel == escena)
            {
                configuracionNivel.muertes3 = so.muertes3;
                configuracionNivel.muertes2 = so.muertes2;
                configuracionNivel.muertes1 = so.muertes1;

                configuracionNivel.tiempo3 = so.tiempo3;
                configuracionNivel.tiempo2 = so.tiempo2;
                configuracionNivel.tiempo1 = so.tiempo1;

                configuracionNivel.habilidades3 = so.habilidades3;
                configuracionNivel.habilidades2 = so.habilidades2;
                configuracionNivel.habilidades1 = so.habilidades1;

                idNivel = so.idNivel;

                Debug.Log($"📌 Score config cargada desde ScriptableObject para {escena}");
                return;
            }
        }

        Debug.LogWarning($"⚠️ No encontré ScriptableObject para {escena}. Usando valores default.");
    }

    // ============================================================
    // API PÚBLICA
    // ============================================================

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

    public void RegistrarMuerte() => muertes++;
    public void RegistrarUsoHabilidad() => habilidadesUsadas++;

    public float GetTiempoNivel() => tiempoActual;
    public int GetMuertes() => muertes;
    public int GetHabilidadesUsadas() => habilidadesUsadas;

    // ============================================================
    // CÁLCULO DE ESTRELLAS
    // ============================================================
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

        float promedio = (eT + eM + eH) / 3f;
        return Mathf.RoundToInt(promedio);
    }

    // ============================================================
    // GUARDADO
    // ============================================================
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
