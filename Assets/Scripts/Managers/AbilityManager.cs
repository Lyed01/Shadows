using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public enum AbilityType
{
    AbilityMode,
    ShadowBlocks,
    ReflectiveBlocks,
    AbyssFlame,
    ShadowTp,
}
[System.Serializable]
public class DatosHabilidad
{
    public Sprite icono;
    public string titulo;
    public string descripcion;
}


[System.Serializable]
public class AbilityEvent : UnityEvent<AbilityType> { }

public class AbilityManager : PersistentSingleton<AbilityManager>
{
    [Header("Prefabs de habilidades")]
    public GameObject prefabAbyssFlame;

    // === Eventos globales ===
    public static Action OnUsarHabilidad;

    // === Estados internos ===
    private readonly Dictionary<AbilityType, bool> habilidades = new();

    // === Eventos locales (para UI y feedback) ===
    public AbilityEvent OnAbilityUnlocked = new();
    public AbilityEvent OnAbilityLocked = new();

    [Header("Feedback visual")]
    public PopupHabilidadUI popupHabilidad;


    protected override void OnBoot()
    {
        base.OnBoot();
        Debug.Log("🟢 AbilityManager persistente inicializado.");

        LoadProgress();

        // Suscripción global a eventos del juego
        GameManager.OnPlayerDeath += ReiniciarCargasGlobales;
    }

    private void OnDestroy()
    {
        GameManager.OnPlayerDeath -= ReiniciarCargasGlobales;
    }

    // === RESTABLECER CARGAS / MUERTE ===
    private void ReiniciarCargasGlobales()
    {
        Debug.Log("♻️ AbilityManager: Reiniciando cargas tras la muerte del jugador");

        var hud = HUDHabilidad.Instance;
        if (hud != null)
            hud.Reiniciar();
    }

    // === GESTIÓN DE HABILIDADES ===
    public void Unlock(AbilityType tipo)
    {
        // 🟦 CASO ESPECIAL: AbilityMode NO SE DESBLOQUEA
        if (tipo == AbilityType.AbilityMode)
        {
            Debug.Log("ℹ️ AbilityMode no se desbloquea; solo muestra el pop-up.");

            if (popupHabilidad != null)
            {
                var datos = ObtenerDatosHabilidad(tipo);
                popupHabilidad.Mostrar(datos.icono, datos.titulo, datos.descripcion);
            }

            return; // ⛔ No continúa hacia el desbloqueo real
        }

        // === DESBLOQUEO NORMAL PARA OTRAS HABILIDADES ===
        if (habilidades.TryGetValue(tipo, out bool activa) && activa)
            return;

        habilidades[tipo] = true;

        OnAbilityUnlocked.Invoke(tipo);

        PlayerPrefs.SetInt($"Habilidad_{tipo}", 1);
        PlayerPrefs.Save();

        if (popupHabilidad != null)
        {
            var datos = ObtenerDatosHabilidad(tipo);
            popupHabilidad.Mostrar(datos.icono, datos.titulo, datos.descripcion);
        }

        Debug.Log($"🌀 Habilidad desbloqueada: {tipo}");
    }


    public void Lock(AbilityType tipo)
    {
        if (!habilidades.ContainsKey(tipo)) return;

        habilidades[tipo] = false;
        OnAbilityLocked.Invoke(tipo);
        PlayerPrefs.SetInt($"Habilidad_{tipo}", 0);
        PlayerPrefs.Save();

        Debug.Log($"🛑 Habilidad bloqueada: {tipo}");
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
        List<AbilityType> activas = new();
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
                OnAbilityUnlocked.Invoke(tipo);
        }

        Debug.Log("📦 Habilidades cargadas desde PlayerPrefs.");
    }

    // === SINCRONIZACIÓN CON EL JUGADOR ===
    public void SincronizarJugador(Jugador jugador)
    {
        if (jugador == null) return;

        HUDHabilidad hud = jugador.hudHabilidad ?? HUDHabilidad.Instance;
        if (hud != null)
        {
            hud.gameObject.SetActive(true);
            hud.Reiniciar();
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
        Debug.Log("🧹 Reseteando TODAS las habilidades a estado bloqueado...");

        // 1. Borrar PlayerPrefs
        foreach (AbilityType tipo in Enum.GetValues(typeof(AbilityType)))
            PlayerPrefs.DeleteKey($"Habilidad_{tipo}");

        PlayerPrefs.Save();

        // 2. Vaciar el diccionario interno correctamente
        foreach (AbilityType tipo in Enum.GetValues(typeof(AbilityType)))
            habilidades[tipo] = false;

        // 3. Emitir eventos de bloqueo para que la UI se actualice
        foreach (AbilityType tipo in Enum.GetValues(typeof(AbilityType)))
            OnAbilityLocked?.Invoke(tipo);

        // 4. Reiniciar HUD si existe
        HUDHabilidad.Instance?.Reiniciar();

        Debug.Log("✨ TODAS las habilidades fueron bloqueadas y el estado fue limpiado.");
        LoadProgress();
    }


    public DatosHabilidad ObtenerDatosHabilidad(AbilityType tipo)
    {
        switch (tipo)
        {
           

            case AbilityType.ShadowBlocks:
                return new DatosHabilidad
                {
                    icono = Resources.Load<Sprite>("Sprites/Pixel/Iconos/ShadowBLock"),
                    titulo = "ShadowBLocks",
                    descripcion = "Coloca bloques de sombra para bloquear la luz. ¡Cuidado, no duran para siempre!. \n MouseButton1"
                };

            case AbilityType.ReflectiveBlocks:
                return new DatosHabilidad
                {
                    icono = Resources.Load<Sprite>("Sprites/Pixel/Iconos/MirrorBLock"),
                    titulo = "MirrorBlocks",
                    descripcion = "Redirige la luz amarilla, cuidado donde apuntas. \n pulsa MouseButton2 en el bloque para cambiar su dirección"
                };

            case AbilityType.AbyssFlame:
                return new DatosHabilidad
                {
                    icono = Resources.Load<Sprite>("Sprites/Pixel/Iconos/AbyssFlame"),
                    titulo = "AbyssFlame",
                    descripcion = "Proyecta una llama oscura que corrompe e interactua con el entorno.\n Muevete con WASD y extinguela con MouseButton2"
                };

            case AbilityType.ShadowTp:
                return new DatosHabilidad
                {
                    icono = Resources.Load<Sprite>("Sprites/Pixel/Iconos/ShadowTp"),
                    titulo = "ShadowTP",
                    descripcion = "Teletransportate entre zonas corruptas en un instante. \n MouseButton1 donde quieras teletransportarte"
                };


             case AbilityType.AbilityMode:
                return new DatosHabilidad
                {
                    icono = Resources.Load<Sprite>("Sprites/Pixel/Iconos/PulseEffect"),
                    titulo = "AbilityMode",
                    descripcion = "Activa el rango antes de usar cualquier habilidad. \n Tecla SPACE"
                };

            default:
                return new DatosHabilidad();
        }
    }

}
