using System;
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
public class AbilityEvent : UnityEvent<AbilityType> { }

public class AbilityManager : PersistentSingleton<AbilityManager>
{
    [Header("Prefabs de habilidades")]
    public GameObject prefabAbyssFlame;

    // === Eventos globales ===
    // Es un event y no un Action publico: desde afuera solo se puede suscribir
    // o desuscribir. Las habilidades avisan el uso con NotificarUso().
    public static event Action OnUsarHabilidad;

    public static void NotificarUso() => OnUsarHabilidad?.Invoke();

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
    private readonly HashSet<AbilityType> desbloqueadas = new();

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
        // CASO ESPECIAL: AbilityMode NO SE DESBLOQUEA
        if (tipo == AbilityType.AbilityMode)
        {
            Log.Info(this, "AbilityMode no se desbloquea; solo muestra el pop-up.");

            if (popupHabilidad != null)
                NotificarDesbloqueo(tipo);

            return; // No continúa hacia el desbloqueo real
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

        HabilidadSO datos = HabilidadSO.De(tipo);
        if (datos == null)
        {
            Log.Aviso(this, $"No hay asset de habilidad para {tipo}: no se muestra el popup.");
            return;
        }

        PopupManager.Obtener().EncolarHabilidad(popupHabilidad, datos, prioridad);
    }


    public bool IsUnlocked(AbilityType tipo) => desbloqueadas.Contains(tipo);

    // === PERSISTENCIA DE HABILIDADES ===
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

        Log.Info(this, "Habilidades sincronizadas con jugador en nueva escena.");
    }

    /// <summary>Bloquea todas las habilidades y borra su guardado. La usan Nueva partida y Opciones.</summary>
    [ContextMenu("Reiniciar progreso de habilidades")]
    public void ReiniciarProgreso()
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
    }
}
