using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Rendering.Universal;

[ExecuteAlways]
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class TopLightDetector : LightDetectorBase
{
    // CONFIGURACIÓN GENERAL
    [Header("Configuración General")]
    public AnimationCurve curvaIntensidad = AnimationCurve.EaseInOut(0, 1, 1, 0);

    // MOVIMIENTO ENTRE PUNTOS
    [Header("Movimiento en Patrulla")]
    public Transform[] puntosPatrulla;
    public float velocidadMovimiento = 2f;
    public bool idaYVuelta = true;
    public bool moverEntrePuntos = true;
    private int indiceObjetivo = 0;
    private bool retrocediendo = false;

    // PARÁMETROS DEL HAZ (CIRCULAR)
    [Header("Haz Cenital Circular")]
    public float radio = 4f;
    [Range(12, 128)] public int resolucion = 48;


    // MATERIAL VISUAL
    // TITILEO / APAGONES
    [Header("Titileo")]
    // LÁMPARA VISUAL
    [Header("Sprite Lámpara")]
    public Sprite lampSprite;
    public Vector3 lampOffset = Vector3.zero;
    public float lampScale = 1f;

    private SpriteRenderer lampRenderer;

    // LUZ 2D
    [Header("Luz 2D")]
    public bool usarLuz2D = true;
    [Range(0f, 2f)] public float intensidadLuz2D = 0.8f;
    public float multiplicadorRadioLuz = 1.1f;

    private Light2D luz2D;

    // INTERNOS MESH
    private MeshFilter meshFilter;
    private Mesh mesh;

    private HashSet<ShadowBlock> iluminadosPrev = new();

    // Buffers del circulo: se reutilizan porque se arma una vez por frame y por luz.
    private HashSet<ShadowBlock> iluminadosEsteFrame = new();
    private readonly Dictionary<ShadowBlock, float> distanciaPorBloque = new();
    private readonly List<Vector3> vertices = new();
    private readonly List<int> triangles = new();
    private readonly List<Vector2> uvs = new();
    private Vector3[] formaLuz;
    private float radioFormaLuz;
    private bool generandoLuz;

    // RESET

    private bool resetInProgress = false;


    // === Estado inicial ===
    private Vector3 initPos;
    private Quaternion initRot;

    private float initRadio;
    private int initResolucion;
    private bool initTitilar;
    private bool initLuzEncendida;

    private int initIndiceObjetivo;
    private bool initRetrocediendo;
    
    private Vector3 initLampOffset;
    private Color initLampColor; 
    private bool initMoverEntrePuntos;
    private bool initUsarLuz2D;
    private float initIntensidadLuz2D;
    private float initMultiplicadorRadioLuz;


    // AWAKE
    void Awake()
    {
        meshFilter = GetComponent<MeshFilter>();
        meshRenderer = GetComponent<MeshRenderer>();

        if (meshFilter.sharedMesh == null)
        {
            mesh = new Mesh { name = "TopLightMesh" };
            meshFilter.sharedMesh = mesh;
        }
        else mesh = meshFilter.sharedMesh;

        CrearLampara();

        if (usarLuz2D) CrearLuz2D();


        // === Guardar estado inicial del TopLight ===
        initPos = transform.position;
        initRot = transform.rotation;
        initMoverEntrePuntos = moverEntrePuntos;
        initRadio = radio;
        initResolucion = resolucion;
        initTitilar = titilar;
        initLuzEncendida = luzEncendida;
 
        initTipoLuz = tipoLuz;

        InicializarTitileo();


        initIndiceObjetivo = indiceObjetivo;
        initRetrocediendo = retrocediendo;

        initLampOffset = lampOffset;
        if (lampRenderer != null)
            initLampColor = lampRenderer.color;

        initUsarLuz2D = usarLuz2D;
        initIntensidadLuz2D = intensidadLuz2D;
        initMultiplicadorRadioLuz = multiplicadorRadioLuz;


    }

    // UPDATE
    void Update()
    {
        if (resetInProgress)
            return;

        if (!Application.isPlaying)
            return;

        // MATERIAL
        meshRenderer.sharedMaterial =
            tipoLuz == TipoLuz.Roja ? materialRoja : materialAmarilla;

        //  APAGADO MANUAL  luzActiva controla TODO el apagado real
        if (!luzActiva)
        {
            if (meshRenderer != null)
                meshRenderer.enabled = false;

            if (mesh != null)
                mesh.Clear();

            if (usarLuz2D && luz2D != null)
                luz2D.intensity = 0f;

            // LÁMPARA PERMANENTE
            if (lampRenderer != null)
                lampRenderer.enabled = true;

            return;
        }

        // MOVIMIENTO
        ActualizarMovimiento();

        // TITILEO
        ActualizarTitileo();

        //  SI EL TITILEO APAGA  SOLO APAGA EL HAZ, NO LA LÁMPARA
        if (!luzEncendida)
        {
            if (meshRenderer != null)
                meshRenderer.enabled = false;

            if (mesh != null)
                mesh.Clear();

            if (usarLuz2D && luz2D != null)
                luz2D.intensity = 0f;

            if (lampRenderer != null)
                lampRenderer.enabled = true;

            return;
        }

        // GENERAR LUZ
        GenerarLuzCircular();

        // LUZ 2D
        if (usarLuz2D && luz2D != null)
            luz2D.intensity = intensidadLuz2D;

        // LÁMPARA (siempre visible)
        if (lampRenderer != null)
            lampRenderer.enabled = true;
    }


    // MOVIMIENTO ENTRE PUNTOS
    private void ActualizarMovimiento()
    {
        if (!moverEntrePuntos)
            return;

        if (puntosPatrulla == null || puntosPatrulla.Length <= 1) return;

        Transform objetivo = puntosPatrulla[indiceObjetivo];

        transform.position = Vector2.MoveTowards(
            transform.position,
            objetivo.position,
            velocidadMovimiento * Time.deltaTime
        );

        if (Vector2.Distance(transform.position, objetivo.position) < 0.05f)
        {
            if (idaYVuelta)
            {
                if (!retrocediendo)
                {
                    if (indiceObjetivo < puntosPatrulla.Length - 1)
                        indiceObjetivo++;
                    else
                    {
                        retrocediendo = true;
                        indiceObjetivo--;
                    }
                }
                else
                {
                    if (indiceObjetivo > 0)
                        indiceObjetivo--;
                    else
                    {
                        retrocediendo = false;
                        indiceObjetivo++;
                    }
                }
            }
            else indiceObjetivo = (indiceObjetivo + 1) % puntosPatrulla.Length;
        }
    }

    // TITILEO

    // LÓGICA PRINCIPAL DEL HAZ CIRCULAR

    /// <summary>
    /// Arma el circulo de luz y, si se le pide, aplica lo que la luz hace sobre
    /// lo que toca. Van separados porque son dos trabajos distintos: en el
    /// editor interesa ver la forma, no que la escena reaccione.
    /// </summary>
    private void GenerarLuzCircular(bool aplicarEfectos = true)
    {
        // Un efecto puede volver a pedir la luz (Matar reinicia el nivel) y pisaria
        // los buffers a mitad del bucle. La llamada de afuera rehace la malla igual.
        if (generandoLuz) return;

        generandoLuz = true;
        try { ArmarCirculo(aplicarEfectos); }
        finally { generandoLuz = false; }
    }

    private void ArmarCirculo(bool aplicarEfectos)
    {
        Vector2 origen = transform.position;

        vertices.Clear();
        vertices.Add(Vector3.zero);
        triangles.Clear();
        uvs.Clear();
        uvs.Add(new Vector2(0.5f, 0f));

        // Distancia del impacto mas cercano a cada bloque, para aplicarle el
        // daño una sola vez por frame y no una vez por rayo.
        distanciaPorBloque.Clear();

        for (int i = 0; i <= resolucion; i++)
        {
            float ang = i * Mathf.PI * 2f / resolucion;
            Vector2 dir = new(Mathf.Cos(ang), Mathf.Sin(ang));

            RaycastHit2D hit = Physics2D.Raycast(origen, dir, radio, mascaraBloqueos);
            Vector2 punto = hit.collider ? hit.point : origen + dir * radio;
            float distanciaAlCentro = Vector2.Distance(origen, punto);

            if (aplicarEfectos && hit.collider)
            {
                // En el haz cenital el nucleo no es una franja sino el centro
                // del circulo: mata cerca de la lampara y el anillo de afuera
                // solo alumbra.
                bool dentroDelNucleo = distanciaAlCentro <= radio * fraccionLetal;

                // --- JUGADOR ---
                if (dentroDelNucleo && hit.collider.TryGetComponent(out Jugador j))
                    j.Matar();

                //  LUZ ROJA cenital elimina AbyssFlame
                if (tipoLuz == TipoLuz.Roja &&
                    hit.collider.TryGetComponent(out AbyssFlame flame))
                {
                    flame.Extinguir();
                }

                // --- BLOQUES ---
                if (hit.collider.TryGetComponent(out ShadowBlock sb))
                {
                    if (!distanciaPorBloque.TryGetValue(sb, out float previa) || distanciaAlCentro < previa)
                        distanciaPorBloque[sb] = distanciaAlCentro;
                }
            }

            vertices.Add(transform.InverseTransformPoint(punto));

            // El shader lee el desvio en u y el largo en v. En un circulo los
            // dos son la distancia al centro, asi su nucleo coincide con el letal.
            float largo = distanciaAlCentro / radio;
            uvs.Add(new Vector2(0.5f + 0.5f * largo, largo));
        }

        // Triángulos del mesh
        for (int i = 1; i < vertices.Count - 1; i++)
        {
            triangles.Add(0);
            triangles.Add(i);
            triangles.Add(i + 1);
        }

        mesh.Clear();
        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangles, 0);
        mesh.SetUVs(0, uvs);
        mesh.RecalculateBounds();

        iluminadosEsteFrame.Clear();

        foreach (var kvp in distanciaPorBloque)
        {
            // La roja lo mata de una, pero por RecibirLuz para respetar la
            // proteccion del bloque recien colocado.
            float daño = tipoLuz == TipoLuz.Roja
                ? kvp.Key.vidaBajoLuz
                : dañoBase * curvaIntensidad.Evaluate(kvp.Value / radio) * Time.deltaTime;

            kvp.Key.RecibirLuz(daño, tipoLuz);
            iluminadosEsteFrame.Add(kvp.Key);
        }

        // Salir de la luz
        foreach (var sb in iluminadosPrev)
            if (!iluminadosEsteFrame.Contains(sb))
                sb.SalirDeLuz();

        (iluminadosPrev, iluminadosEsteFrame) = (iluminadosEsteFrame, iluminadosPrev);

        ActualizarFormaLuz2D();
    }

    // LUZ 2D
    private void CrearLuz2D()
    {
        var existentes = GetComponentsInChildren<Light2D>(true);
        foreach (var l in existentes)
        {
            if (Application.isPlaying) Destroy(l.gameObject);
            else DestroyImmediate(l.gameObject);
        }

        GameObject luzObj = new("Luz2D_TopLight");
        luzObj.transform.SetParent(transform);
        luzObj.transform.localPosition = Vector3.zero;

        luz2D = luzObj.AddComponent<Light2D>();
        luz2D.lightType = Light2D.LightType.Freeform;
        luz2D.shadowIntensity = 0.25f;
        luz2D.falloffIntensity = 0.4f;
        luz2D.intensity = intensidadLuz2D;

        ActualizarColorLuz2D();

        // Es una luz nueva: hay que pasarle la forma aunque no haya cambiado.
        formaLuz = null;
        ActualizarFormaLuz2D();
    }

    private void ActualizarFormaLuz2D()
    {
        if (luz2D == null) return;

        int puntos = Mathf.Clamp(resolucion, 12, 256);
        float r = radio * multiplicadorRadioLuz;

        // El circulo no depende de lo que tenga alrededor: solo cambia con estos valores.
        if (formaLuz != null && formaLuz.Length == puntos && radioFormaLuz == r)
            return;

        formaLuz = new Vector3[puntos];
        radioFormaLuz = r;

        for (int i = 0; i < puntos; i++)
        {
            float ang = i * Mathf.PI * 2f / puntos;
            formaLuz[i] = new Vector3(Mathf.Cos(ang) * r, Mathf.Sin(ang) * r, 0f);
        }

        luz2D.SetShapePath(formaLuz);
    }

    private void ActualizarColorLuz2D()
    {
        if (luz2D == null) return;

        luz2D.color = tipoLuz == TipoLuz.Roja
            ? new Color(1f, 0.2f, 0.2f)
            : new Color(1f, 0.95f, 0.7f);
    }

    // LÁMPARA VISUAL
    private void CrearLampara()
    {
        if (lampSprite == null) return;

        Transform exist = transform.Find("LampSprite");
        if (exist != null)
        {
            lampRenderer = exist.GetComponent<SpriteRenderer>();
            return;
        }

        GameObject lampObj = new("LampSprite");
        lampObj.transform.SetParent(transform);
        lampObj.transform.localPosition = lampOffset;

        lampRenderer = lampObj.AddComponent<SpriteRenderer>();
        lampRenderer.sprite = lampSprite;
        lampRenderer.transform.localScale = Vector3.one * lampScale;

        lampRenderer.sortingLayerID = meshRenderer.sortingLayerID;
        lampRenderer.sortingOrder = meshRenderer.sortingOrder + 5;

        if (tipoLuz == TipoLuz.Roja)
            lampRenderer.color = new Color(1f, 0.4f, 0.4f);
        else
            lampRenderer.color = new Color(1f, 1f, 0.85f);
    }

    // CAMBIO DE TIPO DE LUZ
    protected override void ActualizarPorTipoDeLuz()
    {
        base.ActualizarPorTipoDeLuz();

        ActualizarColorLuz2D();

        if (lampRenderer != null)
        {
            lampRenderer.color = tipoLuz == TipoLuz.Roja
                ? new Color(1f, 0.4f, 0.4f)
                : new Color(1f, 1f, 0.85f);
        }

#if UNITY_EDITOR
        UnityEditor.SceneView.RepaintAll();
#endif
    }

    /// <summary>La luz 2D acompaña al parpadeo.</summary>
    protected override void AplicarIntensidadLuz2D(bool encendida)
    {
        if (usarLuz2D && luz2D != null)
            luz2D.intensity = encendida ? intensidadLuz2D : 0f;
    }

    // ENCENDER / APAGAR LUZ (igual que SpotLightDetector)
    public override void SetLuzActiva(bool encendida)
    {
        luzActiva = encendida;

        if (!encendida)
        {
            if (meshRenderer != null)
                meshRenderer.enabled = false;

            if (mesh != null)
                mesh.Clear();

            if (usarLuz2D && luz2D != null)
                luz2D.intensity = 0f;

            if (lampRenderer != null)
                lampRenderer.enabled = true;

            return;
        }

        if (meshRenderer != null)
            meshRenderer.enabled = true;

        if (usarLuz2D && luz2D != null)
            luz2D.intensity = intensidadLuz2D;

        GenerarLuzCircular();
    }


    void OnDrawGizmos()
    {
        // Alcance: hasta aca llega la luz, pero el borde no mata
        Gizmos.color = tipoLuz == TipoLuz.Roja
            ? new Color(1f, 0.3f, 0.3f, 0.35f)
            : new Color(1f, 0.95f, 0.4f, 0.35f);
        Gizmos.DrawWireSphere(transform.position, radio);

        // Nucleo: de aca para adentro, mata
        Gizmos.color = new Color(1f, 0.3f, 0.2f, 0.9f);
        Gizmos.DrawWireSphere(transform.position, radio * fraccionLetal);
    }


    /// <summary>
    /// Devuelve la luz a su estado inicial. Lo llama GameManager al reiniciar
    /// el nivel.
    /// </summary>
    public override void ResetToInitialState()
    {
        if (noReset) return;
        StartCoroutine(ProcesarReset());
    }

    private System.Collections.IEnumerator ProcesarReset()
    {
        resetInProgress = true;

        // === Transform ===
        transform.position = initPos;
        transform.rotation = initRot;

        // === Parámetros de luz ===
        radio = initRadio;
        resolucion = initResolucion;
        titilar = initTitilar;
        luzEncendida = initLuzEncendida;
        InicializarTitileo();
        tipoLuz = initTipoLuz;

        // === Movimiento ===
        indiceObjetivo = initIndiceObjetivo;
        retrocediendo = initRetrocediendo;
        moverEntrePuntos = initMoverEntrePuntos;
        // === Material ===
        meshRenderer.sharedMaterial =
            tipoLuz == TipoLuz.Roja ? materialRoja : materialAmarilla;

        // === LUZ 2D ===
        usarLuz2D = initUsarLuz2D;
        intensidadLuz2D = initIntensidadLuz2D;
        multiplicadorRadioLuz = initMultiplicadorRadioLuz;

        if (luz2D != null)
        {
            luz2D.intensity = intensidadLuz2D;
            ActualizarColorLuz2D();
            ActualizarFormaLuz2D();
        }

        // === Reconstrucción del haz ===
        GenerarLuzCircular();

        // === Lámpara ===
        if (lampRenderer != null)
        {
            lampRenderer.color = initLampColor;
            lampRenderer.transform.localPosition = initLampOffset;
            lampRenderer.enabled = luzEncendida;
        }

        // === Congelar 1 frame ===
        yield return null;

        resetInProgress = false;
        Log.Info(this, $"TopLight {name} reseteada.");
    }

}
