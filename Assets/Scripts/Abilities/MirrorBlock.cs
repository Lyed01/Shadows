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

    private SpriteRenderer sr;

    // --- Lógica de encendido/apagado ---
    private bool recibiendoLuz = false;
    private float tiempoSinLuz = 0f;
    public float tiempoApagado = 0.1f;

    private GameObject luzInstancia;
    private ReflectiveLightEmitter emisor;

    private Vector2 direccionActual;
    public Vector2 DireccionActual => direccionActual;

    // ============================
    // START
    // ============================
    protected override void Start()
    {
        base.Start();

        sr = GetComponent<SpriteRenderer>();

        direccionActual = direccionInicial;

        if (spriteNormal != null)
            sr.sprite = spriteNormal;
    }

    // ============================
    // UPDATE (control de apagado + sprites)
    // ============================
    void Update()
    {
        if (!recibiendoLuz)
        {
            tiempoSinLuz += Time.deltaTime;

            if (tiempoSinLuz >= tiempoApagado)
            {
                ApagarLuzReflejada();

                // ⛔ NO forzar sprite si está dañado
                if (!EstaDaniado() && spriteNormal != null)
                    sr.sprite = spriteNormal;
            }
        }

        recibiendoLuz = false;
    }

    // ============================
    // LUZ DIRECTA DEL SPOTLIGHT
    // ============================
    public override void RecibirLuz(float daño, SpotLightDetector.TipoLuz tipo)
    {
        recibiendoLuz = true;
        tiempoSinLuz = 0f;

        // ⭐ Daño original del ShadowBlock (incluye protección anti-nacimiento)
        base.RecibirLuz(daño, tipo);

        // Cambia sprite según estado (si no está dañado)
        if (!EstaDaniado() && spriteActivo != null)
            sr.sprite = spriteActivo;

        if (vidaActual > 0f && tipo != SpotLightDetector.TipoLuz.Roja)
            ActivarLuzReflejada(tipo);
    }



    // ============================
    // LUZ REFLEJADA DEL EMITTER
    // ============================
    public void RecibirLuz(Vector2 dirLuz, float daño, SpotLightDetector.TipoLuz tipo,
                       Vector2 normal, float alcanceOriginal, Vector2 puntoImpacto)
    {
        recibiendoLuz = true;
        tiempoSinLuz = 0f;

        // ⭐ Usa la misma lógica de ShadowBlock (incluye protección anti-instant-kill)
        base.RecibirLuz(daño, tipo);

        if (vidaActual > 0f && tipo != SpotLightDetector.TipoLuz.Roja)
            ActivarLuzReflejada(tipo);
    }



    // ============================
    // ENCENDER EMITTER
    // ============================
    private void ActivarLuzReflejada(SpotLightDetector.TipoLuz tipo)
    {
        if (luzInstancia == null)
        {
            luzInstancia = Instantiate(prefabLuzReflectiva, transform.position, Quaternion.identity, transform);
            emisor = luzInstancia.GetComponent<ReflectiveLightEmitter>();

            if (emisor == null)
            {
                Debug.LogError($"MirrorBlock '{name}': prefab '{prefabLuzReflectiva.name}' sin ReflectiveLightEmitter.");
                Destroy(luzInstancia);
                return;
            }

            // Configuración del emisor
            emisor.SetTipoLuz(tipo);
            emisor.SetDireccion(direccionActual);
            emisor.SetParametros(alcance, 0.25f);
            emisor.mascaraBloqueos = (mascaraBloqueos.value == 0) ? ~0 : mascaraBloqueos;
        }
        else
        {
            // Por si cambia mientras está encendido
            emisor.SetTipoLuz(tipo);
            emisor.SetDireccion(direccionActual);
        }
    }

    // ============================
    // APAGAR EMITTER
    // ============================
    private void ApagarLuzReflejada()
    {
        if (luzInstancia != null)
        {
            Destroy(luzInstancia);
            luzInstancia = null;
            emisor = null;
        }
    }

    // ============================
    // ROTAR HAZ
    // ============================
    public void RotarHaz()
    {
        direccionActual = new Vector2(direccionActual.y, -direccionActual.x);
        direccionInicial = direccionActual;

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

    // ============================
    // DESTRUIR BLOQUE
    // ============================
    public override void DestruirBloque()
    {
        if (luzInstancia != null)
            Destroy(luzInstancia);

        base.DestruirBloque();
    }

    private bool EstaDaniado()
    {
        return vidaActual < vidaBajoLuz && vidaActual > 0f;
    }


}
