using System.Collections.Generic;
using UnityEngine;

public class ShadowBlock : MonoBehaviour
{
    /// <summary>Id del bloque de sombra comun, el que trae el prefab base.</summary>
    public const string IdSombra = "sombra";

    /// <summary>Id del bloque espejo.</summary>
    public const string IdEspejo = "espejo";

    [Header("Identidad")]
    [Tooltip("Nombre con el que la fabrica conoce a este tipo de bloque. Tiene que ser distinto en cada prefab.")]
    public string idTipo = IdSombra;

    [Tooltip("Cuantas cargas de habilidad cuesta colocarlo.")]
    public int costoCargas = 1;

    [Header("Ajustes")]
    [Tooltip("Ancho de sombra y vida. Estan en un asset para poder afinarlos en play: el cambio " +
             "lo ven todos los bloques de este tipo y queda guardado.")]
    public AjustesDeBloque ajustes;

    // Sin asset se usan los valores de siempre, y Awake avisa del olvido.
    public float AnchoSombra => ajustes != null ? ajustes.anchoSombra : 1f;
    public float VidaBajoLuz => ajustes != null ? ajustes.vidaBajoLuz : 5f;

    /// <summary>
    /// Los bloques que estan en juego. Las luces los recorren para calcular su
    /// sombra sin buscarlos en la escena cada frame. Un bloque guardado en el
    /// pool se apaga y sale de la lista.
    /// </summary>
    public static IReadOnlyList<ShadowBlock> Activos => activos;
    private static readonly List<ShadowBlock> activos = new();

    public Collider2D Colisionador { get; private set; }

    protected float vidaActual;
    protected bool bajoLuz = false;

    [Header("UI")]
    public GameObject barraPrefab;
    protected LifeBar barraInstanciada;

    [Header("Sprites de daño")]
    public Sprite spriteOriginal;                 // sprite sin daño
    public Sprite[] spritesDaño;                  // 8 sprites, de leve → severo daño
    private SpriteRenderer spriteRenderer;

    [HideInInspector] public GridManager gridManager;
    [HideInInspector] public Vector3Int cellPos;
    [HideInInspector] public HUDHabilidad hudHabilidad;

    /// <summary>Pool que entregó este bloque, o null si se creó suelto.</summary>
    [HideInInspector] public PoolDeBloques pool;

    /// <summary>Prefab del que salió, para saber a qué fila del pool vuelve.</summary>
    [HideInInspector] public GameObject prefabDeOrigen;

    // Recien colocado no recibe daño ni libera su celda: la luz que ya
    // estaba ahi lo romperia en el mismo frame.
    private const float ProteccionAlNacer = 0.1f;
    private const float MinimoParaLiberarCelda = 0.05f;
    private float tiempoCreacion;

    private bool inicializado;

    protected virtual void Awake()
    {
        tiempoCreacion = Time.time;
        Colisionador = GetComponent<Collider2D>();

        if (ajustes == null)
            Debug.LogError($"[ShadowBlock] '{name}' no tiene asset de ajustes: usa ancho 1 y vida 5.", this);
    }

    protected virtual void OnEnable() => activos.Add(this);
    protected virtual void OnDisable() => activos.Remove(this);

    // === Ciclo de vida ===
    protected virtual void Start()
    {
        Inicializar();
        Reiniciar();
    }

    /// <summary>
    /// Lo que se arma una sola vez en la vida del objeto: cachear componentes y
    /// crear la barra de vida. Separado de Reiniciar porque el bloque se recicla
    /// desde el pool, y entonces Start no vuelve a correr.
    /// </summary>
    protected virtual void Inicializar()
    {
        if (inicializado) return;
        inicializado = true;

        spriteRenderer = GetComponent<SpriteRenderer>();

        if (spriteOriginal == null && spriteRenderer != null)
            spriteOriginal = spriteRenderer.sprite;

        if (barraPrefab != null)
        {
            GameObject barra = Instantiate(barraPrefab, transform);
            barra.transform.localPosition = new Vector3(0, 0.6f, 0);
            barraInstanciada = barra.GetComponent<LifeBar>();
        }
    }

    /// <summary>
    /// Deja el bloque como recién colocado. Lo llama el pool antes de volver a
    /// entregarlo, porque el objeto reciclado conserva el estado del uso anterior.
    /// </summary>
    public virtual void Reiniciar()
    {
        Inicializar();

        tiempoCreacion = Time.time;
        vidaActual = VidaBajoLuz;
        bajoLuz = false;

        if (spriteRenderer != null && spriteOriginal != null)
            spriteRenderer.sprite = spriteOriginal;

        if (barraInstanciada != null)
        {
            barraInstanciada.SetVida(vidaActual, VidaBajoLuz);
            barraInstanciada.gameObject.SetActive(false); // invisible hasta recibir daño
        }
    }

    protected virtual void Update()
    {
        if (bajoLuz && barraInstanciada != null)
            barraInstanciada.SetVida(vidaActual, VidaBajoLuz);

        ActualizarSpritePorVida();
    }

    // === Recibir luz (daño normal) ===
    public virtual void RecibirLuz(float daño)
    {
        if (Time.time - tiempoCreacion < ProteccionAlNacer)
            return;

        if (!bajoLuz && barraInstanciada != null)
            barraInstanciada.gameObject.SetActive(true);

        bajoLuz = true;
        vidaActual -= daño;
        vidaActual = Mathf.Max(vidaActual, 0f);

        ActualizarSpritePorVida();

        if (vidaActual <= 0f)
            DestruirBloque();
    }

    // === Recibir luz (con tipo de luz) ===
    public virtual void RecibirLuz(float daño, TipoLuz tipo)
    {
        // Luz roja → daño multiplicado
        if (tipo == TipoLuz.Roja)
        {
            float dañoAmplificado = daño * 10f;
            RecibirLuz(dañoAmplificado);
            return;
        }

        // Luz normal
        RecibirLuz(daño);
    }

    // === Actualizar sprite según nivel de daño ===
    private void ActualizarSpritePorVida()
    {
        if (spriteRenderer == null || spritesDaño == null || spritesDaño.Length == 0)
            return;

        float porcentajeVida = vidaActual / VidaBajoLuz;

        int total = spritesDaño.Length;
        int index;

        
        if (porcentajeVida <= 0.15f)
        {
            // Cuanto avanzo en la zona final: 0 con un 10 % de vida, 1 al morir.
            float t = Mathf.InverseLerp(0.10f, 0f, porcentajeVida);

            // Los ultimos sprites pasan 3 veces mas rapido, como un estallido.
            const float velocidadFinal = 3f;

            int inicioExpl = Mathf.FloorToInt((total - 1) * 0.90f);  // sprites desde el 90% al 100%

            int cantidadFinales = total - inicioExpl;

            // Antes usaba (1 - t): el bloque mostraba el sprite mas roto y se
            // "arreglaba" a medida que se moria.
            int idxFinal = inicioExpl + Mathf.FloorToInt(t * cantidadFinales * velocidadFinal);

            index = Mathf.Clamp(idxFinal, inicioExpl, total - 1);
        }
        else
        {
            // VIDA NORMAL (100% → 10%): todos los sprites progresivos
            float t = Mathf.InverseLerp(1f, 0.10f, porcentajeVida); // 0 → 1
            index = Mathf.FloorToInt(t * (total * 0.90f));          // hasta el 90%
            index = Mathf.Clamp(index, 0, total - 1);
        }

        spriteRenderer.sprite = spritesDaño[index];
    }


    // === Cuando sale de la luz ===
    public virtual void SalirDeLuz()
    {
        bajoLuz = false;
    }

    // === Destrucción controlada ===
    public virtual void DestruirBloque()
    {
        if (gridManager != null)
        {
            if (Time.time - tiempoCreacion > MinimoParaLiberarCelda)
                gridManager.LiberarCelda(cellPos);
            gridManager.OlvidarBloque(gameObject);
        }

        hudHabilidad?.RecuperarCargas();
        Guardar();
    }

    /// <summary>
    /// Saca el bloque de juego. Si vino de un pool vuelve ahí para reusarse; si
    /// no, se destruye como antes. Lo segundo cubre los bloques que algún NPC o
    /// una escena dejan puestos sin pasar por el GridManager.
    /// </summary>
    protected void Guardar()
    {
        if (pool != null)
            pool.Devolver(this);
        else
            Destroy(gameObject);
    }
}
