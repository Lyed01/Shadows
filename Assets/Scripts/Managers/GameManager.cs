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

    private bool EsHub => SceneManager.GetActiveScene().name == "Hub";

    // 🧠 Inicialización persistente
    protected override void OnBoot()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        Debug.Log("🟢 GameManager persistente inicializado.");
    }

    private void OnDestroy() => SceneManager.sceneLoaded -= OnSceneLoaded;

    private void OnSceneLoaded(Scene escena, LoadSceneMode modo)
    {
        Debug.Log($"🌍 Escena cargada: {escena.name}");

        grid = FindFirstObjectByType<GridManager>();
        ability = FindFirstObjectByType<AbilityManager>();

        // Mantener el del Inspector si ya está asignado
        if (spawnTransform == null)
            spawnTransform = grid?.spawnTransform;

        if (grid != null)
            grid.onPlayerDeath.AddListener(ManejarMuerteJugador);

        SpawnJugadorEnEscena(escena);
        StartCoroutine(EsperarYConectarCamara());
        StartCoroutine(SincronizarDespuesDeFrame());
        ActivarHUD(escena);

        if (jugadorActual != null)
            OnPlayerSpawned?.Invoke(jugadorActual);

        if (EsHub)
            ReactivarSistemaPopupsHub();

        UIManager.Instance?.ReinicializarUI();
    }

    // =============================== SPAWN SYSTEM ===============================

    private Vector3 ResolverSpawn(Scene escena)
    {
        Vector3 posicionDefault = new Vector3(-4f, -25f, 0f);

        Transform preferidoHub = (spawnTransform != null) ? spawnTransform : grid?.spawnTransform;
        Transform preferidoNivel = (grid?.spawnTransform != null) ? grid.spawnTransform : spawnTransform;

        if (escena.name == "Hub")
        {
            if (ultimaPuertaPosicion != Vector3.zero)
            {
                Debug.Log($"📍 Spawn en Hub: última puerta en {ultimaPuertaPosicion}");
                return ultimaPuertaPosicion;
            }

            if (preferidoHub != null)
            {
                Debug.Log($"📍 Spawn en Hub: usando spawnTransform {preferidoHub.position}");
                return preferidoHub.position;
            }

            Debug.Log($"📍 Spawn en Hub: posición por defecto {posicionDefault}");
            return posicionDefault;
        }
        else
        {
            if (preferidoNivel != null)
            {
                Debug.Log($"📍 Spawn en nivel: usando spawnTransform {preferidoNivel.position}");
                return preferidoNivel.position;
            }

            Debug.Log($"📍 Spawn en nivel: Vector3.zero");
            return Vector3.zero;
        }
    }

    private void SpawnJugadorEnEscena(Scene escena)
    {
        jugadorActual = FindFirstObjectByType<Jugador>();
        if (jugadorActual != null) return;

        Vector3 spawnPos = ResolverSpawn(escena);

        if (jugadorPrefab != null)
        {
            jugadorActual = Instantiate(jugadorPrefab, spawnPos, Quaternion.identity);
            jugadorActual.Inicializar(grid, HUDHabilidad.Instance);
            Debug.Log($"👤 Jugador instanciado en {escena.name} en {spawnPos}");
        }
        else
        {
            Debug.LogWarning("⚠️ GameManager: no se encontró prefab del jugador.");
        }
    }

    private void InstanciarJugador()
    {
        if (jugadorPrefab == null)
        {
            Debug.LogError("❌ GameManager: faltan referencias para instanciar jugador.");
            return;
        }

        Vector3 spawnPos = ResolverSpawn(SceneManager.GetActiveScene());
        jugadorActual = Instantiate(jugadorPrefab, spawnPos, Quaternion.identity);
        jugadorActual.Inicializar(grid, HUDHabilidad.Instance);
        StartCoroutine(EsperarYConectarCamara());
    }

    // =============================== UPDATE & GAME STATE ===============================

    void Update()
    {
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
        if (EstadoActual == GameState.Muerte) return;
        EstadoActual = GameState.Muerte;

        OnPlayerDeath?.Invoke();
        Invoke(nameof(ReiniciarFlujoDeJuego), tiempoReinicio);
    }

    private void ReiniciarFlujoDeJuego() => StartCoroutine(ReiniciarConFade());

    private IEnumerator ReiniciarConFade()
    {
        yield return new WaitForSeconds(0.2f);
        if (fader != null) fader.FadeIn(duracionFade);
        yield return new WaitForSeconds(duracionFade);

        if (jugadorActual != null)
            Destroy(jugadorActual.gameObject);

        grid?.EliminarShadowBlocks();
        grid?.ResetearCeldas();

        InstanciarJugador();
        OnLevelRestart?.Invoke();

        if (jugadorActual != null)
            OnPlayerSpawned?.Invoke(jugadorActual);

        if (EsHub)
            ReactivarSistemaPopupsHub();

        yield return new WaitForSeconds(0.1f);
        if (fader != null) fader.FadeOut(duracionFade);

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

        if (fader != null) fader.FadeIn(duracionFade);
        yield return new WaitForSeconds(duracionFade);

        SceneManager.LoadScene("Hub");
        yield return null;

        if (fader != null) fader.FadeOut(duracionFade);

        if (regresoDesdeNivel)
        {
            regresoDesdeNivel = false;
            if (jugadorActual != null)
            {
                jugadorActual.transform.position = ultimaPuertaPosicion;
                jugadorActual.StartCoroutine(EfectoAparicion(jugadorActual.transform));
                Debug.Log($"🚪 Jugador reapareció frente a puerta en {ultimaPuertaPosicion}");
            }
        }

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

        if (fader != null) fader.FadeIn(duracionFade);
        yield return new WaitForSeconds(duracionFade);

        var op = SceneManager.LoadSceneAsync(nombreEscena);
        while (!op.isDone)
            yield return null;

        if (fader != null) fader.FadeOut(duracionFade);
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
        if (escena.name != "Hub")
        {
            HUDHabilidad.Instance?.gameObject.SetActive(true);
            UIManager.Instance?.MostrarHUD();
        }
        else
        {
            Debug.Log("🏠 GameManager: En Hub, no se activa HUD del UIManager.");
        }
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
        var popup = FindFirstObjectByType<PopupNivelUI>(FindObjectsInactive.Include);
        if (popup != null)
        {
            popup.gameObject.SetActive(true);
            Debug.Log("🟢 PopupNivelUI activo en Hub.");
        }

        var puertas = FindObjectsByType<DoorHub>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        if (puertas != null && jugadorActual != null)
        {
            foreach (var p in puertas)
            {
                p.SendMessage("ForzarChequeoJugador", jugadorActual, SendMessageOptions.DontRequireReceiver);
            }
            Debug.Log("🔁 Detección de puertas reactivada.");
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

}
