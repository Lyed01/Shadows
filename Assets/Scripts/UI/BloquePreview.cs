using UnityEngine;

/// <summary>
/// Marca la celda bajo el mouse con la habilidad elegida: fuerte en el modo
/// habilidad y tenue mientras se camina.
/// Pide la celda y su validez al GridManager, con las mismas funciones que usan
/// las habilidades: lo que marca es lo que va a pasar al hacer clic.
/// </summary>
public class BloquePreview : MonoBehaviour
{
    [Header("Referencias")]
    public SpriteRenderer spriteRenderer; // Sprite del contorno (animado o estático)
    public float zOffset = -0.1f;         // Para que no quede detrás del tilemap

    [Tooltip("Color del contorno cuando en esa celda no se puede usar la habilidad.")]
    public Color colorInvalida = new Color(1f, 0.3f, 0.3f, 1f);
    [Tooltip("Opacidad del contorno fuera del modo habilidad, mientras se camina.")]
    [Range(0f, 1f)] public float alfaCaminando = 0.5f;

    private Color colorValida;
    private Jugador jugador;

    void Awake()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        colorValida = spriteRenderer.color;

        // Arranca oculto
        spriteRenderer.enabled = false;
    }

    void Update()
    {
        // Al morir el jugador se instancia otro: se vuelve a buscar.
        if (jugador == null)
            jugador = FindFirstObjectByType<Jugador>();

        // Tambien caminando, mas tenue: asi se ve si la celda esta a alcance
        // antes de frenarse. La AbyssFlame no apunta a una celda.
        HabilidadSO habilidad = AbilitySelector.Instance?.GetHabilidadActual();
        bool enModo = Jugador.ModoHabilidadActivo;
        bool caminando = jugador != null && jugador.ControlActivo && !jugador.InputBloqueado;
        GridManager grid = jugador != null ? jugador.gridManager : null;
        if (grid == null || habilidad == null || habilidad.tipo == AbilityType.AbyssFlame || !(enModo || caminando))
        {
            spriteRenderer.enabled = false;
            return;
        }

        Vector3Int celda = grid.CeldaBajoMouse();
        if (!grid.sueloTilemap.HasTile(celda))
        {
            spriteRenderer.enabled = false;
            return;
        }

        transform.position = grid.sueloTilemap.GetCellCenterWorld(celda) + new Vector3(0, 0, zOffset);

        // Se evalúa cada frame y no solo al cambiar de celda: la luz puede
        // liberar una celda ocupada sin que el mouse se mueva.
        bool valida = grid.EvaluarCelda(celda, jugador.transform.position, jugador.rangoHabilidad)
                      == ResultadoColocacion.Exito;
        Color color = valida ? colorValida : colorInvalida;
        if (!enModo) color.a *= alfaCaminando;
        spriteRenderer.color = color;
        spriteRenderer.enabled = true;
    }
}
