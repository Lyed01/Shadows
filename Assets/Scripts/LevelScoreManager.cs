using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using System.Collections.Generic;

[Serializable]
public class ScoreThreshold
{
    public int muertes3 = 1;
    public int muertes2 = 2;
    public int muertes1 = 3;

    public float tiempo3 = 60f;
    public float tiempo2 = 120f;
    public float tiempo1 = 300f;

    public int habilidades3 = 2;
    public int habilidades2 = 4;
    public int habilidades1 = 6;
}

public class LevelScoreManager : PersistentSingleton<LevelScoreManager>
{
    [Header("ID actual del nivel")]
    public string idNivel = "Nivel1";

    [Header("Configuración cargada")]
    public ScoreThreshold configuracionNivel = new();

    [Header("Configuraciones por nivel (SO)")]
    public List<ScoreThresholdSO> configuracionesSO = new();

    [Header("Estadísticas")]
    [SerializeField] private int muertes;
    [SerializeField] private int habilidadesUsadas;
    [SerializeField] private float tiempoActual;

    private bool nivelEnCurso;
    private bool nivelFinalizado;

    public static Action<int> OnNivelCompletado;
    public static Action OnNivelComenzo;

    protected override void OnBoot()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        AbilityManager.OnUsarHabilidad += RegistrarUsoHabilidad;
        GameManager.OnPlayerDeath += RegistrarMuerte;
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();

        SceneManager.sceneLoaded -= OnSceneLoaded;
        AbilityManager.OnUsarHabilidad -= RegistrarUsoHabilidad;
        GameManager.OnPlayerDeath -= RegistrarMuerte;
    }


    private void OnSceneLoaded(Scene s, LoadSceneMode m)
    {
        // Una escena cuenta como nivel si tiene configuracion de puntuacion.
        // El criterio anterior era por descarte, y hacia entrar como niveles al
        // splash, al tutorial y a la cinematica final.
        bool esNivel = CargarConfiguracionDelNivel(s.name);

        if (esNivel)
        {
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

        if (GameManager.Instance != null && GameManager.Instance.EstaJugando)
            tiempoActual += Time.deltaTime;
    }

    private bool CargarConfiguracionDelNivel(string escena)
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

                Debug.Log($"[LevelScoreManager] Configuracion cargada para {escena}");
                return true;
            }
        }

        return false;
    }

    public void ReiniciarContadores()
    {
        muertes = 0;
        habilidadesUsadas = 0;
        tiempoActual = 0f;
        nivelFinalizado = false;
    }

    public void RegistrarMuerte()
    {
        muertes++;
    }

    public void RegistrarUsoHabilidad()
    {
        habilidadesUsadas++;
    }

    public float GetTiempoNivel() => tiempoActual;
    public int GetMuertes() => muertes;
    public int GetHabilidadesUsadas() => habilidadesUsadas;

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

    public void GuardarResultados(string nivelID, int estrellas, float tiempo, int muertes, int habilidades)
    {
        SaveSystem.GuardarResultado(nivelID, estrellas, tiempo, muertes, habilidades);

        Debug.Log($"[LevelScoreManager] {nivelID}: {estrellas} estrellas, {tiempo:F1}s, {muertes} muertes, {habilidades} habilidades");
    }
    [ContextMenu("🧹 Limpiar progreso de niveles (DEBUG)")]
    public void ResetProgresoNiveles()
    {
        int cantidadReseteada = 0;

        for (int i = 1; i <= 50; i++)
            cantidadReseteada += SaveSystem.BorrarProgresoNivel($"Nivel{i}");

        SaveSystem.Guardar();
        Debug.Log($"[LevelScoreManager] Progreso reseteado. Claves eliminadas: {cantidadReseteada}");
    }

}
