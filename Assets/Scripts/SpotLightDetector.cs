using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Rendering.Universal;

[ExecuteAlways]
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class SpotLightDetector : LightDetectorBase
{
    // ============================================================
    // CONFIGURACIÓN GENERAL
    // ============================================================

    [Header("Estado inicial")]
    public bool empezarApagada = false;

    [Header("Pivot de la lámpara / rotación conjunta")]
    public Transform pivotRotacion;
    public Transform lamparaPivot;
    public Vector2 offsetLampara = Vector2.zero;

    [Header("Parámetros del haz")]
    public Vector2 direccion = Vector2.up;
    [Range(1, 180)] public float anguloCono = 90f;
    public float alcance = 8f;
    [Range(6, 100)] public int cantidadRayos = 30;

    [Header("Ajuste visual de textura")]
    public float multiplicadorAnchoUV = 1f;
    public bool invertirDegradado = false;
    public float offsetU = 0f;

    [Header("Rotación automática")]
    public bool rotacionConstante = false;
    public float velocidadRotacion = 45f;
    public bool oscilacion = false;
    public float rangoOscilacion = 45f;

    [Header("Luz 2D del haz")]
    public bool luzSigueHaz = true;
    [Range(0f, 2f)] public float intensidadHaz = 0.8f;
    [Range(0.5f, 2f)] public float multiplicadorAlcanceLuz = 1.1f;

    // ============================================================
    // INTERNOS
    // ============================================================
    private MeshFilter meshFilter;
    private Mesh mesh;
    private Light2D luzHaz;

    private float anguloActual;
    private float tiempoOscilacion;
    private float giroAcumulado;
    private float anguloBase;
    private float offsetOscilacion = 0f;

    private bool resetInProgress = false;

    // === ESTADO INICIAL REAL ===
    private Vector3 initPos;
    private Quaternion initRot;

    private Vector2 initDireccion;
    private float initAnguloCono;
    private float initAlcance;
    private float initDañoBase;

    private bool initRotacionConstante;
    private bool initOscilacion;
    private float initRangoOscilacion;
    private bool initTitilar;

    private float initAnguloBase;
    private bool initLuzActiva;
    private bool initLuzEncendida;

    // ------------------------------------------------------
    // AWAKE
    // ------------------------------------------------------
    void Awake()
    {
        if (empezarApagada)
            SetLuzActiva(false);

        meshFilter = GetComponentInChildren<MeshFilter>();
        meshRenderer = GetComponentInChildren<MeshRenderer>();

        mesh = meshFilter.sharedMesh ?? new Mesh { name = "SpotLightMesh" };
        meshFilter.sharedMesh = mesh;

        anguloActual = Mathf.Atan2(direccion.y, direccion.x) * Mathf.Rad2Deg;
        anguloBase = anguloActual;

        // Guardar estado inicial
        initPos = transform.position;
        initRot = transform.rotation;

        initDireccion = direccion;
        initAnguloCono = anguloCono;
        initAlcance = alcance;
        initDañoBase = dañoBase;
        initTipoLuz = tipoLuz;

        initRotacionConstante = rotacionConstante;
        initOscilacion = oscilacion;
        initRangoOscilacion = rangoOscilacion;
        initTitilar = titilar;

        initAnguloBase = anguloBase;
        initLuzActiva = luzActiva;
        initLuzEncendida = luzEncendida;

        InicializarTitileo();

        if (luzSigueHaz)
            CrearLuzHaz();
    }

    // ------------------------------------------------------
    // UPDATE
    // ------------------------------------------------------
    void Update()
    {
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            if (meshRenderer == null)
                meshRenderer = GetComponentInChildren<MeshRenderer>();

            meshRenderer.sharedMaterial =
                (tipoLuz == TipoLuz.Roja) ? materialRoja : materialAmarilla;

            GenerarLuzMesh();
            ActualizarPivotVisual();
            return;
        }
#endif

        // Si la luz está apagada manualmente → no hacer nada
        if (!luzActiva)
        {
            if (meshRenderer != null)
                meshRenderer.enabled = false;

            if (luzHaz != null)
                luzHaz.intensity = 0f;

            return;
        }

        // Material según tipo de luz
        if (meshRenderer == null)
            meshRenderer = GetComponentInChildren<MeshRenderer>();

        meshRenderer.sharedMaterial =
            (tipoLuz == TipoLuz.Roja) ? materialRoja : materialAmarilla;

        // Rotación y oscilación
        ActualizarRotacionConstante();
        ActualizarOscilacion();
        AplicarRotacionFinal();

        ActualizarTitileo();

        // Generar mesh si está encendida
        if (luzEncendida)
            GenerarLuzMesh();

        // Pivot visual
        ActualizarPivotVisual();
    }

    // ------------------------------------------------------
    // ROTACIÓN CONSTANTE + OSCILACIÓN
    // ------------------------------------------------------
    private void ActualizarRotacionConstante()
    {
        if (!rotacionConstante) return;
        giroAcumulado += velocidadRotacion * Time.deltaTime;
    }

    private void ActualizarOscilacion()
    {
        if (!oscilacion)
        {
            offsetOscilacion = 0f;
            return;
        }

        tiempoOscilacion += Time.deltaTime;

        float halfCycle =
            (Mathf.Sin(tiempoOscilacion * velocidadRotacion * Mathf.Deg2Rad) + 1f) * 0.5f;

        offsetOscilacion = halfCycle * rangoOscilacion;
    }

    private void AplicarRotacionFinal()
    {
        anguloActual = anguloBase + giroAcumulado + offsetOscilacion;

        float rad = anguloActual * Mathf.Deg2Rad;
        direccion = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)).normalized;
    }

    private void ActualizarPivotVisual()
    {
        if (lamparaPivot == null && pivotRotacion == null) return;

        float z = anguloActual - 90f;

        Transform piv = lamparaPivot != null ? lamparaPivot : pivotRotacion;
        piv.rotation = Quaternion.Euler(0, 0, z);

        if (lamparaPivot != null)
        {
            lamparaPivot.position =
                ((pivotRotacion != null) ? pivotRotacion.position : transform.position)
                + (Vector3)offsetLampara;
        }
    }

    // ------------------------------------------------------
    // TITILEO
    // ------------------------------------------------------

    // ------------------------------------------------------
    // MESH + LUZ
    // ------------------------------------------------------
    private void GenerarLuzMesh()
    {
        // 1. Apagada manualmente
        if (!luzActiva)
            return;

        // 2. Titileo la apagó en este frame
        if (!luzEncendida)
        {
            if (mesh != null)
                mesh.Clear();
            return;
        }

        Vector2 origen = (pivotRotacion != null)
            ? pivotRotacion.position
            : (Vector2)transform.position;

        Vector2 dirBase = direccion.normalized;
        float angInicio = -anguloCono * 0.5f;

        Dictionary<ShadowBlock, float> iluminadosEsteFrame = new();
        List<Vector3> vertices = new() { Vector3.zero };
        List<int> triangles = new();

        for (int i = 0; i <= cantidadRayos; i++)
        {
            float t = i / (float)cantidadRayos;
            float angActual = angInicio + t * anguloCono;
            Vector2 dirRay = Quaternion.Euler(0, 0, angActual) * dirBase;

            RaycastHit2D hit =
                Physics2D.Raycast(origen, dirRay, alcance, mascaraBloqueos);

            Vector2 puntoMundo =
                hit.collider ? hit.point : origen + dirRay * alcance;

            // Daño al jugador
            if (hit.collider && hit.collider.TryGetComponent(out Jugador j))
                j.Matar();

            // Luz roja mata AbyssFlame
            if (tipoLuz == TipoLuz.Roja &&
                hit.collider &&
                hit.collider.TryGetComponent(out AbyssFlame flame))
            {
                flame.Extinguir();
            }

            // Enviar luz a receptores
            if (hit.collider && hit.collider.TryGetComponent(out LightReceptor receptor))
                receptor.RecibirLuz(tipoLuz);

            // ShadowBlocks
            if (hit.collider && hit.collider.TryGetComponent(out ShadowBlock sb))
            {
                float distancia = Vector2.Distance(origen, puntoMundo);

                if (!iluminadosEsteFrame.ContainsKey(sb) ||
                    distancia < iluminadosEsteFrame[sb])
                {
                    iluminadosEsteFrame[sb] = distancia;
                }
            }

            vertices.Add(transform.InverseTransformPoint(puntoMundo));
        }

        // Aplicar daño por bloque
        foreach (var kvp in iluminadosEsteFrame)
        {
            float dist = kvp.Value;
            float intensidad = 1f - Mathf.Clamp01(dist / alcance);
            float daño = dañoBase * intensidad * Time.deltaTime;

            kvp.Key.RecibirLuz(daño, tipoLuz);
        }

        // Triángulos
        for (int i = 1; i < vertices.Count - 1; i++)
        {
            triangles.Add(0);
            triangles.Add(i);
            triangles.Add(i + 1);
        }

        // UVs
        List<Vector2> uvs = new();
        for (int i = 0; i < vertices.Count; i++)
        {
            float u = (float)i / (vertices.Count - 1);
            float v = i == 0 ? 0f : 1f;
            if (invertirDegradado) v = 1f - v;

            u = (u - 0.5f) / multiplicadorAnchoUV + 0.5f + offsetU;
            uvs.Add(new Vector2(u, v));
        }

        mesh.Clear();
        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangles, 0);
        mesh.SetUVs(0, uvs);
        mesh.RecalculateBounds();

        // Luz 2D freeform
        if (luzSigueHaz && luzHaz != null)
        {
            int count = vertices.Count;
            Vector3[] forma3D = new Vector3[count];

            Vector2 origenLuz = origen;
            forma3D[0] = transform.InverseTransformPoint(origenLuz);

            for (int i = 1; i < count; i++)
            {
                Vector3 v = vertices[i];
                Vector3 mundo = transform.TransformPoint(v);
                Vector2 dir = ((Vector2)mundo - origenLuz).normalized;
                float dist = Vector2.Distance(mundo, origenLuz);
                Vector2 extendido = origenLuz + dir * dist * multiplicadorAlcanceLuz;
                forma3D[i] = transform.InverseTransformPoint(extendido);
            }

            luzHaz.SetShapePath(forma3D);
            luzHaz.intensity = luzEncendida ? intensidadHaz : 0f;
        }
    }

    // ------------------------------------------------------
    // CREAR LUZ FREEFORM 2D
    // ------------------------------------------------------
    private void CrearLuzHaz()
    {
        var existentes = GetComponentsInChildren<Light2D>(true);
        foreach (var l in existentes)
        {
            if (Application.isPlaying)
                Destroy(l.gameObject);
            else
                DestroyImmediate(l.gameObject);
        }

        GameObject luzObj = new GameObject("LuzHaz2D");
        luzObj.transform.SetParent(transform);
        luzObj.transform.localPosition = Vector3.zero;

        luzHaz = luzObj.AddComponent<Light2D>();
        luzHaz.lightType = Light2D.LightType.Freeform;
        luzHaz.shadowIntensity = 0.15f;
        luzHaz.falloffIntensity = 0.35f;
        luzHaz.intensity = intensidadHaz;

        ActualizarColorLuzHaz();
    }

    protected override void AplicarIntensidadLuz2D(bool encendida)
    {
        if (luzHaz != null)
            luzHaz.intensity = encendida ? intensidadHaz : 0f;
    }

    private void ActualizarColorLuzHaz()
    {
        if (luzHaz == null) return;

        if (tipoLuz == TipoLuz.Roja)
            luzHaz.color = new Color(1f, 0.25f, 0.2f);
        else
            luzHaz.color = new Color(1f, 0.95f, 0.7f);
    }

    // ------------------------------------------------------
    // TIPO DE LUZ (API PÚBLICA)
    // ------------------------------------------------------
    protected override void ActualizarPorTipoDeLuz()
    {
        if (meshRenderer == null)
            meshRenderer = GetComponentInChildren<MeshRenderer>();

        base.ActualizarPorTipoDeLuz();
        ActualizarColorLuzHaz();

#if UNITY_EDITOR
        UnityEditor.SceneView.RepaintAll();
#endif
    }

    // ------------------------------------------------------
    // ENCENDER / APAGAR COMPLETAMENTE
    // ------------------------------------------------------
    public override void SetLuzActiva(bool encendida)
    {
        luzActiva = encendida;

        if (!encendida)
        {
            if (meshRenderer != null)
                meshRenderer.enabled = false;

            if (luzHaz != null)
                luzHaz.intensity = 0f;

            if (mesh != null)
                mesh.Clear();

            luzEncendida = false;
            giroAcumulado = 0f;
            offsetOscilacion = 0f;

            return;
        }

        if (meshRenderer != null)
            meshRenderer.enabled = true;

        if (luzHaz != null)
            luzHaz.intensity = intensidadHaz;

        InicializarTitileo();
        GenerarLuzMesh();
    }

    // ------------------------------------------------------
    // RESET TOTAL
    // ------------------------------------------------------
    public override void ResetToInitialState()
    {
        if (noReset) return;
        StartCoroutine(ProcesarReset());
    }

    private System.Collections.IEnumerator ProcesarReset()
    {
        resetInProgress = true;

        // Transform
        transform.position = initPos;
        transform.rotation = initRot;

        // Parámetros
        direccion = initDireccion;
        anguloCono = initAnguloCono;
        alcance = initAlcance;
        dañoBase = initDañoBase;

        rotacionConstante = initRotacionConstante;
        oscilacion = initOscilacion;
        rangoOscilacion = initRangoOscilacion;
        titilar = initTitilar;
        InicializarTitileo();

        anguloBase = initAnguloBase;
        giroAcumulado = 0f;
        offsetOscilacion = 0f;
        tiempoOscilacion = 0f;

        luzActiva = initLuzActiva;
        luzEncendida = initLuzEncendida;

        // Tipo de luz vuelve al inicial
        tipoLuz = initTipoLuz;
        ActualizarPorTipoDeLuz();

        // Luz 2D
        if (luzHaz != null)
        {
            luzHaz.intensity = intensidadHaz;
            ActualizarColorLuzHaz();
        }

        // Reconstrucción visual
        GenerarLuzMesh();
        ActualizarPivotVisual();

        yield return null;

        resetInProgress = false;

        Debug.Log($"🔄 Spotlight {name} reseteado a su estado inicial.");
    }
}
