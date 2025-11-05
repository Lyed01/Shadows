using UnityEngine;
using System;

[System.Serializable]
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

public class LevelScoreManager : MonoBehaviour
{
    public static LevelScoreManager Instance { get; private set; }

    [Header("Configuración del nivel")]
    public string idNivel = "Nivel1";
    public ScoreThreshold configuracionNivel = new ScoreThreshold();

    [Header("Estadísticas actuales")]
    [SerializeField] private int muertes;
    [SerializeField] private int habilidadesUsadas;
    [SerializeField] private float tiempoActual;

    private bool nivelFinalizado = false;

    // === EVENTOS ===
    public static Action<int> OnNivelCompletado; // devuelve estrellas totales (0–3)

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        GameManager.OnPlayerDeath += RegistrarMuerte;
        AbilityManager.OnUsarHabilidad += RegistrarUsoHabilidad;
    }

    void Update()
    {
        if (!nivelFinalizado && GameManager.Instance != null && GameManager.Instance.EstaJugando)
        {
            tiempoActual += Time.deltaTime;
        }
    }

    // === REGISTROS ===
    public void RegistrarMuerte() => muertes++;
    public void RegistrarUsoHabilidad() => habilidadesUsadas++;

    public void ReiniciarContadores()
    {
        muertes = 0;
        habilidadesUsadas = 0;
        tiempoActual = 0f;
        nivelFinalizado = false;
    }

    // === CONSULTAS ===
    public float GetTiempoNivel() => tiempoActual;
    public int GetMuertes() => muertes;
    public int GetHabilidadesUsadas() => habilidadesUsadas;

    // === CÁLCULO DE ESTRELLAS ===
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
        int estrellasTiempo = CalcularPorTiempo();
        int estrellasMuertes = CalcularPorMuertes();
        int estrellasHabilidades = CalcularPorHabilidades();

        float promedio = (estrellasTiempo + estrellasMuertes + estrellasHabilidades) / 3f;
        return Mathf.RoundToInt(promedio);
    }

    // === FINALIZACIÓN DE NIVEL ===
    public void CompletarNivel()
    {
        if (nivelFinalizado) return;
        nivelFinalizado = true;

        int estrellasFinales = CalcularEstrellasFinales();

        Debug.Log($"🏁 Nivel completado con {estrellasFinales}⭐ " +
                  $"(Muertes: {muertes}, Tiempo: {tiempoActual:F1}s, Habilidades: {habilidadesUsadas})");

        GuardarResultados(idNivel, estrellasFinales, tiempoActual, muertes, habilidadesUsadas);
        OnNivelCompletado?.Invoke(estrellasFinales);
    }

    // === GUARDADO DE RESULTADOS ===
    public void GuardarResultados(string nivelID, int estrellas, float tiempo, int muertes, int habilidades)
    {
        PlayerPrefs.SetInt($"Nivel_{nivelID}_Estrellas", estrellas);
        PlayerPrefs.SetFloat($"Nivel_{nivelID}_Tiempo", tiempo);
        PlayerPrefs.SetInt($"Nivel_{nivelID}_Muertes", muertes);
        PlayerPrefs.SetInt($"Nivel_{nivelID}_Habilidades", habilidades);
        PlayerPrefs.Save();

        Debug.Log($"💾 Guardado → {nivelID}: {estrellas}⭐ | {tiempo:F1}s | {muertes} muertes | {habilidades} habilidades");
    }

    private void OnDisable()
    {
        GameManager.OnPlayerDeath -= RegistrarMuerte;
        AbilityManager.OnUsarHabilidad -= RegistrarUsoHabilidad;
    }

    // === DEBUG / RESETEO ===
#if UNITY_EDITOR || DEVELOPMENT_BUILD
    [ContextMenu("🧹 Limpiar progreso de niveles (DEBUG)")]
    public void ResetProgresoNiveles()
    {
        int cantidadReseteada = 0;

        // 🔹 Borrar claves conocidas (Nivel_1 a Nivel_50)
        for (int i = 1; i <= 50; i++)
        {
            string nivel = $"Nivel{i}";
            string[] claves = {
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
        Debug.Log($"🧹 Progreso de niveles reseteado. Claves eliminadas: {cantidadReseteada}");
    }
#endif
}
