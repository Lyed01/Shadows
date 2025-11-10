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

    // 🔔 Nuevo: evento global al spawnear/re-spawnear jugador
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
    private bool regresoDesdeNivel = false; // ✅ indica si se vuelve desde un nivel
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
        spawnTransform = grid?.spawnTransform;

        if (grid != null)
            grid.onPlayerDeath.AddListener(ManejarMuerteJugador);

        // 🟣 Instanciar jugador si no existe
        SpawnJugadorEnEscena(escena);

        // 🎥 Conectar cámara (en siguiente frame)
        StartCoroutine(EsperarYConectarCamara());

        // 🧩 Sincronizar habilidades y HUD
        StartCoroutine(SincronizarDespuesDeFrame());
        ActivarHUD(escena);

        // 🔔 Importante: anunciar spawn a todos los sistemas (Hub incluido)
        if (jugadorActual != null)
            OnPlayerSpawned?.Invoke(jugadorActual);

        // 🧷 Si estamos en el Hub, asegurar que los popups queden operativos
        if (EsHub)
            ReactivarSistemaPopupsHub();
    }

    private void ActivarHUD(Scene escena)
    {
        if (HUDHabilidad.Instance != null)
        {
            HUDHabilidad.Instance.gameObject.SetActive(true);
            Debug.Log("🟢 HUD persistente activado.");
        }

        if (UIManager.Instance != null)
        {
            if (escena.name != "Hub")
                UIManager.Instance.MostrarHUD();
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

        Vector3 spawnPos = Vector3.zero;
        Vector3 posicionDefault = new Vector3(-4f, -25f, 0f);

        if (escena.name == "Hub")
            spawnPos = (ultimaPuertaPosicion != Vector3.zero) ? ultimaPuertaPosicion : posicionDefault;
        else
            spawnPos = (spawnTransform != null) ? spawnTransform.position : Vector3.zero;

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

        // 🔔 anunciar respawn y rearmar popups si estamos en Hub
        if (jugadorActual != null)
            OnPlayerSpawned?.Invoke(jugadorActual);

        if (EsHub)
            ReactivarSistemaPopupsHub();

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

        Vector3 spawnPos = (spawnTransform != null) ? spawnTransform.position : Vector3.zero;
        jugadorActual = Instantiate(jugadorPrefab, spawnPos, Quaternion.identity);

        jugadorActual.Inicializar(grid, HUDHabilidad.Instance);
        StartCoroutine(EsperarYConectarCamara());
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

    private IEnumerator CargarHubAsync()
    {
        EstadoActual = GameState.Transicion;
        Time.timeScale = 1f;

        if (fader != null) fader.FadeIn(duracionFade);
        yield return new WaitForSeconds(duracionFade);

        SceneManager.LoadScene("Hub");
        yield return null;

        if (fader != null) fader.FadeOut(duracionFade);

        // ✅ Si volvemos desde un nivel, colocar jugador en la última puerta usada
        if (regresoDesdeNivel)
        {
            regresoDesdeNivel = false;
            if (jugadorActual != null)
            {
                jugadorActual.transform.position = ultimaPuertaPosicion;
                jugadorActual.StartCoroutine(EfectoAparicion(jugadorActual.transform));
                Debug.Log($"🚪 Jugador reapareció frente a puerta en {ultimaPuertaPosicion} con efecto visual.");
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

        // 🟢 Guardamos la posición de retorno del Hub
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

    public void VolverAlHub()
{
    regresoDesdeNivel = true;
    StartCoroutine(CargarHubAsync());
}
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

    // ====== Hub helpers ======
    private void ReactivarSistemaPopupsHub()
    {
        // 1) Asegurar que el popup global esté activo (si existe)
        var popup = FindFirstObjectByType<PopupNivelUI>(FindObjectsInactive.Include);
        if (popup != null)
        {
            popup.gameObject.SetActive(true);
            Debug.Log("🟢 PopupNivelUI activo en Hub.");
        }

        // 2) Reforzar chequeo inmediato de solape con puertas (por si el jugador renace dentro)
        var puertas = FindObjectsByType<DoorHub>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        if (puertas != null && jugadorActual != null)
        {
            foreach (var p in puertas)
            {
                try
                {
                    // Si DoorHub implementa este método, lo llamamos:
                    p.SendMessage("ForzarChequeoJugador", jugadorActual, SendMessageOptions.DontRequireReceiver);
                }
                catch { /* ignorar si no existe */ }
            }
            Debug.Log("🔁 Reforzado de detección de puertas en Hub tras respawn.");
        }
    }
    private IEnumerator EfectoAparicion(Transform objetivo)
    {
        if (objetivo == null) yield break;

        Vector3 escalaInicial = Vector3.zero;
        Vector3 escalaFinal = Vector3.one;
        float duracion = 0.35f;
        float tiempo = 0f;

        objetivo.localScale = escalaInicial;

        // Esperá un frame por seguridad (por si el fade todavía está activo)
        yield return null;

        while (tiempo < duracion)
        {
            tiempo += Time.deltaTime;
            float t = Mathf.Sin((tiempo / duracion) * (Mathf.PI * 0.5f)); // easing OutSine
            objetivo.localScale = Vector3.LerpUnclamped(escalaInicial, escalaFinal, t);
            yield return null;
        }

        objetivo.localScale = escalaFinal;
    }
}
