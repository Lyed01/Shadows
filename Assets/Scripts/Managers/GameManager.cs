using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using System.Collections;

public class GameManager : PersistentSingleton<GameManager>
{
    public enum GameState { Jugando, Pausado, Muerte, Transicion }
    public GameState EstadoActual { get; private set; } = GameState.Jugando;

    public static Action OnPlayerDeath;
    public static Action OnLevelRestart;
    public static Action OnPause;
    public static Action OnResume;
    public static Action<Jugador> OnPlayerSpawned;

    [Header("Transición visual")]
    public ScreenFader fader;
    public float duracionFade = 0.75f;

    [Header("Referencias principales")]
    public GridManager grid;
    public AbilityManager ability;
    public Jugador jugadorPrefab;
    public Transform spawnTransform;

    private Jugador jugadorActual;
    private string ultimaPuertaID;
    private Vector3 ultimaPuertaPosicion;
    private bool regresoDesdeNivel = false;
    public float tiempoReinicio = 1.0f;
    private bool primeraVezEnHub = true;

    private bool EsHub => SceneManager.GetActiveScene().name == Escenas.Hub;

    // 🧠 Inicialización persistente
    protected override void OnBoot()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        Debug.Log("🟢 GameManager persistente inicializado.");
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene escena, LoadSceneMode modo)
    {

        Debug.Log($"🌍 Escena cargada: {escena.name}");


        if (escena.name == Escenas.Hub)
        {
            if (primeraVezEnHub)
            {
                primeraVezEnHub = false;
                Debug.Log("🏠 Marcando primera visita REAL al Hub");
            }
        }
        // CoreManagers NO debe instanciar jugador
        if (escena.name == Escenas.CoreManagers)
        {
            return;
        }

        grid = FindFirstObjectByType<GridManager>();
        ability = FindFirstObjectByType<AbilityManager>();

        if (grid != null)
            grid.onPlayerDeath.AddListener(ManejarMuerteJugador);

        SpawnJugador(escena, respawn: false);
        StartCoroutine(SincronizarDespuesDeFrame());
        ActivarHUD(escena);

        if (jugadorActual != null)
            OnPlayerSpawned?.Invoke(jugadorActual);

        if (EsHub)
            ReactivarSistemaPopupsHub();

        UIManager.Instance?.ReinicializarUI();
    }


    // =============================== SPAWN SYSTEM ===============================

    /// <summary>
    /// Decide donde aparece el jugador. Al volver al Hub desde un nivel lo deja
    /// frente a la puerta que uso; en cualquier otro caso usa el SpawnPoint de la
    /// escena, y si no hay, el del GridManager.
    /// </summary>
    private Vector3 ResolverSpawn(Scene escena, bool respawn)
    {
        // Volver frente a la puerta solo aplica al entrar al Hub, no al renacer
        // dentro de el.
        if (!respawn && escena.name == Escenas.Hub
            && ultimaPuertaPosicion != Vector3.zero && !primeraVezEnHub)
        {
            Debug.Log("[GameManager] Regreso al Hub: aparece frente a la puerta");
            return ultimaPuertaPosicion;
        }

        var sp = FindSpawnPointSoloDeLaEscena(escena);
        if (sp != null)
            return sp.transform.position;

        if (grid != null && grid.spawnTransform != null)
            return grid.spawnTransform.position;

        Debug.LogWarning($"[GameManager] {escena.name} no tiene SpawnPoint. Usando Vector3.zero");
        return Vector3.zero;
    }



    /// <summary>
    /// Deja un jugador vivo en la escena y la camara enganchada a el. Con
    /// respawn en true siempre instancia uno nuevo; si no, reutiliza el que ya
    /// estuviera en la escena.
    /// </summary>
    private void SpawnJugador(Scene escena, bool respawn)
    {
        // En un respawn siempre se instancia. El jugador anterior acaba de recibir
        // Destroy(), que Unity resuelve recien al final del frame, asi que la
        // referencia todavia no es nula: chequearla aca dejaria el nivel sin
        // jugador y sin reiniciar.
        if (!respawn)
        {
            jugadorActual = FindFirstObjectByType<Jugador>();

            if (jugadorActual != null)
            {
                StartCoroutine(EsperarYConectarCamara());
                return;
            }
        }

        if (jugadorPrefab == null)
        {
            Debug.LogError("[GameManager] Falta asignar el prefab del jugador");
            return;
        }

        Vector3 spawnPos = ResolverSpawn(escena, respawn);

        jugadorActual = Instantiate(jugadorPrefab, spawnPos, Quaternion.identity);
        jugadorActual.Inicializar(grid, HUDHabilidad.Instance);

        Debug.Log($"[GameManager] Jugador instanciado en {escena.name} en {spawnPos}");

        StartCoroutine(EsperarYConectarCamara());
    }





    // =============================== UPDATE & GAME STATE ===============================

    void Update()
    {
        if (SceneManager.GetActiveScene().name == Escenas.MainMenu)
            return;
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (EstadoActual == GameState.Jugando)
                PausarJuego();
            else if (EstadoActual == GameState.Pausado)
                ReanudarJuego();
        }
    }
    private void ManejarMuerteJugador()
    {
        DialogueSystemWorld.Instance?.ForzarCerrarDialogo();
        if (EstadoActual == GameState.Muerte) return;
        EstadoActual = GameState.Muerte;

        OnPlayerDeath?.Invoke(); // LevelScoreManager escucha esto

        // ❌ Nada de popups acá
        Invoke(nameof(ReiniciarFlujoDeJuego), tiempoReinicio);
    }




    private void ReiniciarFlujoDeJuego() => StartCoroutine(ReiniciarConFade());

    private IEnumerator ReiniciarConFade()
    {
        yield return new WaitForSeconds(0.2f);

        // Pantalla negra instantánea
        fader?.InstantBlack();
        yield return null;

        if (jugadorActual != null)
            Destroy(jugadorActual.gameObject);

        grid?.EliminarShadowBlocks();
        grid?.ResetearCeldas();
        ResetearEntornoInteractivo();

        // 👤 Nuevo jugador
        SpawnJugador(SceneManager.GetActiveScene(), respawn: true);

        // 🔑 AVISAR A TODOS LOS NPCs QUE HAY UN JUGADOR NUEVO
        if (jugadorActual != null)
            OnPlayerSpawned?.Invoke(jugadorActual);

        OnLevelRestart?.Invoke();

        // 🌌 Si estamos en el Hub, reactivamos sistema de popups con el jugador NUEVO
        if (EsHub && jugadorActual != null)
        {
            ReactivarSistemaPopupsHub();
        }

        // Fade suave de vuelta
        fader?.FadeOut(duracionFade);
        yield return new WaitForSeconds(duracionFade);

        EstadoActual = GameState.Jugando;
    }




    public void PausarJuego()
    {
        if (EstadoActual == GameState.Pausado) return;
        EstadoActual = GameState.Pausado;
        Time.timeScale = 0f;
        OnPause?.Invoke();
        UIManager.Instance?.MostrarPausa();
    }

    public void ReanudarJuego()
    {
        if (EstadoActual != GameState.Pausado) return;
        EstadoActual = GameState.Jugando;
        Time.timeScale = 1f;
        OnResume?.Invoke();
        UIManager.Instance?.MostrarHUD();
    }

    // =============================== CARGA DE ESCENAS ===============================

    public void VolverAlHub()
    {
        regresoDesdeNivel = true;
        StartCoroutine(CargarHubAsync());
    }

    private IEnumerator CargarHubAsync()
    {
        EstadoActual = GameState.Transicion;
        Time.timeScale = 1f;

        // 🔥 Cubrimos pantalla instantáneamente
        fader?.InstantBlack();
        yield return null;

        SceneManager.LoadScene(Escenas.Hub);
        yield return null;

        // Al cargar la escena, ResolverSpawn ya dejo al jugador frente a la
        // puerta que uso. Aca solo queda el efecto de aparicion, y tiene que
        // correr junto con el fade: si se lanza despues, el jugador ya se veia
        // en pantalla y volver a escalarlo desde cero parece un segundo spawn.
        if (regresoDesdeNivel)
        {
            regresoDesdeNivel = false;

            if (!primeraVezEnHub && jugadorActual != null)
                jugadorActual.StartCoroutine(EfectoAparicion(jugadorActual.transform));
        }

        fader?.FadeOut(duracionFade);
        yield return new WaitForSeconds(duracionFade);

        EstadoActual = GameState.Jugando;
    }



    public void CargarNivelDesdePuerta(DoorHub puerta)
    {
        if (puerta == null)
        {
            Debug.LogError("❌ CargarNivelDesdePuerta: puerta nula");
            return;
        }

        ultimaPuertaID = puerta.idNivel;
        ultimaPuertaPosicion = puerta.puntoSpawnRetorno != null
            ? puerta.puntoSpawnRetorno.position
            : puerta.transform.position;

        Debug.Log($"📍 Guardando punto de retorno del Hub: {ultimaPuertaPosicion}");
        StartCoroutine(CargarEscenaAsync(puerta.nombreEscenaNivel));
    }

    private IEnumerator CargarEscenaAsync(string nombreEscena)
    {
        EstadoActual = GameState.Transicion;
        Time.timeScale = 1f;

        // 🔥 Ocultamos pantalla INMEDIATO para evitar ver HUD o flashes
        fader?.InstantBlack();

        yield return null; // esperar un frame por seguridad

        var op = SceneManager.LoadSceneAsync(nombreEscena);
        while (!op.isDone)
            yield return null;

        // 🔥 Ahora que la escena ya cargó, limpiamos visualmente
        fader?.FadeOut(duracionFade);

        yield return new WaitForSeconds(duracionFade);

        EstadoActual = GameState.Jugando;
    }



    // =============================== UTILIDADES ===============================

    private IEnumerator EsperarYConectarCamara()
    {
        yield return null;
        var vcam = FindFirstObjectByType<Unity.Cinemachine.CinemachineCamera>();
        if (vcam != null && jugadorActual != null)
        {
            vcam.Follow = jugadorActual.transform;
            vcam.LookAt = jugadorActual.transform;
            Debug.Log("🎥 Cámara Cinemachine reconectada al nuevo jugador.");
        }
    }

    private void ActivarHUD(Scene escena)
    {
        string n = escena.name;

        // 🔴 ESCENAS QUE NUNCA DEBEN TENER HUD
        if (n == Escenas.MainMenu)
        {
            Debug.Log("🎛 GameManager: MainMenu → HUD apagado.");
            UIManager.Instance?.OcultarTodo();
            HUDHabilidad.Instance?.gameObject.SetActive(false);
            return;
        }

        // 🔴 ESCENAS MARCADAS COMO CINEMÁTICA
        if (UIManager.Instance != null && UIManager.Instance.EscenaSinUI(n))
        {
            Debug.Log($"🚫 GameManager: '{n}' está marcada como sin UI.");
            UIManager.Instance.OcultarTodo();
            HUDHabilidad.Instance?.gameObject.SetActive(false);
            return;
        }

        // 🟣 HUB — mostrar HUD normal
        if (escena.name == Escenas.Hub)
        {
            Debug.Log("🏠 GameManager: En Hub → mostrar HUD.");
            HUDHabilidad.Instance?.gameObject.SetActive(true);
            UIManager.Instance?.MostrarHUD();
            return;
        }


        // 🟢 NIVELES NORMALES — mostrar HUD
        Debug.Log("🎮 Nivel normal → mostrar HUD.");
        HUDHabilidad.Instance?.gameObject.SetActive(true);
        UIManager.Instance?.MostrarHUD();
    }




    private IEnumerator SincronizarDespuesDeFrame()
    {
        yield return null;
        if (AbilityManager.Instance != null && jugadorActual != null)
            AbilityManager.Instance.SincronizarJugador(jugadorActual);
    }

    // =============================== HUB HELPERS ===============================

    private void ReactivarSistemaPopupsHub()
    {
        Debug.Log("🔁 ReactivarSistemaPopupsHub() llamado. Escena actual: "
                  + SceneManager.GetActiveScene().name
                  + " | jugadorActual = " + (jugadorActual ? jugadorActual.name : "NULL"));

        var popup = FindFirstObjectByType<PopupNivelUI>(FindObjectsInactive.Include);
        if (popup != null)
        {
            popup.gameObject.SetActive(true);
            Debug.Log("🟢 PopupNivelUI activo en Hub.");
        }
        else
        {
            Debug.LogWarning("⚠️ No encontré PopupNivelUI en el Hub.");
        }

        var puertas = FindObjectsByType<DoorHub>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        Debug.Log("🚪 Puertas encontradas en Hub: " + puertas.Length);

        if (puertas != null && jugadorActual != null)
        {
            foreach (var p in puertas)
            {
                Debug.Log("➡️ Mandando ForzarChequeoJugador a puerta: " + p.name);
                p.SendMessage("ForzarChequeoJugador", jugadorActual, SendMessageOptions.DontRequireReceiver);
            }
            Debug.Log("✅ Detección de puertas reactivada.");
        }
    }


    private IEnumerator EfectoAparicion(Transform objetivo)
    {
        if (objetivo == null) yield break;
        Vector3 escalaInicial = Vector3.zero;
        Vector3 escalaFinal = Vector3.one;
        float duracion = 0.35f, tiempo = 0f;

        objetivo.localScale = escalaInicial;
        yield return null;

        while (tiempo < duracion)
        {
            tiempo += Time.deltaTime;
            float t = Mathf.Sin((tiempo / duracion) * (Mathf.PI * 0.5f));
            objetivo.localScale = Vector3.LerpUnclamped(escalaInicial, escalaFinal, t);
            yield return null;
        }

        objetivo.localScale = escalaFinal;
    }

    // =============================== INTERFAZ PÚBLICA ===============================

    /// <summary>
    /// Permite consultar si el juego está en modo jugable (no pausado, muerto o en transición).
    /// </summary>
    public bool EstaJugando => EstadoActual == GameState.Jugando;

    /// <summary>
    /// Cambia manualmente el estado del juego (usado por triggers u otros sistemas).
    /// </summary>
    public void CambiarEstado(GameState nuevoEstado)
    {
        EstadoActual = nuevoEstado;
        Debug.Log($"🔄 Estado del juego cambiado a: {nuevoEstado}");
    }
    private SpawnPoint FindSpawnPointSoloDeLaEscena(Scene escenaNivel)
    {
        // Buscar todos los SpawnPoints, incluso inactivos
        var todos = GameObject.FindObjectsByType<SpawnPoint>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None
        );

        foreach (var sp in todos)
        {
            if (sp == null) continue;

            // Ignorar CoreManagers SIEMPRE
            if (sp.gameObject.scene.name == Escenas.CoreManagers)
                continue;

            // Ignorar cualquier escena que no sea la que Unity tiene activa
            if (sp.gameObject.scene != escenaNivel)
                continue;

            return sp;
        }

        return null;
    }
    private void ResetearEntornoInteractivo()
    {
        // 🔘 Reiniciar TODOS los switches
        foreach (var sw in FindObjectsByType<Switch>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            sw.ResetSwitch();

        // 🔆 Reiniciar TODOS los receptores de luz
        foreach (var rec in FindObjectsByType<LightReceptor>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            rec.ResetReceptor();

        // 💡 Reiniciar TODOS los SpotLights cónicos
        foreach (var luz in FindObjectsByType<SpotLightDetector>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            luz.ResetToInitialState();

        // 🔆 Reiniciar TopLights cenitales (si querés lo mismo)
        foreach (var top in FindObjectsByType<TopLightDetector>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            top.ResetToInitialState();

        // 🚪 Reiniciar todas las puertas a su estado inicial
        foreach (var p in FindObjectsByType<Door>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            p.ResetToInitialState();
    }
}
