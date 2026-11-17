using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Un nivel de demostracion: que escena es, que TP muestra y que controles extra tiene.</summary>
public class Demo
{
    public string Escena;
    public string Titulo;
    public string Descripcion;
    public string Controles;
    public int[] Tps;
    public bool ColocaBloques;
    public bool Habilidades;
    public bool Ranking;
}

/// <summary>
/// Hace jugables las demos de la materia. Vive en DontDestroyOnLoad (se crea
/// solo, como el panel F1): al cargar una escena de la lista la reconoce, abre el
/// panel con los TP de ese nivel y le da los controles de demostracion. F2
/// vuelve al indice desde cualquier demo.
/// </summary>
public class Demostracion : MonoBehaviour
{
    public static readonly Demo[] Todas =
    {
        new Demo
        {
            Escena = "Demo_Bloques",
            Titulo = "Bloques de sombra",
            Descripcion = "Los bloques se guardan en una pila (se deshacen de a uno), se reciclan en un pool y se arman desde un catalogo ordenado.",
            Controles = "Clic izq: poner bloque | Clic der o Z: deshacer | 1: sombra, 2: espejo",
            Tps = new[] { 2, 6, 8, 9 }, ColocaBloques = true,
        },
        new Demo
        {
            Escena = "Nivel1",
            Titulo = "Noxel, el NPC que demuestra",
            Descripcion = "Noxel ejecuta su demostracion paso a paso: los pasos esperan en una cola y salen en el orden en que se cargaron.",
            Controles = "Habla con Noxel y mira la cola en el panel",
            Tps = new[] { 3 },
        },
        new Demo
        {
            Escena = "Demo_Habilidades",
            Titulo = "Habilidades, sonidos y avisos",
            Descripcion = "Las habilidades desbloqueadas son un conjunto sin repetidos; los avisos y los sonidos esperan en colas de prioridad.",
            Controles = "U: desbloquear la siguiente habilidad | K: rafaga de sonidos de distinta prioridad",
            Tps = new[] { 4, 5, 6 }, Habilidades = true,
        },
        new Demo
        {
            Escena = "Demo_Ranking",
            Titulo = "Ranking de tiempos",
            Descripcion = "Las puertas viven en una lista y el ranking las ordena por tiempo con CollectionSort.",
            Controles = "R: tiempos al azar en 6 niveles de prueba | C: borrarlos",
            Tps = new[] { 1, 7 }, Ranking = true,
        },
        new Demo
        {
            Escena = "Demo_Perseguidor",
            Titulo = "El perseguidor",
            Descripcion = "La grilla es un grafo y Dijkstra busca el camino mas corto. El NPC te sigue y rodea los bloques que pongas.",
            Controles = "Clic izq: poner bloque | Clic der o Z: deshacer | cian = ruta (Gizmos en Game)",
            Tps = new[] { 10 }, ColocaBloques = true,
        },
    };

    public static Demo Actual { get; private set; }

    private bool[] habilidadesAlEntrar;
    private string idBloque = ShadowBlock.IdSombra;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Crear()
    {
        if (FindFirstObjectByType<Demostracion>() != null) return;

        var go = new GameObject(nameof(Demostracion));
        DontDestroyOnLoad(go);
        var demostracion = go.AddComponent<Demostracion>();

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

    private bool arrancaDirecto;

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

        PanelEstructurasEnJuego.Mostrar(Actual);
        if (Actual == null) return;

        if (Actual.ColocaBloques) StartCoroutine(DesbloquearCeldas());
        if (Actual.Habilidades) GuardarHabilidades();
        if (Actual.Ranking) CrearPuertasDeRanking();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.F2) && SceneManager.GetActiveScene().name != Escenas.Indice)
        {
            SceneManager.LoadScene(Escenas.Indice);
            return;
        }

        if (Actual == null) return;
        if (Actual.ColocaBloques) ControlarBloques();
        if (Actual.Habilidades) ControlarHabilidades();
        if (Actual.Ranking) ControlarRanking();
    }

    // === BLOQUES ===
    private IEnumerator DesbloquearCeldas()
    {
        // GridManager arma sus celdas en su propio Start.
        yield return null;
        yield return null;

        var grid = FindFirstObjectByType<GridManager>();
        if (grid == null) yield break;

        var suelo = grid.sueloTilemap;
        suelo.CompressBounds();
        foreach (var pos in suelo.cellBounds.allPositionsWithin)
            if (suelo.HasTile(pos)) grid.CorromperCeldaUnica(pos);
    }

    private void ControlarBloques()
    {
        var grid = FindFirstObjectByType<GridManager>();
        var jugador = FindFirstObjectByType<Jugador>();
        if (grid == null || jugador == null || Camera.main == null) return;

        if (Input.GetKeyDown(KeyCode.Alpha1)) idBloque = ShadowBlock.IdSombra;
        if (Input.GetKeyDown(KeyCode.Alpha2)) idBloque = ShadowBlock.IdEspejo;

        if (Input.GetMouseButtonDown(0))
        {
            Vector3 mundo = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            mundo.z = 0f;
            grid.IntentarColocarBloque(mundo, idBloque, jugador.transform.position, float.MaxValue);
        }

        // Z ya lo maneja PlayerAbilityController si el jugador tiene la habilidad
        if (Input.GetMouseButtonDown(1)) grid.DeshacerUltimoBloque();
    }

    // === HABILIDADES ===
    // La demo arranca con el conjunto vacio para que se vea crecer. Bloquear y
    // desbloquear guardan en PlayerPrefs: se recuerda lo que habia y se devuelve
    // al salir, para no tocar el progreso del jugador.
    private void GuardarHabilidades()
    {
        var tipos = (AbilityType[])System.Enum.GetValues(typeof(AbilityType));
        habilidadesAlEntrar = new bool[tipos.Length];
        for (int i = 0; i < tipos.Length; i++)
            habilidadesAlEntrar[i] = AbilityManager.Instance != null && AbilityManager.Instance.IsUnlocked(tipos[i]);

        if (AbilityManager.Instance != null) AbilityManager.Instance.ResetAll();
    }

    private bool cerrando;

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

    private void ControlarHabilidades()
    {
        var habilidades = AbilityManager.Instance;
        if (habilidades == null) return;

        if (Input.GetKeyDown(KeyCode.U))
        {
            foreach (AbilityType tipo in System.Enum.GetValues(typeof(AbilityType)))
            {
                // AbilityMode no se desbloquea de verdad: solo muestra el aviso
                if (tipo == AbilityType.AbilityMode || habilidades.IsUnlocked(tipo)) continue;
                habilidades.Unlock(tipo);
                break;
            }
        }

        var audio = AudioManager.Instance;
        if (audio != null && Input.GetKeyDown(KeyCode.K))
        {
            // Todo en el mismo frame: la cola deja sonar primero lo de prioridad alta
            for (int i = 0; i < 3; i++) audio.ReproducirBloque();
            for (int i = 0; i < 2; i++) audio.ReproducirTeleport();
            for (int i = 0; i < 2; i++) audio.ReproducirHabilidad();
            audio.ReproducirMuerte();
        }
    }

    // === RANKING ===
    private const string IdPrefijoDemo = "Demo_Nivel";
    private const int PuertasDeRanking = 6;

    private void CrearPuertasDeRanking()
    {
        // Puertas de mentira con ids propios: el ranking real no se toca
        for (int i = 1; i <= PuertasDeRanking; i++)
        {
            var go = new GameObject("PuertaDemo" + i);
            go.AddComponent<BoxCollider2D>().isTrigger = true;
            go.AddComponent<DoorHub>().idNivel = IdPrefijoDemo + i;
        }
    }

    private void ControlarRanking()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            for (int i = 1; i <= PuertasDeRanking; i++)
            {
                string id = IdPrefijoDemo + i;
                SaveSystem.BorrarProgresoNivel(id);
                SaveSystem.GuardarResultado(id, 1, Random.Range(20f, 300f), 0, 0);
            }
        }

        if (Input.GetKeyDown(KeyCode.C))
            for (int i = 1; i <= PuertasDeRanking; i++) SaveSystem.BorrarProgresoNivel(IdPrefijoDemo + i);
    }
}
