using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;


public enum AbilityType
{
    ShadowBlocks,
    ReflectiveBlocks,
    AbyssFlame,
    ShadowTp,
}

[System.Serializable]
public class AbilityEvent : UnityEvent<AbilityType> { }

public class AbilityManager : MonoBehaviour
{
    [Header("Prefabs de habilidades")]
    public GameObject prefabAbyssFlame;

    public static AbilityManager Instance { get; private set; }

    // === Eventos globales ===
    public static System.Action OnUsarHabilidad;

    // === Estados internos ===
    private readonly Dictionary<AbilityType, bool> habilidades = new();

    // === Eventos locales (para UI y feedback) ===
    public AbilityEvent OnAbilityUnlocked = new();
    public AbilityEvent OnAbilityLocked = new();

    // === CICLO DE VIDA ===
    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        LoadProgress();


        foreach (AbilityType tipo in System.Enum.GetValues(typeof(AbilityType)))
        {
            // Si ya estaba guardada como desbloqueada, mantenerla
            bool guardada = PlayerPrefs.GetInt($"Habilidad_{tipo}", 0) == 1;
            habilidades[tipo] = guardada;
        }
    }

    private void OnEnable()
    {
        // 🔹 Escuchar eventos del GameManager
        GameManager.OnPlayerDeath += ReiniciarCargasGlobales;
    }

    private void OnDisable()
    {
        GameManager.OnPlayerDeath -= ReiniciarCargasGlobales;
    }

    // === RESTABLECER CARGAS / MUERTE ===
    private void ReiniciarCargasGlobales()
    {
        Debug.Log("♻️ AbilityManager: Reiniciando cargas tras la muerte del jugador");

        // Reinicia el HUD global de habilidades
        var hud = FindFirstObjectByType<HUDHabilidad>();
        if (hud != null)
            hud.Reiniciar();
    }

    // === GESTIÓN DE HABILIDADES ===
    public void Unlock(AbilityType tipo)
    {
        if (habilidades.ContainsKey(tipo) && habilidades[tipo]) return;

        habilidades[tipo] = true;
        OnAbilityUnlocked.Invoke(tipo);

        Debug.Log($"🌀 Habilidad desbloqueada: {tipo}");
        PlayerPrefs.SetInt($"Habilidad_{tipo}", 1);
        PlayerPrefs.Save();
    }

    public void Lock(AbilityType tipo)
    {
        if (!habilidades.ContainsKey(tipo)) return;

        habilidades[tipo] = false;
        OnAbilityLocked.Invoke(tipo);

        Debug.Log($"🛑 Habilidad bloqueada: {tipo}");
        PlayerPrefs.SetInt($"Habilidad_{tipo}", 0);
        PlayerPrefs.Save();
    }

    public bool IsUnlocked(AbilityType tipo)
    {
        return habilidades.ContainsKey(tipo) && habilidades[tipo];
    }

    public void ResetAll()
    {
        foreach (var tipo in new List<AbilityType>(habilidades.Keys))
            Lock(tipo);

        Debug.Log("🔁 Todas las habilidades han sido bloqueadas (reset global).");
    }
    public List<AbilityType> GetUnlockedAbilities()
    {
        List<AbilityType> activas = new List<AbilityType>();
        foreach (var kvp in habilidades)
            if (kvp.Value) activas.Add(kvp.Key);
        return activas;
    }


    // === PERSISTENCIA DE HABILIDADES ===
    public void SaveProgress()
    {
        foreach (var kvp in habilidades)
            PlayerPrefs.SetInt($"Habilidad_{kvp.Key}", kvp.Value ? 1 : 0);

        PlayerPrefs.Save();
        Debug.Log("💾 Progreso de habilidades guardado.");
    }

    public void LoadProgress()
    {
        foreach (AbilityType tipo in Enum.GetValues(typeof(AbilityType)))
        {
            bool desbloqueada = PlayerPrefs.GetInt($"Habilidad_{tipo}", 0) == 1;
            habilidades[tipo] = desbloqueada;
            if (desbloqueada)
                OnAbilityUnlocked.Invoke(tipo); // actualiza el HUD automáticamente
        }

        Debug.Log("📦 Habilidades cargadas desde PlayerPrefs.");
    }


    public void SincronizarJugador(Jugador jugador)
    {
        if (jugador == null) return;

        HUDHabilidad hud = jugador.hudHabilidad ?? FindFirstObjectByType<HUDHabilidad>();
        if (hud != null)
        {
            hud.gameObject.SetActive(true);
            hud.Reiniciar(); // asegura cargas visibles
        }

        foreach (AbilityType tipo in habilidades.Keys)
        {
            if (habilidades[tipo])
                jugador.RecibirHabilidad();
        }

        Debug.Log("🔁 Habilidades sincronizadas con jugador en nueva escena.");
    }



#if UNITY_EDITOR
[ContextMenu("🧹 Resetear PlayerPrefs (debug)")]
#endif
    public void ResetearProgresoDebug()
    {
        foreach (AbilityType tipo in System.Enum.GetValues(typeof(AbilityType)))
            PlayerPrefs.DeleteKey($"Habilidad_{tipo}");

        PlayerPrefs.Save();
        Debug.Log("🧹 PlayerPrefs limpiado: todas las habilidades bloqueadas.");
    }
}
