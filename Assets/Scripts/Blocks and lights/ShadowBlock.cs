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

    // 🔸 Marca de tiempo para evitar liberar celdas si se destruye instantáneamente
    private float tiempoCreacion;

    // === Ciclo de vida ===
    protected virtual void Start()
    {
        tiempoCreacion = Time.time; // guarda el momento en que se creó el bloque
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

    void Update()
    {
        if (bajoLuz && barraInstanciada != null)
            barraInstanciada.SetVida(vidaActual, vidaBajoLuz);

        ActualizarSpritePorVida();
    }

    // === Recibir luz (daño normal) ===
    public virtual void RecibirLuz(float daño)
    {
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
    public virtual void RecibirLuz(float daño, SpotLightDetector.TipoLuz tipo)
    {
        if (tipo == SpotLightDetector.TipoLuz.Roja)
        {
            // 🔥 Luz roja inflige muchísimo más daño
            float dañoAmplificado = daño * 10f;
            RecibirLuz(dañoAmplificado);
            return;
        }

        // Luz amarilla → daño normal
        RecibirLuz(daño);
    }

    // === Actualizar sprite según nivel de daño ===
    private void ActualizarSpritePorVida()
    {
        if (spriteRenderer == null || spritesDaño == null || spritesDaño.Length == 0)
            return;

        float porcentajeVida = vidaActual / vidaBajoLuz;
        int index = Mathf.Clamp(Mathf.FloorToInt((1f - porcentajeVida) * spritesDaño.Length), 0, spritesDaño.Length - 1);

        Sprite spriteActual = (porcentajeVida >= 1f) ? spriteOriginal : spritesDaño[index];
        spriteRenderer.sprite = spriteActual;
    }

    // === Cuando sale de la luz ===
    public virtual void SalirDeLuz()
    {
        bajoLuz = false;
    }

    // === Destrucción controlada ===
    public virtual void DestruirBloque()
    {
        // ⚙️ Evita liberar la celda si el bloque se destruye en el mismo frame que se creó
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
