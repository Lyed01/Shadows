using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Un nivel de demostracion: escena, objetivo y habilidades con las que arranca.</summary>
public class Demo
{
    public string Escena;
    public string Titulo;
    public string Descripcion;
    public string Controles;
    public AbilityType[] Habilidades;
    public bool GuiaAutomatica;
}

/// <summary>
/// Niveles de demostracion de la materia: tres niveles del juego donde los diez TP
/// actuan a la vez, con un relato en vivo al costado (Narrador). Vive en
/// DontDestroyOnLoad (se crea solo, como el panel F1): reconoce la escena que se
/// carga, deja al jugador con las habilidades que toca y lleva el tiempo del nivel.
/// F2 vuelve al indice. No agrega controles propios: se juega con los del juego.
/// </summary>
public class Demostracion : MonoBehaviour
{
    private const string Controles =
        "WASD: mover | Espacio: modo habilidad | 1-4 o rueda: elegir habilidad | clic: usar | Z: deshacer bloque | F1: ocultar relato | F2: indice";

    public static readonly Demo[] Todas =
    {
        new Demo
        {
            Escena = "Demo_Nivel1",
            Titulo = "Nivel 1 - Aprender con Noxel",
            Descripcion = "Habla con Noxel: te entrega la primera habilidad. Llega a la salida sin que el perseguidor te alcance.",
            Controles = Controles,
            Habilidades = new AbilityType[0],
        },
        new Demo
        {
            Escena = "Demo_Nivel2",
            Titulo = "Nivel 2 - Bloques y espejos",
            Descripcion = "Un guia repite su rutina solo. Usa los bloques de sombra y espejo, y encerra al perseguidor para que no encuentre camino.",
            Controles = Controles,
            Habilidades = new[] { AbilityType.AbilityMode, AbilityType.ShadowBlocks, AbilityType.ReflectiveBlocks },
            GuiaAutomatica = true,
        },
        new Demo
        {
            Escena = "Demo_Nivel3",
            Titulo = "Nivel 3 - Todas las habilidades",
            Descripcion = "Con las cuatro habilidades, llega a la salida mientras el perseguidor te sigue.",
            Controles = Controles,
            Habilidades = new[] { AbilityType.AbilityMode, AbilityType.ShadowBlocks, AbilityType.ReflectiveBlocks, AbilityType.AbyssFlame, AbilityType.ShadowTp },
            GuiaAutomatica = true,
        },
    };

    public static Demo Actual { get; private set; }

    private bool[] habilidadesAlEntrar;
    private bool cerrando;
    private float inicioDelNivel;
    private bool arrancaDirecto;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Crear()
    {
        if (FindFirstObjectByType<Demostracion>() != null) return;

        var go = new GameObject(nameof(Demostracion));
        DontDestroyOnLoad(go);
        var demostracion = go.AddComponent<Demostracion>();
        demostracion.CrearPuertasDelRanking(go.transform);

        // Play directo desde una demo (en el editor): faltan los managers de
        // CoreManagers y su BootstrapLoader saltaria al menu. Se cargan los
        // managers sin ese salto y se recarga la demo, para que GameManager la vea
        // cargarse y le ponga el jugador.
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            string nombre = SceneManager.GetSceneAt(i).name;
            if (!EsDeDemostracion(nombre)) continue;

            demostracion.arrancaDirecto = true;
            demostracion.StartCoroutine(demostracion.ArrancarConManagers(nombre));
            return;
        }
    }

    private IEnumerator ArrancarConManagers(string demo)
    {
        if (!SceneManager.GetSceneByName(Escenas.CoreManagers).isLoaded)
            yield return SceneManager.LoadSceneAsync(Escenas.CoreManagers, LoadSceneMode.Additive);

        var bootstrap = FindFirstObjectByType<BootstrapLoader>();
        if (bootstrap != null) bootstrap.enabled = false;

        yield return null;
        arrancaDirecto = false;
        SceneManager.LoadScene(demo);
    }

    private static bool EsDeDemostracion(string escena)
    {
        if (escena == Escenas.Indice) return true;
        foreach (Demo d in Todas) if (d.Escena == escena) return true;
        return false;
    }

    private void OnEnable() => SceneManager.sceneLoaded += AlCargar;
    private void OnDisable() => SceneManager.sceneLoaded -= AlCargar;

    private void AlCargar(Scene escena, LoadSceneMode modo)
    {
        // sceneLoaded corre antes del Start del BootstrapLoader: ahi se lo apaga
        if (arrancaDirecto && escena.name == Escenas.CoreManagers)
        {
            var bootstrap = FindFirstObjectByType<BootstrapLoader>();
            if (bootstrap != null) bootstrap.enabled = false;
        }

        if (modo == LoadSceneMode.Single) Entrar(escena);
    }

    private void Entrar(Scene escena)
    {
        RestaurarHabilidades();
        StopAllCoroutines();

        Actual = null;
        foreach (Demo d in Todas) if (d.Escena == escena.name) Actual = d;

        Narrador.Limpiar();
        Narrador.Activo = Actual != null;
        PanelEstructurasEnJuego.Mostrar(Actual);
        if (Actual == null) return;

        inicioDelNivel = Time.time;
        Narrador.Decir(1, $"Lista: SimpleArrayList<DoorHub> guarda {DoorHub.Registradas.Count} puertas (una por nivel) y se lee por indice.");
        StartCoroutine(PrepararNivel(Actual));
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.F2) && SceneManager.GetActiveScene().name != Escenas.Indice)
            SceneManager.LoadScene(Escenas.Indice);
    }

    // === NIVEL ===
    private IEnumerator PrepararNivel(Demo demo)
    {
        // El jugador y el AbilityManager se conectan en los primeros frames
        yield return null;
        yield return null;

        GuardarHabilidades();
        foreach (AbilityType tipo in demo.Habilidades) AbilityManager.Instance?.Unlock(tipo);

        if (!demo.GuiaAutomatica) yield break;

        // Un NPC que repite su rutina solo: la cola de pasos se vacia a la vista
        yield return new WaitForSeconds(1f);
        foreach (var npc in FindObjectsByType<NPCDemostrador>(FindObjectsSortMode.None))
            npc.IniciarDemostracion();
    }

    /// <summary>La llama la salida del nivel: guarda el tiempo en el ranking y vuelve al indice.</summary>
    public static void CompletarNivel()
    {
        var instancia = FindFirstObjectByType<Demostracion>();
        if (Actual == null || instancia == null) return;

        instancia.StartCoroutine(instancia.Terminar(Actual));
    }

    private IEnumerator Terminar(Demo demo)
    {
        float tiempo = Mathf.Max(Time.time - inicioDelNivel, 0.1f);
        SaveSystem.GuardarResultado(demo.Escena, 3, tiempo, 0, 0);

        // El ranking ordena las marcas de todas las puertas con CollectionSort
        string[] marcas = RankingDeTiempos.Detalle().TrimEnd().Split('\n');
        Narrador.Decir(7, $"Sorting: CollectionSort ordena los tiempos de {DoorHub.Registradas.Count} niveles. Primero: {marcas[0].Trim()}");

        yield return new WaitForSeconds(3f);
        SceneManager.LoadScene(Escenas.Indice);
    }

    // === RANKING ===
    // Una puerta por nivel de demostracion, con su id propio: el ranking real no
    // se toca y la lista de puertas (TP01) y su orden por tiempo (TP07) funcionan
    // igual desde cualquier nivel.
    private void CrearPuertasDelRanking(Transform padre)
    {
        foreach (Demo demo in Todas)
        {
            var go = new GameObject("Puerta " + demo.Escena);
            go.transform.SetParent(padre, false);
            go.AddComponent<BoxCollider2D>().isTrigger = true;
            go.AddComponent<DoorHub>().idNivel = demo.Escena;
        }
    }

    // === HABILIDADES ===
    // Cada nivel arranca con las habilidades que le tocan para que el conjunto
    // se vea llenarse. Bloquear y desbloquear guardan en PlayerPrefs: se recuerda
    // lo que habia y se devuelve al salir, para no tocar el progreso del jugador.
    private void GuardarHabilidades()
    {
        var tipos = (AbilityType[])System.Enum.GetValues(typeof(AbilityType));
        habilidadesAlEntrar = new bool[tipos.Length];
        for (int i = 0; i < tipos.Length; i++)
            habilidadesAlEntrar[i] = AbilityManager.Instance != null && AbilityManager.Instance.IsUnlocked(tipos[i]);

        if (AbilityManager.Instance != null) AbilityManager.Instance.ResetAll();
    }

    // Al parar el Play dentro de la demo tambien hay que devolver el progreso
    private void OnApplicationQuit()
    {
        cerrando = true;
        RestaurarHabilidades();
    }

    private void RestaurarHabilidades()
    {
        if (habilidadesAlEntrar == null) return;

        var tipos = (AbilityType[])System.Enum.GetValues(typeof(AbilityType));
        for (int i = 0; i < tipos.Length; i++)
            SaveSystem.SetHabilidad(tipos[i], habilidadesAlEntrar[i]);

        // Con el juego cerrandose alcanza con lo guardado: tocar el manager lo resucitaria
        if (!cerrando && AbilityManager.Instance != null)
        {
            for (int i = 0; i < tipos.Length; i++)
                if (!habilidadesAlEntrar[i]) AbilityManager.Instance.Lock(tipos[i]);

            // Vuelve a cargar lo guardado: suma las del jugador sin mostrar avisos
            AbilityManager.Instance.LoadProgress();
        }
        habilidadesAlEntrar = null;
    }
}
