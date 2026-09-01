using UnityEngine;

public class ShadowBlock : MonoBehaviour
{
    [Header("Vida")]
    public float vidaBajoLuz = 5f;
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

    public static event System.Action<ShadowBlock> OnBloqueDestruido;

    // Marca de tiempo para evitar liberar celdas si se destruye instantáneamente
    private float tiempoCreacion;

    protected virtual void Awake()
    {
        tiempoCreacion = Time.time;
    }
    // === Ciclo de vida ===
    protected virtual void Start()
    {
        vidaActual = vidaBajoLuz;
        spriteRenderer = GetComponent<SpriteRenderer>();

        if (spriteOriginal == null && spriteRenderer != null)
            spriteOriginal = spriteRenderer.sprite;

        if (barraPrefab != null)
        {
            GameObject barra = Instantiate(barraPrefab, transform);
            barra.transform.localPosition = new Vector3(0, 0.6f, 0);
            barraInstanciada = barra.GetComponent<LifeBar>();
            barraInstanciada?.SetVida(vidaActual, vidaBajoLuz);
            barraInstanciada.gameObject.SetActive(false); // invisible hasta recibir daño
        }
    }

    protected virtual void Update()
    {
        if (bajoLuz && barraInstanciada != null)
            barraInstanciada.SetVida(vidaActual, vidaBajoLuz);

        ActualizarSpritePorVida();
    }

    // === Recibir luz (daño normal) ===
    public virtual void RecibirLuz(float daño)
    {
        // Protección: NO recibir daño en los primeros ms de vida
        if (Time.time - tiempoCreacion < 0.1f)
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

        float porcentajeVida = vidaActual / vidaBajoLuz;

        int total = spritesDaño.Length;
        int index;

        
        if (porcentajeVida <= 0.15f)
        {
            // cuánta vida le queda en la zona final
            float t = Mathf.InverseLerp(0.10f, 0f, porcentajeVida); // 1 → 0

            // velocidad del final (entre 2x y 4x más rápido)
            float velocidadFinal = 3f;

            int inicioExpl = Mathf.FloorToInt((total - 1) * 0.90f);  // sprites desde el 90% al 100%

            int cantidadFinales = total - inicioExpl;

            int idxFinal = inicioExpl + Mathf.FloorToInt((1f - t) * cantidadFinales * velocidadFinal);

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
        // Evita liberar la celda si el bloque se destruye en el mismo frame que se creó
        if (Time.time - tiempoCreacion > 0.05f)
        {
            if (gridManager != null)
                gridManager.LiberarCelda(cellPos);
        }

        hudHabilidad?.RecuperarCargas();
        OnBloqueDestruido?.Invoke(this);

        Destroy(gameObject);
    }
}
