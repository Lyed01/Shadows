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
    /// <summary>
    /// Las habilidades que el jugador tiene desbloqueadas. Es un conjunto: una
    /// habilidad esta o no esta, nunca dos veces, y el orden no importa.
    ///
    /// Se usa la implementacion estatica. Las dos cuestan O(n) en todo, asi que
    /// la eleccion no se decide por tiempo: el conjunto no pasa de cinco
    /// elementos, uno por AbilityType, y entra entero en la capacidad inicial
    /// del arreglo sin tener que agrandarlo nunca. Guardarlo contiguo cuesta
    /// menos memoria que enlazar un nodo con su puntero por cada habilidad.
    ///
    /// Las consultas no corren por frame: se desbloquea al tocar un trigger o
    /// terminar un dialogo, y se lee al cargar la partida y al spawnear al
    /// jugador.
    /// </summary>
    private readonly ISimpleSet<AbilityType> desbloqueadas = new SimpleArraySet<AbilityType>();

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
            Log.Info(this, "AbilityMode no se desbloquea; solo muestra el pop-up.");

            if (popupHabilidad != null)
                NotificarDesbloqueo(tipo);

            return; //  No continúa hacia el desbloqueo real
        }

        // === DESBLOQUEO NORMAL PARA OTRAS HABILIDADES ===
        // Add ya avisa si estaba: si devuelve false no hay nada nuevo que anunciar.
        if (!desbloqueadas.Add(tipo))
            return;

        OnAbilityUnlocked.Invoke(tipo);

        SaveSystem.SetHabilidad(tipo, true);

        if (popupHabilidad != null)
            NotificarDesbloqueo(tipo);

        Log.Info(this, $"Habilidad desbloqueada: {tipo}");
    }

    /// <summary>
    /// Manda la notificacion a la fila del PopupManager en lugar de mostrarla
    /// de una, asi dos desbloqueos seguidos no se pisan.
    ///
    /// El modo habilidad va primero cuando coincide con otro: es la mecanica
    /// base y sin entenderla el resto no se puede usar.
    /// </summary>
    private void NotificarDesbloqueo(AbilityType tipo)
    {
        int prioridad = tipo == AbilityType.AbilityMode ? 1 : 2;

        PopupManager.Obtener().EncolarHabilidad(
            popupHabilidad, ObtenerDatosHabilidad(tipo), prioridad);
    }


    public void Lock(AbilityType tipo)
    {
        if (!desbloqueadas.Contains(tipo)) return;

        desbloqueadas.Remove(tipo);
        OnAbilityLocked.Invoke(tipo);
        SaveSystem.SetHabilidad(tipo, false);

        Log.Info(this, $"Habilidad bloqueada: {tipo}");
    }

    public bool IsUnlocked(AbilityType tipo)
    {
        return desbloqueadas.Contains(tipo);
    }

    public void ResetAll()
    {
        // Se copia antes de recorrer, porque Lock modifica el conjunto.
        foreach (var tipo in desbloqueadas.ToArray())
            Lock(tipo);

        Log.Info(this, "Todas las habilidades han sido bloqueadas (reset global).");
    }

    public List<AbilityType> GetUnlockedAbilities()
    {
        return new List<AbilityType>(desbloqueadas.ToArray());
    }

    // === PERSISTENCIA DE HABILIDADES ===
    public void SaveProgress()
    {
        // Se recorre el enum y no el conjunto, porque tambien hay que dejar
        // guardadas en false las que no estan desbloqueadas.
        foreach (AbilityType tipo in Enum.GetValues(typeof(AbilityType)))
            SaveSystem.SetHabilidad(tipo, desbloqueadas.Contains(tipo));
        Log.Info(this, "Progreso de habilidades guardado.");
    }

    public void LoadProgress()
    {
        foreach (AbilityType tipo in Enum.GetValues(typeof(AbilityType)))
        {
            if (!SaveSystem.GetHabilidad(tipo)) continue;

            desbloqueadas.Add(tipo);
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

        foreach (AbilityType _ in desbloqueadas)
            jugador.RecibirHabilidad();

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

        // 2. Vaciar el conjunto interno
        desbloqueadas.Clear();

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
