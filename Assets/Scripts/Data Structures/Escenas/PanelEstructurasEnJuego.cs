using System.Text;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Panel de depuracion que se enciende con F1 en cualquier escena del juego y
/// muestra en vivo las estructuras de datos de la materia que el juego usa, una
/// seccion por TP. Solo lee: no cambia nada del juego.
///
/// Se crea solo al cargar la primera escena (sin tocar ninguna escena) y sobrevive
/// a los cambios de nivel. En un build final no se crea.
/// </summary>
public class PanelEstructurasEnJuego : MonoBehaviour
{
    private const float Refresco = 0.25f;

    private Text texto;
    private GameObject panel;
    private float proximoRefresco;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Crear()
    {
        if (!Application.isEditor && !Debug.isDebugBuild) return;
        if (FindFirstObjectByType<PanelEstructurasEnJuego>() != null) return;

        var go = new GameObject(nameof(PanelEstructurasEnJuego));
        DontDestroyOnLoad(go);
        go.AddComponent<PanelEstructurasEnJuego>();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.F1))
        {
            if (panel == null) Construir();
            else panel.SetActive(!panel.activeSelf);

            proximoRefresco = 0f;
        }

        if (panel == null || !panel.activeSelf || Time.unscaledTime < proximoRefresco) return;

        proximoRefresco = Time.unscaledTime + Refresco;
        texto.text = Armar();
    }

    private void Construir()
    {
        // Sin GraphicRaycaster ni raycastTarget: el panel no tapa los clics del juego
        RectTransform raiz = UIRapida.CrearCanvas(transform, 1000, false);
        RectTransform fondo = UIRapida.Rect("Panel", raiz, new Color(0.03f, 0.04f, 0.06f, 0.82f));
        fondo.GetComponent<Image>().raycastTarget = false;
        UIRapida.Rellenar(fondo, 0.55f, 0.01f, 0.995f, 0.99f, 0);

        texto = UIRapida.EtiquetaLlena(fondo, UIRapida.Fuente(14), "", 14, 12);
        panel = raiz.gameObject;
    }

    private static string Armar()
    {
        var sb = new StringBuilder();
        sb.AppendLine("ESTRUCTURAS DE DATOS EN VIVO   (F1 cierra)");
        sb.AppendLine();

        // TP01 List
        var puertas = DoorHub.Registradas;
        sb.Append($"TP01 List · SimpleArrayList<DoorHub>: {puertas.Count} puertas");
        sb.AppendLine(puertas.Count == 0 ? "  (se llena en el Hub)" : "");
        sb.AppendLine();

        // TP02 Stack
        var grid = FindFirstObjectByType<GridManager>();
        sb.AppendLine("TP02 Stack · SimpleArrayStack<ShadowBlock>, Z deshace el de la cima");
        if (grid == null) sb.AppendLine("  (sin GridManager en esta escena)");
        else
        {
            ShadowBlock[] pila = grid.HistorialDeBloques;
            sb.AppendLine(pila.Length == 0 ? "  vacia" : $"  {pila.Length} apilados, cima primero:");

            for (int i = 0; i < pila.Length && i < 6; i++)
            {
                ShadowBlock b = pila[i];
                bool vivo = b != null && b.gameObject.activeInHierarchy;
                sb.AppendLine(b == null ? "    (destruido)" : $"    {b.idTipo} en {b.cellPos}{(vivo ? "" : "  - ya destruido")}");
            }

            if (pila.Length > 6) sb.AppendLine($"    ... y {pila.Length - 6} mas");
        }
        sb.AppendLine();

        // TP03 Queue
        var npcs = FindObjectsByType<NPCDemostrador>(FindObjectsSortMode.None);
        sb.AppendLine("TP03 Queue · SimpleLinkedQueue<DemostracionPaso> de los NPC");
        if (npcs.Length == 0) sb.AppendLine("  (sin NPC demostrador en esta escena)");
        foreach (NPCDemostrador npc in npcs)
        {
            DemostracionPaso[] pendientes = npc.PasosPendientes;
            sb.Append($"  {npc.name}: ");
            sb.AppendLine(pendientes.Length == 0 ? "sin pasos pendientes" : $"{pendientes.Length} pendientes: " + string.Join(", ", System.Array.ConvertAll(pendientes, p => p.tipo.ToString())));
        }
        sb.AppendLine();

        // TP04 Priority Queue
        var audio = AudioManager.Instance;
        var popups = FindFirstObjectByType<PopupManager>();
        sb.AppendLine("TP04 Priority Queue · sonidos y popups (se vacian cada frame)");
        sb.AppendLine(audio == null ? "  audio: sin AudioManager" : $"  FX encolados {audio.FxEncolados}, sonaron {audio.FxReproducidos}");
        sb.AppendLine(popups == null ? "  popups: sin PopupManager" : $"  popups: {popups.Postulaciones} postulaciones");
        sb.AppendLine();

        // TP05 Set
        var habilidades = AbilityManager.Instance;
        sb.AppendLine("TP05 Set · SimpleArraySet<AbilityType> de habilidades desbloqueadas");
        sb.AppendLine(habilidades == null ? "  (sin AbilityManager)" : "  [" + string.Join(", ", habilidades.GetUnlockedAbilities()) + "]");
        sb.AppendLine();

        // TP06 Dictionary
        var controlador = FindFirstObjectByType<PlayerAbilityController>();
        sb.AppendLine("TP06 Dictionary · habilidades del jugador y pool de bloques");
        sb.AppendLine(controlador == null ? "  habilidades: sin jugador en esta escena" : "  habilidades registradas: [" + string.Join(", ", controlador.TiposRegistrados) + "]");
        if (grid != null) sb.AppendLine($"  bloques guardados en el pool: {grid.BloquesEnReserva}");
        sb.AppendLine();

        // TP07 Sorting
        string ranking = RankingDeTiempos.Detalle();
        sb.AppendLine("TP07 Sorting · CollectionSort ordena tus mejores tiempos");
        sb.AppendLine(ranking.Length == 0 ? "  (sin tiempos todavia, o no estas en el Hub)" : ranking.TrimEnd());
        sb.AppendLine();

        // TP08 BST y TP09 AVL
        sb.AppendLine("TP08 BST / TP09 AVL · catalogo de bloques (la raiz a la izquierda)");
        sb.AppendLine(grid == null ? "  (sin GridManager)" : grid.Fabrica.DescribirCatalogo().TrimEnd());

        return sb.ToString();
    }
}
