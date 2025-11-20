using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(BoxCollider2D))]
public class MirrorBlock : ShadowBlock
{
    [Header("Luz Emisora")]
    public GameObject prefabLuzReflectiva;
    public Vector2 direccionInicial = Vector2.right;
    public float alcance = 6f;
    public LayerMask mascaraBloqueos;

    [Header("Sprites")]
    public Sprite spriteActivo;
    public Sprite spriteNormal;

    // --- Nueva lógica de encendido/apagado ---
    private bool recibiendoLuz = false;
    private float tiempoSinLuz = 0f;
    public float tiempoApagado = 0.1f;

    private GameObject luzInstancia;
    private ReflectiveLightEmitter emisor;
    private Vector2 direccionActual;
    public Vector2 DireccionActual => direccionActual;

    protected override void Start()
    {
        base.Start();
        direccionActual = direccionInicial;
    }

    void Update()
    {
        // Si en este frame NO recibió luz → contar tiempo
        if (!recibiendoLuz)
        {
            tiempoSinLuz += Time.deltaTime;

            if (tiempoSinLuz >= tiempoApagado)
                ApagarLuzReflejada();
        }

        // Reset para el próximo frame (si recibe luz, RecibirLuz() lo marcará)
        recibiendoLuz = false;
    }

    // --- RECEPCIÓN DE LUZ (SpotLightDetector) ---
    public override void RecibirLuz(float daño, SpotLightDetector.TipoLuz tipo)
    {
        recibiendoLuz = true;
        tiempoSinLuz = 0f;

        base.RecibirLuz(daño, tipo);

        if (vidaActual > 0f && tipo != SpotLightDetector.TipoLuz.Roja)
            ActivarLuzReflejada(tipo);
    }

    // --- RECEPCIÓN DE LUZ REFLEJADA (ReflectiveLightEmitter) ---
    public void RecibirLuz(Vector2 dirLuz, float daño, SpotLightDetector.TipoLuz tipo, Vector2 normal, float alcanceOriginal, Vector2 puntoImpacto)
    {
        recibiendoLuz = true;
        tiempoSinLuz = 0f;

        RecibirLuz(daño, tipo);

        if (vidaActual > 0f && tipo != SpotLightDetector.TipoLuz.Roja)
            ActivarLuzReflejada(tipo);
    }

    // --- ENCIENDE O CREA LA LUZ REFLECTIVA ---
    private void ActivarLuzReflejada(SpotLightDetector.TipoLuz tipo)
    {
        if (luzInstancia == null)
        {
            luzInstancia = Instantiate(prefabLuzReflectiva, transform.position, Quaternion.identity, transform);
            emisor = luzInstancia.GetComponent<ReflectiveLightEmitter>();

            if (emisor == null)
            {
                Debug.LogError($"MirrorBlock '{name}': el prefab '{prefabLuzReflectiva.name}' no tiene ReflectiveLightEmitter.");
                Destroy(luzInstancia);
                return;
            }

            emisor.SetTipoLuz(tipo);
            emisor.SetDireccion(direccionActual);
            emisor.SetParametros(alcance, 0.25f);
            emisor.mascaraBloqueos = (mascaraBloqueos.value == 0) ? ~0 : mascaraBloqueos;
        }

        if (spriteActivo != null)
        {
            SpriteRenderer sr = GetComponent<SpriteRenderer>();
            if (sr != null) sr.sprite = spriteActivo;
        }
    }

    // --- APAGA LA LUZ CUANDO DEJA DE RECIBIR ---
    private void ApagarLuzReflejada()
    {
        if (luzInstancia != null)
        {
            Destroy(luzInstancia);
            luzInstancia = null;
            emisor = null;

            if (spriteNormal != null)
            {
                SpriteRenderer sr = GetComponent<SpriteRenderer>();
                if (sr != null) sr.sprite = spriteNormal;
            }
        }
    }

    // --- ROTAR HAZ (clic derecho) ---
    public void RotarHaz()
    {
        direccionActual = new Vector2(direccionActual.y, -direccionActual.x);
        direccionInicial = direccionActual;

        Debug.Log($"🔁 MirrorBlock cambió dirección del haz a {direccionActual}");

        if (emisor != null)
            emisor.SetDireccion(direccionActual);
    }

    // --- NPC Demostrador: setear dirección manual ---
    public void SetDireccionInicial(Vector2 dir)
    {
        if (dir == Vector2.zero) return;

        direccionInicial = dir.normalized;
        direccionActual = direccionInicial;

        if (emisor != null)
            emisor.SetDireccion(direccionActual);

        Debug.Log($"🔁 MirrorBlock ajustó dirección inicial a {direccionActual}");
    }

    public override void DestruirBloque()
    {
        if (luzInstancia != null)
            Destroy(luzInstancia);

        base.DestruirBloque();
    }
}
