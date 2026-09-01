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
    [Header("Indicador de dirección")]
    public Sprite spriteFlecha;
    private GameObject flechaGO;
    private SpriteRenderer flechaSR;


    // --- Lógica de encendido/apagado ---
    private bool recibiendoLuz = false;
    private float tiempoSinLuz = 0f;
    public float tiempoApagado = 0.1f;

    private GameObject luzInstancia;
    private ReflectiveLightEmitter emisor;

    private Vector2 direccionActual;
    public Vector2 DireccionActual => direccionActual;

    // START
    protected override void Start()
    {
        base.Start();

        sr = GetComponent<SpriteRenderer>();

        direccionActual = direccionInicial;

        if (spriteNormal != null)
            sr.sprite = spriteNormal;

        CrearFlechaDireccion(); //  AÑADIR ESTO
    }


    // UPDATE (control de apagado + sprites)
    protected override void Update()
    {
        base.Update();

        if (!recibiendoLuz)
        {
            tiempoSinLuz += Time.deltaTime;

            if (tiempoSinLuz >= tiempoApagado)
                ApagarLuzReflejada();
        }

        // base.Update() reescribe el sprite en cada frame con el del nivel de
        // daño. Mientras el bloque este intacto manda el estado de iluminacion;
        // en cuanto recibe daño, manda el sprite de daño.
        if (!EstaDaniado())
        {
            Sprite deseado = recibiendoLuz ? spriteActivo : spriteNormal;
            if (deseado != null)
                sr.sprite = deseado;
        }

        recibiendoLuz = false;
    }
    // FLECHA DE DIRECCIÓN
    private void CrearFlechaDireccion()
    {
        // Crear objeto hijo
        flechaGO = new GameObject("IndicadorFlecha");
        flechaGO.transform.SetParent(transform);

        flechaSR = flechaGO.AddComponent<SpriteRenderer>();
        flechaSR.sprite = spriteFlecha;
        flechaSR.sortingOrder = sr.sortingOrder + 1; // se dibuja arriba del bloque

        ActualizarFlecha();
    }


    private void ActualizarFlecha()
    {
        if (flechaGO == null || flechaSR == null || sr == null) return;

        Vector2 dir = direccionActual.normalized;

        // 1. Tamaño del bloque
        Vector2 ext = sr.bounds.extents;

        // 2. Distancia extra para separarlo unos píxeles
        float separacion = 0.20f; // ≈ 1 a 2 píxeles, ajustable

        // 3. Calcular posición final = borde + separación
        Vector3 offset = new Vector3(
            dir.x * (ext.x + separacion),
            dir.y * (ext.y + separacion),
            0f
        );

        flechaGO.transform.localPosition = offset;

        // 4. Rotar flecha sabiendo que el sprite original apunta HACIA ARRIBA (0,1)
        // Convertimos dir a un ángulo y lo usamos directamente
        float angulo = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

        // El sprite apunta hacia arriba  Vector2.up = 90°
        // Así que corregimos restando 90°
        flechaGO.transform.localRotation = Quaternion.Euler(0, 0, angulo - 90f);

        // 5. Escala opcional
        flechaGO.transform.localScale = Vector3.one * 0.55f;
    }


    // LUZ DIRECTA DEL SPOTLIGHT
    public override void RecibirLuz(float daño, TipoLuz tipo)
    {
        recibiendoLuz = true;
        tiempoSinLuz = 0f;

        //  Daño original del ShadowBlock (incluye protección anti-nacimiento)
        base.RecibirLuz(daño, tipo);

        // Cambia sprite según estado (si no está dañado)
        if (!EstaDaniado() && spriteActivo != null)
            sr.sprite = spriteActivo;

        if (vidaActual > 0f && tipo != TipoLuz.Roja)
            ActivarLuzReflejada(tipo);
    }


    // LUZ REFLEJADA DEL EMITTER
    public void RecibirLuz(Vector2 dirLuz, float daño, TipoLuz tipo,
                       Vector2 normal, float alcanceOriginal, Vector2 puntoImpacto)
    {
        recibiendoLuz = true;
        tiempoSinLuz = 0f;

        //  Usa la misma lógica de ShadowBlock (incluye protección anti-instant-kill)
        base.RecibirLuz(daño, tipo);

        if (vidaActual > 0f && tipo != TipoLuz.Roja)
            ActivarLuzReflejada(tipo);
    }


    // ENCENDER EMITTER
    private void ActivarLuzReflejada(TipoLuz tipo)
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

    // APAGAR EMITTER
    private void ApagarLuzReflejada()
    {
        if (luzInstancia != null)
        {
            Destroy(luzInstancia);
            luzInstancia = null;
            emisor = null;
        }
    }

    // ROTAR HAZ
    public void RotarHaz()
    {
        direccionActual = new Vector2(direccionActual.y, -direccionActual.x);
        direccionInicial = direccionActual;

        ActualizarFlecha(); //  AÑADIR ESTO

        if (emisor != null)
            emisor.SetDireccion(direccionActual);
    }
    

    // --- NPC Demostrador: setear dirección manual ---
    public void SetDireccionInicial(Vector2 dir)
    {
        if (dir == Vector2.zero) return;

        direccionInicial = dir.normalized;
        direccionActual = direccionInicial;
        ActualizarFlecha();


        if (emisor != null)
            emisor.SetDireccion(direccionActual);

        Log.Info(this, $"MirrorBlock ajustó dirección inicial a {direccionActual}");
    }

    // DESTRUIR BLOQUE
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
