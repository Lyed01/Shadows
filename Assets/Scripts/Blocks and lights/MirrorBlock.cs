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

    private GameObject luzInstancia;
    private ReflectiveLightEmitter emisor;
    private Vector2 direccionActual;
    public Vector2 DireccionActual => direccionActual; // (opcional) solo lectura

    protected override void Start()
    {
        base.Start();
        direccionActual = direccionInicial;
    }

    public override void RecibirLuz(float daño, SpotLightDetector.TipoLuz tipo)
    {
        base.RecibirLuz(daño, tipo);
        if (vidaActual > 0f && tipo != SpotLightDetector.TipoLuz.Roja)
            ActivarLuzReflejada(tipo);
    }

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

    public void RotarHaz()
    {
        direccionActual = new Vector2(direccionActual.y, -direccionActual.x);
        direccionInicial = direccionActual;

        Debug.Log($"🔁 MirrorBlock cambió dirección del haz a {direccionActual}");

        if (emisor != null)
            emisor.SetDireccion(direccionActual);
    }

    public override void DestruirBloque()
    {
        if (luzInstancia != null)
            Destroy(luzInstancia);
        base.DestruirBloque();
    }

    public void RecibirLuz(Vector2 dirLuz, float daño, SpotLightDetector.TipoLuz tipo, Vector2 normal, float alcanceOriginal, Vector2 puntoImpacto)
    {
        RecibirLuz(daño, tipo);

        if (vidaActual > 0f && tipo != SpotLightDetector.TipoLuz.Roja)
            ActivarLuzReflejada(tipo);
    }
    // --- NUEVO MÉTODO PARA NPCDemostrADOR ---
    public void SetDireccionInicial(Vector2 dir)
    {
        if (dir == Vector2.zero) return;

        direccionInicial = dir.normalized;

        // si direcciónActual es privada, asegurate de tenerla declarada así arriba:
        // private Vector2 direccionActual;
        direccionActual = direccionInicial;

        // Si ya tiene emisor, actualizarlo inmediatamente
        if (emisor != null)
            emisor.SetDireccion(direccionActual);

        Debug.Log($"🔁 MirrorBlock ajustó dirección inicial a {direccionActual}");
    }

}
