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


        if (escena.name == "Hub")
        {
            if (primeraVezEnHub)
            {
                primeraVezEnHub = false;
                Debug.Log("🏠 Marcando primera visita REAL al Hub");
            }
        }
        // CoreManagers NO debe instanciar jugador
        if (escena.name == "CoreManagers")
        {
            return;
        }

        grid = FindFirstObjectByType<GridManager>();
        ability = FindFirstObjectByType<AbilityManager>();

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
        // Caso especial: HUB
        if (escena.name == "Hub")
        {
            // NO es la primera vez → volver frente a la puerta
            if (ultimaPuertaPosicion != Vector3.zero && !primeraVezEnHub)
            {
                Debug.Log("🚪 Regresando al Hub desde nivel → usando puerta");
                return ultimaPuertaPosicion;
            }
        }

        // 2 — Buscar solo en ESTA escena
        var sp = FindSpawnPointSoloDeLaEscena(escena);
        if (sp != null)
        {
            Debug.Log("📍 SpawnPoint ENCONTRADO EN ESTA ESCENA: " + sp.transform.position);
            return sp.transform.position;
        }

        // 3 — fallback GridManager
        if (grid != null && grid.spawnTransform != null)
            return grid.spawnTransform.position;

        // 4 — fallback final
        Debug.LogWarning("⚠ No hay SpawnPoint en esta escena. Usando Vector3.zero");
        return Vector3.zero;
    }



    private void SpawnJugadorEnEscena(Scene escena)
    {
        jugadorActual = FindFirstObjectByType<Jugador>();
        if (jugadorActual != null) return;

        Vector3 spawnPos = ResolverSpawn(escena);

        jugadorActual = Instantiate(jugadorPrefab, spawnPos, Quaternion.identity);
        jugadorActual.Inicializar(grid, HUDHabilidad.Instance);

        Debug.Log($"👤 Jugador instanciado en {escena.name} en {spawnPos}");
    }



    private void InstanciarJugador()
    {
        if (jugadorPrefab == null)
        {
            Debug.LogError("❌ GameManager: faltan referencias para instanciar jugador.");
            return;
        }

        Scene escenaActual = SceneManager.GetActiveScene();
        SpawnPoint sp = FindSpawnPointSoloDeLaEscena(escenaActual);

        Vector3 spawnPos;

        if (sp != null)
        {
            Debug.Log("📍 Respawn usando SpawnPoint de esta escena: " + sp.transform.position);
            spawnPos = sp.transform.position;
        }
        else if (grid != null && grid.spawnTransform != null)
        {
            Debug.Log("📍 Respawn usando spawnTransform del GridManager");
            spawnPos = grid.spawnTransform.position;
        }
        else
        {
            Debug.LogWarning("⚠ Respawn sin SpawnPoint. Usando Vector3.zero.");
            spawnPos = Vector3.zero;
        }

        jugadorActual = Instantiate(jugadorPrefab, spawnPos, Quaternion.identity);
        jugadorActual.Inicializar(grid, HUDHabilidad.Instance);
        StartCoroutine(EsperarYConectarCamara());
    }


    // =============================== UPDATE & GAME STATE ===============================

    void Update()
    {
        if (SceneManager.GetActiveScene().name == "MainMenu")
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
        ResetearEntornoInteractivo();
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

        // ========= Lógica correcta de retorno =========

        // Solo teletransportar si realmente venimos desde un nivel
        if (regresoDesdeNivel)
        {
            regresoDesdeNivel = false;

            // No teletransportar en la primera visita al Hub
            if (!primeraVezEnHub)
            {
                // Solo mover si la puerta tiene un valor válido
                if (ultimaPuertaPosicion != Vector3.zero && jugadorActual != null)
                {
                    jugadorActual.transform.position = ultimaPuertaPosicion;
                    jugadorActual.StartCoroutine(EfectoAparicion(jugadorActual.transform));

                    Debug.Log($"🚪 Jugador reapareció frente a puerta en {ultimaPuertaPosicion}");
                }
                else
                {
                    Debug.Log("⚠ No hay posición válida de puerta para teletransporte.");
                }
            }
        }

        // ==============================================

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
        if (escena.name == "MainMenu" || escena.name == "Menu" || escena.name == "MainMenuScene")
        {
            HUDHabilidad.Instance?.gameObject.SetActive(false);
            return;
        }

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
            if (sp.gameObject.scene.name == "CoreManagers")
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
