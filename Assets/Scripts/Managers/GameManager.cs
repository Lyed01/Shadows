using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using System.Collections;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public enum GameState { Jugando, Pausado, Muerte, Transicion }
    public GameState EstadoActual { get; private set; } = GameState.Jugando;

    public static Action OnPlayerDeath;
    public static Action OnLevelRestart;
    public static Action OnPause;
    public static Action OnResume;

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
    public float tiempoReinicio = 1.0f;

    // === CICLO DE VIDA ===
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene escena, LoadSceneMode modo)
    {
        Debug.Log($"🌍 Escena cargada: {escena.name}");

        grid = FindFirstObjectByType<GridManager>();
        ability = FindFirstObjectByType<AbilityManager>();
        spawnTransform = grid?.spawnTransform;

        if (grid != null)
            grid.onPlayerDeath.AddListener(ManejarMuerteJugador);

        // 🟣 Instanciar jugador si no existe
        SpawnJugadorEnEscena(escena);

        // 🎥 Conectar cámara (en siguiente frame)
        StartCoroutine(EsperarYConectarCamara());

        // 🟢 Activar HUD persistente (si existe)
        if (HUDHabilidad.Instance != null)
        {
            HUDHabilidad.Instance.gameObject.SetActive(true);
            Debug.Log("🟢 HUD persistente activado.");
        }

        // 🧩 Sincronizar habilidades después de 1 frame
        StartCoroutine(SincronizarDespuesDeFrame());
        // 🧭 Reset de UI al entrar a niveles (excepto Hub)
        if (UIManager.Instance != null)
        {
            if (escena.name != "Hub")
            {
                UIManager.Instance.MostrarHUD(); // Oculta menús y muestra HUD
                Debug.Log("🎮 Reinicio de UI: solo HUD activo en nivel.");
            }
        }
    }

    private IEnumerator SincronizarDespuesDeFrame()
    {
        yield return null;
        if (AbilityManager.Instance != null && jugadorActual != null)
            AbilityManager.Instance.SincronizarJugador(jugadorActual);
    }

    private void SpawnJugadorEnEscena(Scene escena)
    {
        jugadorActual = FindFirstObjectByType<Jugador>();
        if (jugadorActual != null) return;

        Vector3 spawnPos;
        Vector3 posicionDefault = new Vector3(-4f, -25f, 0f);

        if (escena.name == "Hub")
            spawnPos = ultimaPuertaPosicion != Vector3.zero ? ultimaPuertaPosicion : Vector3.zero;
        else
            spawnPos = spawnTransform != null ? spawnTransform.position : Vector3.zero;

        if(escena.name == "Hub")
        {
            if (ultimaPuertaPosicion != Vector3.zero)
                spawnPos = ultimaPuertaPosicion;
            else
                spawnPos = posicionDefault;
        }
            
            


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

    // === CICLO DE JUEGO ===
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

        // 🟢 Crea nuevo jugador y reconecta cámara + HUD
        InstanciarJugador();

        OnLevelRestart?.Invoke();

        yield return new WaitForSeconds(0.1f);
        if (fader != null) fader.FadeOut(duracionFade);

        EstadoActual = GameState.Jugando;
    }

    private void InstanciarJugador()
    {
        if (jugadorPrefab == null)
        {
            Debug.LogError("❌ GameManager: faltan referencias para instanciar jugador.");
            return;
        }

        Vector3 spawnPos = spawnTransform != null ? spawnTransform.position : Vector3.zero;
        jugadorActual = Instantiate(jugadorPrefab, spawnPos, Quaternion.identity);

        jugadorActual.Inicializar(grid, HUDHabilidad.Instance);
        StartCoroutine(EsperarYConectarCamara());
    }

    // === PAUSA ===
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

    // === CAMBIO DE ESCENAS ===
    private IEnumerator CargarHubAsync()
    {
        EstadoActual = GameState.Transicion;
        Time.timeScale = 1f;

        if (fader != null) fader.FadeIn(duracionFade);
        yield return new WaitForSeconds(duracionFade);

        SceneManager.LoadScene("Hub");
        yield return null;

        if (fader != null) fader.FadeOut(duracionFade);
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

    public void VolverAlHub() => StartCoroutine(CargarHubAsync());
    public void CambiarEstado(GameState nuevoEstado) => EstadoActual = nuevoEstado;
    public bool EstaJugando => EstadoActual == GameState.Jugando;

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
}
