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
        Log.Info(this, "AbilityManager persistente inicializado.");

        LoadProgress();

        // Suscripción global a eventos del juego
        GameManager.OnPlayerDeath += ReiniciarCargasGlobales;
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();

        GameManager.OnPlayerDeath -= ReiniciarCargasGlobales;
    }

    // === RESTABLECER CARGAS / MUERTE ===
    private void ReiniciarCargasGlobales()
    {
        Log.Info(this, "AbilityManager: Reiniciando cargas tras la muerte del jugador");

        var hud = HUDHabilidad.Instance;
        if (hud != null)
            hud.Reiniciar();
    }

    // === GESTIÓN DE HABILIDADES ===
    public void Unlock(AbilityType tipo)
    {
        //  CASO ESPECIAL: AbilityMode NO SE DESBLOQUEA
        if (tipo == AbilityType.AbilityMode)
        {
            Log.Info(this, "ℹ AbilityMode no se desbloquea; solo muestra el pop-up.");

            if (popupHabilidad != null)
            {
                var datos = ObtenerDatosHabilidad(tipo);
                popupHabilidad.Mostrar(datos.icono, datos.titulo, datos.descripcion);
            }

            return; //  No continúa hacia el desbloqueo real
        }

        // === DESBLOQUEO NORMAL PARA OTRAS HABILIDADES ===
        if (habilidades.TryGetValue(tipo, out bool activa) && activa)
            return;

        habilidades[tipo] = true;

        OnAbilityUnlocked.Invoke(tipo);

        SaveSystem.SetHabilidad(tipo, true);

        if (popupHabilidad != null)
        {
            var datos = ObtenerDatosHabilidad(tipo);
            popupHabilidad.Mostrar(datos.icono, datos.titulo, datos.descripcion);
        }

        Log.Info(this, $"Habilidad desbloqueada: {tipo}");
    }


    public void Lock(AbilityType tipo)
    {
        if (!habilidades.ContainsKey(tipo)) return;

        habilidades[tipo] = false;
        OnAbilityLocked.Invoke(tipo);
        SaveSystem.SetHabilidad(tipo, false);

        Log.Info(this, $"Habilidad bloqueada: {tipo}");
    }

    public bool IsUnlocked(AbilityType tipo)
    {
        return habilidades.ContainsKey(tipo) && habilidades[tipo];
    }

    public void ResetAll()
    {
        foreach (var tipo in new List<AbilityType>(habilidades.Keys))
            Lock(tipo);

        Log.Info(this, "Todas las habilidades han sido bloqueadas (reset global).");
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
            SaveSystem.SetHabilidad(kvp.Key, kvp.Value);
        Log.Info(this, "Progreso de habilidades guardado.");
    }

    public void LoadProgress()
    {
        foreach (AbilityType tipo in Enum.GetValues(typeof(AbilityType)))
        {
            bool desbloqueada = SaveSystem.GetHabilidad(tipo);
            habilidades[tipo] = desbloqueada;
            if (desbloqueada)
                OnAbilityUnlocked.Invoke(tipo);
        }

        Log.Info(this, "Habilidades cargadas desde PlayerPrefs.");
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

        Log.Info(this, "Habilidades sincronizadas con jugador en nueva escena.");
    }

#if UNITY_EDITOR
    [ContextMenu(" Resetear PlayerPrefs (debug)")]
#endif
    public void ResetearProgresoDebug()
    {
        Log.Info(this, "Reseteando TODAS las habilidades a estado bloqueado...");

        // 1. Borrar PlayerPrefs
        foreach (AbilityType tipo in Enum.GetValues(typeof(AbilityType)))
            SaveSystem.BorrarHabilidad(tipo);

        SaveSystem.Guardar();

        // 2. Vaciar el diccionario interno correctamente
        foreach (AbilityType tipo in Enum.GetValues(typeof(AbilityType)))
            habilidades[tipo] = false;

        // 3. Emitir eventos de bloqueo para que la UI se actualice
        foreach (AbilityType tipo in Enum.GetValues(typeof(AbilityType)))
            OnAbilityLocked?.Invoke(tipo);

        // 4. Reiniciar HUD si existe
        HUDHabilidad.Instance?.Reiniciar();

        Log.Info(this, "TODAS las habilidades fueron bloqueadas y el estado fue limpiado.");
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
