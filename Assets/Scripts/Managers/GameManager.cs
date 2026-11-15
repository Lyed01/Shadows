using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using System.Collections;

public partial class GameManager : PersistentSingleton<GameManager>
{
    public enum GameState { Jugando, Pausado, Muerte, Transicion }
    public GameState EstadoActual => estado.Tipo;

    // Un objeto por estado, en el orden del enum. Las clases estan en GameManager.Estados.cs.
    private EstadoJuego[] estados;
    private EstadoJuego estado;

    public static event Action OnPlayerDeath;
    public static event Action OnLevelRestart;
    public static event Action OnPause;
    public static event Action OnResume;
    public static event Action<Jugador> OnPlayerSpawned;

    [Header("Transición visual")]
    public ScreenFader fader;
    public float duracionFade = 0.75f;
    [Tooltip("Segundos minimos de la pantalla de carga, para alcanzar a leer el consejo.")]
    public float minimoPantallaCarga = 2.5f;

    [Header("Referencias principales")]
    public GridManager grid;
    public AbilityManager ability;
    public Jugador jugadorPrefab;

    private Jugador jugadorActual;
    private Vector3 ultimaPuertaPosicion;
    private bool regresoDesdeNivel = false;
    public float tiempoReinicio = 1.0f;

    private bool EsHub => SceneManager.GetActiveScene().name == Escenas.Hub;

    // Inicialización persistente
    protected override void OnBoot()
    {
        estados = new EstadoJuego[]
        {
            new EstadoJugando(this), new EstadoPausado(this),
            new EstadoMuerte(this), new EstadoTransicion(this),
        };
        for (int i = 0; i < estados.Length; i++)
            Debug.Assert((int)estados[i].Tipo == i, "Los estados no siguen el orden del enum GameState");
        estado = estados[(int)GameState.Jugando];

        SceneManager.sceneLoaded += OnSceneLoaded;
        Log.Info(this, "GameManager persistente inicializado.");
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene escena, LoadSceneMode modo)
    {
        Log.Info(this, $"Escena cargada: {escena.name}");

        // CoreManagers no lleva jugador.
        if (escena.name == Escenas.CoreManagers)
            return;

        grid = FindFirstObjectByType<GridManager>();
        ability = FindFirstObjectByType<AbilityManager>();

        if (grid != null)
            grid.onPlayerDeath.AddListener(ManejarMuerteJugador);

        // En los menus no se juega: antes aparecia un jugador en el centro del menu.
        bool esMenu = escena.name == Escenas.MainMenu || escena.name == Escenas.SplashScreen;
        if (esMenu)
            jugadorActual = null;
        else
        {
            SpawnJugador(escena, respawn: false);
            StartCoroutine(SincronizarDespuesDeFrame());
        }
        UIManager.Instance?.AplicarUIDeEscena(escena.name);

        if (jugadorActual != null)
            OnPlayerSpawned?.Invoke(jugadorActual);

        if (EsHub)
            ReactivarSistemaPopupsHub();

        UIManager.Instance?.ReinicializarUI();
    }


    // SPAWN SYSTEM
    /// <summary>
    /// Decide donde aparece el jugador. Al volver al Hub desde un nivel lo deja
    /// frente a la puerta que uso; en cualquier otro caso usa el SpawnPoint de la
    /// escena, y si no hay, el del GridManager.
    /// </summary>
    private Vector3 ResolverSpawn(Scene escena, bool respawn)
    {
        // Volver frente a la puerta solo aplica al entrar al Hub, no al renacer
        // dentro de el. Sin puerta guardada (partida nueva) se usa el SpawnPoint.
        if (!respawn && escena.name == Escenas.Hub && ultimaPuertaPosicion != Vector3.zero)
        {
            Log.Info(this, "Regreso al Hub: aparece frente a la puerta");
            return ultimaPuertaPosicion;
        }

        var sp = FindSpawnPointSoloDeLaEscena(escena);
        if (sp != null)
            return sp.transform.position;

        if (grid != null && grid.spawnTransform != null)
            return grid.spawnTransform.position;

        Log.Aviso(this, $"{escena.name} no tiene SpawnPoint. Usando Vector3.zero");
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

        Log.Info(this, $"Jugador instanciado en {escena.name} en {spawnPos}");

        StartCoroutine(EsperarYConectarCamara());
    }


    // UPDATE & GAME STATE
    void Update()
    {
        // La copia descartada del singleton no paso por OnBoot y no tiene estados.
        if (estado == null || SceneManager.GetActiveScene().name == Escenas.MainMenu)
            return;
        if (Input.GetKeyDown(KeyCode.Escape))
            estado.AlPresionarEscape();
    }

    // EstadoMuerte avisa la muerte y programa el reinicio. Nada de popups aca.
    private void ManejarMuerteJugador()
    {
        DialogueSystemWorld.Instance?.ForzarCerrarDialogo();
        CambiarEstado(GameState.Muerte);
    }


    private void ReiniciarFlujoDeJuego() => StartCoroutine(ReiniciarConFade());

    private IEnumerator ReiniciarConFade()
    {
        yield return new WaitForSeconds(0.2f);

        if (fader != null)
            yield return fader.FadeIn(duracionFade);

        if (jugadorActual != null)
            Destroy(jugadorActual.gameObject);

        ReinicioDeNivel.Reiniciar(grid);

        // Nuevo jugador
        SpawnJugador(SceneManager.GetActiveScene(), respawn: true);

        // AVISAR A TODOS LOS NPCs QUE HAY UN JUGADOR NUEVO
        if (jugadorActual != null)
            OnPlayerSpawned?.Invoke(jugadorActual);

        OnLevelRestart?.Invoke();

        // Si estamos en el Hub, reactivamos sistema de popups con el jugador NUEVO
        if (EsHub && jugadorActual != null)
        {
            ReactivarSistemaPopupsHub();
        }

        // Un frame para que la camara siga al jugador nuevo: la sombra se retira desde el.
        yield return null;
        fader?.FadeOut(duracionFade);
        yield return new WaitForSeconds(duracionFade);

        CambiarEstado(GameState.Jugando);
    }


    // EstadoPausado congela el tiempo y muestra la pausa al entrar, y lo
    // deshace al salir.
    public void PausarJuego() => CambiarEstado(GameState.Pausado);

    public void ReanudarJuego()
    {
        if (EstadoActual != GameState.Pausado) return;
        CambiarEstado(GameState.Jugando);
    }

    // CARGA DE ESCENAS

    /// <summary>Vuelve a cargar el nivel actual, con el fundido de siempre.</summary>
    public void ReiniciarNivel() => StartCoroutine(CargarEscenaAsync(SceneManager.GetActiveScene().name));

    public void IrAlMenuPrincipal() => StartCoroutine(CargarEscenaAsync(Escenas.MainMenu));

    public void VolverAlHub()
    {
        regresoDesdeNivel = true;
        StartCoroutine(CargarEscenaAsync(Escenas.Hub, AlLlegarAlHub));
    }

    /// <summary>Retoma la partida: al Hub si ya termino el Tutorial, si no al Tutorial.</summary>
    public void ContinuarPartida() =>
        CargarEscena(SaveSystem.TutorialTerminado ? Escenas.Hub : Escenas.Tutorial);

    /// <summary>Borra el progreso (no las opciones) y arranca desde el Tutorial.</summary>
    public void NuevaPartida()
    {
        SaveSystem.BorrarProgreso();
        AbilityManager.Instance?.ReiniciarProgreso();
        SaveSystem.MarcarPartidaIniciada();
        // Era de la partida anterior: si no, al llegar al Hub aparece en esa puerta.
        ultimaPuertaPosicion = Vector3.zero;
        CargarEscena(Escenas.Tutorial);
    }

    /// <summary>Carga una escena con la sombra y la pantalla de carga.</summary>
    public void CargarEscena(string nombreEscena) => StartCoroutine(CargarEscenaAsync(nombreEscena));

    // Al cargar la escena, ResolverSpawn ya dejo al jugador frente a la puerta
    // que uso. Aca solo queda el efecto de aparicion, y tiene que correr junto
    // con el fade: si se lanza despues, el jugador ya se veia en pantalla y
    // volver a escalarlo desde cero parece un segundo spawn.
    private void AlLlegarAlHub()
    {
        if (!regresoDesdeNivel) return;
        regresoDesdeNivel = false;

        if (jugadorActual != null)
            jugadorActual.StartCoroutine(EfectoAparicion(jugadorActual.transform));
    }


    public void CargarNivelDesdePuerta(DoorHub puerta)
    {
        if (puerta == null)
        {
            Debug.LogError(" CargarNivelDesdePuerta: puerta nula");
            return;
        }

        ultimaPuertaPosicion = puerta.puntoSpawnRetorno != null
            ? puerta.puntoSpawnRetorno.position
            : puerta.transform.position;

        Log.Info(this, $"Guardando punto de retorno del Hub: {ultimaPuertaPosicion}");
        StartCoroutine(CargarEscenaAsync(puerta.nombreEscenaNivel));
    }

    private IEnumerator CargarEscenaAsync(string nombreEscena, Action alCargar = null)
    {
        CambiarEstado(GameState.Transicion);
        // Si no, el dialogo sigue esperando a su globo, que se destruye con la escena.
        DialogueSystemWorld.Instance?.ForzarCerrarDialogo();

        if (fader != null)
        {
            yield return fader.FadeIn(duracionFade);
            fader.MostrarCarga(true);
        }

        // La escena se activa recien cuando paso el minimo: si no, el nivel
        // arrancaria (y su timer) debajo de la pantalla de carga.
        float inicio = Time.unscaledTime;
        var op = SceneManager.LoadSceneAsync(nombreEscena);
        op.allowSceneActivation = false;
        while (op.progress < 0.9f || Time.unscaledTime - inicio < minimoPantallaCarga)
            yield return null;
        op.allowSceneActivation = true;
        while (!op.isDone)
            yield return null;

        // Un frame para que la camara siga al jugador: la sombra se retira desde el.
        yield return null;
        alCargar?.Invoke();

        if (fader != null)
        {
            fader.MostrarCarga(false);
            fader.FadeOut(duracionFade);
        }
        yield return new WaitForSecondsRealtime(duracionFade);

        CambiarEstado(GameState.Jugando);
    }


    // UTILIDADES
    private IEnumerator EsperarYConectarCamara()
    {
        yield return null;
        var vcam = FindFirstObjectByType<Unity.Cinemachine.CinemachineCamera>();
        if (vcam != null && jugadorActual != null)
        {
            vcam.Follow = jugadorActual.transform;
            vcam.LookAt = jugadorActual.transform;
            Log.Info(this, "Cámara Cinemachine reconectada al nuevo jugador.");
        }
    }

    private IEnumerator SincronizarDespuesDeFrame()
    {
        yield return null;
        if (AbilityManager.Instance != null && jugadorActual != null)
            AbilityManager.Instance.SincronizarJugador(jugadorActual);
    }

    // HUB HELPERS
    /// <summary>
    /// Deja el popup del Hub listo tras cargar la escena o revivir al jugador.
    /// Ya no hace falta avisarle a cada puerta: PopupNivelUI recorre el registro
    /// de DoorHub por su cuenta y elige la mas cercana en cada frame.
    /// </summary>
    private void ReactivarSistemaPopupsHub()
    {
        var popup = FindFirstObjectByType<PopupNivelUI>(FindObjectsInactive.Include);

        if (popup == null)
        {
            Log.Aviso(this, "No hay PopupNivelUI en el Hub");
            return;
        }

        popup.gameObject.SetActive(true);
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

    // INTERFAZ PÚBLICA
    /// <summary>
    /// Permite consultar si el juego está en modo jugable (no pausado, muerto o en transición).
    /// </summary>
    public bool EstaJugando => EstadoActual == GameState.Jugando;

    /// <summary>
    /// Sale del estado actual y entra al nuevo. Pedir el estado en el que ya se
    /// esta no hace nada: asi morir dos veces o pausar en pausa no repiten efectos.
    /// </summary>
    public void CambiarEstado(GameState nuevoEstado)
    {
        if (estado.Tipo == nuevoEstado) return;
        estado.Salir();
        estado = estados[(int)nuevoEstado];
        estado.Entrar();
        Log.Info(this, $"Estado del juego cambiado a: {nuevoEstado}");
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
}
