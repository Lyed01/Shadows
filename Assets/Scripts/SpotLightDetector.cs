using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Rendering.Universal;

[ExecuteAlways]
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class SpotLightDetector : MonoBehaviour
{
    public enum TipoLuz { Amarilla, Roja }

    [Header("Configuración General")]
    public TipoLuz tipoLuz = TipoLuz.Amarilla;

    [Header("Estado inicial")]
    public bool empezarApagada = false;

    [Header("Pivot de la lámpara / rotación conjunta")]
    public Transform pivotRotacion;
    public Transform lamparaPivot;
    public Vector2 offsetLampara = Vector2.zero;

    [Header("Parámetros del haz")]
    public Vector2 direccion = Vector2.up;
    [Range(10, 180)] public float anguloCono = 90f;
    public float alcance = 8f;
    [Range(6, 100)] public int cantidadRayos = 30;

    [Header("Daño / Intensidad")]
    public float dañoBase = 1f;

    [Header("Capas")]
    public LayerMask mascaraBloqueos;

    [Header("Materiales Mesh")]
    public Material materialAmarilla;
    public Material materialRoja;

    [Header("Ajuste visual de textura")]
    public float multiplicadorAnchoUV = 1f;
    public bool invertirDegradado = false;
    public float offsetU = 0f;

    [Header("Rotación automática")]
    public bool rotacionConstante = false;
    public float velocidadRotacion = 45f;
    public bool oscilacion = false;
    public float rangoOscilacion = 45f;

    [Header("Titileo / Apagones")]
    public bool titilar = false;
    public Vector2 tiempoEncendida = new(2f, 4f);
    public Vector2 tiempoApagada = new(0.3f, 1.2f);

    [Header("Luz 2D del haz")]
    public bool luzSigueHaz = true;
    [Range(0f, 2f)] public float intensidadHaz = 0.8f;
    [Range(0.5f, 2f)] public float multiplicadorAlcanceLuz = 1.1f;

    // Internos
    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;
    private Mesh mesh;
    private Light2D luzHaz;

    private float anguloActual;
    private float timerTitileo;
    private float tiempoOscilacion;
    private float giroAcumulado;
    private bool luzEncendida = true;
    private float anguloBase;
    private bool luzActiva = true;

    // ===== Estado inicial =====
    private Vector3 initialPosition;
    private Quaternion initialRotation;
    private SpotLightDetector.TipoLuz initialTipoLuz;
    private float initialAngulo;
    private float initialAlcance;
    private float initialDañoBase;
    private bool initialRotacionConstante;
    private bool initialTitilar;
    private float offsetOscilacion; 

    // ------------------------------------------------------

    void Start()
    {
        if (empezarApagada)
            SetLuzActiva(false);
        
    }
    void Awake()
    {
        meshFilter = GetComponentInChildren<MeshFilter>();
        meshRenderer = GetComponentInChildren<MeshRenderer>();

        mesh = meshFilter.sharedMesh ?? new Mesh { name = "SpotLightMesh" };
        meshFilter.sharedMesh = mesh;

        anguloActual = Mathf.Atan2(direccion.y, direccion.x) * Mathf.Rad2Deg;
        anguloBase = anguloActual;

        // Guardar estado inicial
        initialPosition = transform.position;
        initialRotation = transform.rotation;
        initialTipoLuz = tipoLuz;
        initialAngulo = anguloCono;
        initialAlcance = alcance;
        initialDañoBase = dañoBase;
        initialRotacionConstante = rotacionConstante;
        initialTitilar = titilar;

        if (luzSigueHaz)
            CrearLuzHaz();
    }

    void Update()
    {
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            meshRenderer.sharedMaterial = (tipoLuz == TipoLuz.Roja) ? materialRoja : materialAmarilla;
            GenerarLuzMesh();
            ActualizarPivotVisual();
            return;
        }
#endif

        meshRenderer.sharedMaterial = (tipoLuz == TipoLuz.Roja) ? materialRoja : materialAmarilla;

        ActualizarRotacionConstante();
        ActualizarOscilacion();
        AplicarRotacionFinal();

        ActualizarTitileo();

        if (luzActiva)
            GenerarLuzMesh();


        ActualizarPivotVisual();

        if (lamparaPivot != null)
        {
            var sprites = lamparaPivot.GetComponentsInChildren<SpriteRenderer>(true);
            foreach (var sr in sprites)
            {
                if (sr != null && !sr.enabled)
                    sr.enabled = true;
            }
        }
    }

    // ------------------------------------------------------
    // ROTACIÓN CONSTANTE + OSCILACIÓN
    // ------------------------------------------------------

    private void ActualizarRotacionConstante()
    {
        if (!rotacionConstante) return;

        // Solo acumula el giro en grados
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

        // Oscilación sólo hacia un lado (0 → rango)
        float halfCycle = (Mathf.Sin(tiempoOscilacion * velocidadRotacion * Mathf.Deg2Rad) + 1f) * 0.5f;

        // 0 a rangoOscilacion
        offsetOscilacion = halfCycle * rangoOscilacion;
    }


    private void AplicarRotacionFinal()
    {
        // Ángulo total: base + giro constante + oscilación
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
            lamparaPivot.position = ((pivotRotacion != null) ? pivotRotacion.position : transform.position)
                                    + (Vector3)offsetLampara;
        }
    }

    // ------------------------------------------------------
    // TITILEO
    // ------------------------------------------------------
    private void ActualizarTitileo()
    {
        if (!titilar)
        {
            luzEncendida = true;
            meshRenderer.enabled = true;
            return;
        }

        timerTitileo -= Time.deltaTime;

        if (timerTitileo <= 0f)
        {
            luzEncendida = !luzEncendida;
            timerTitileo = luzEncendida
                ? Random.Range(tiempoEncendida.x, tiempoEncendida.y)
                : Random.Range(tiempoApagada.x, tiempoApagada.y);
        }

        meshRenderer.enabled = luzEncendida;
        if (luzHaz != null)
            luzHaz.intensity = luzEncendida ? intensidadHaz : 0f;
    }

    // ------------------------------------------------------
    // MESH + LUZ
    // ------------------------------------------------------
    private void GenerarLuzMesh()
    {
        if (!luzActiva)
            return;

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

            RaycastHit2D hit = Physics2D.Raycast(origen, dirRay, alcance, mascaraBloqueos);
            Vector2 puntoMundo = hit.collider ? hit.point : origen + dirRay * alcance;

            if (hit.collider && hit.collider.TryGetComponent(out Jugador j))
                j.Matar();

            if (hit.collider && hit.collider.TryGetComponent(out LightReceptor receptor))
                receptor.RecibirLuz(tipoLuz);

            if (hit.collider && hit.collider.TryGetComponent(out ShadowBlock sb))
            {
                float distancia = Vector2.Distance(origen, puntoMundo);

                if (!iluminadosEsteFrame.ContainsKey(sb) || distancia < iluminadosEsteFrame[sb])
                    iluminadosEsteFrame[sb] = distancia;
            }

            vertices.Add(transform.InverseTransformPoint(puntoMundo));
        }

        // Aplicar daño una vez por bloque
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

        // Luz 2D
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
    // CREAR LUZ FREEFORM
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

    private void ActualizarColorLuzHaz()
    {
        if (luzHaz == null) return;

        if (tipoLuz == TipoLuz.Roja)
            luzHaz.color = new Color(1f, 0.25f, 0.2f);
        else
            luzHaz.color = new Color(1f, 0.95f, 0.7f);
    }

    // ------------------------------------------------------
    // TIPO DE LUZ
    // ------------------------------------------------------
    public void AlternarTipoLuz()
    {
        tipoLuz = (tipoLuz == TipoLuz.Amarilla) ? TipoLuz.Roja : TipoLuz.Amarilla;
        ActualizarEstadoLuz();
    }

    public void SetTipoLuz(TipoLuz nuevoTipo)
    {
        tipoLuz = nuevoTipo;
        ActualizarEstadoLuz();
    }

    private void ActualizarEstadoLuz()
    {
        if (meshRenderer == null)
            meshRenderer = GetComponent<MeshRenderer>();

        if (tipoLuz == TipoLuz.Roja)
        {
            meshRenderer.sharedMaterial = materialRoja;
            dañoBase = 10f;
        }
        else
        {
            meshRenderer.sharedMaterial = materialAmarilla;
            dañoBase = 5f;
        }

        ActualizarColorLuzHaz();

#if UNITY_EDITOR
        UnityEditor.SceneView.RepaintAll();
#endif
    }

    public void SetLuzActiva(bool encendida)
    {
        luzActiva = encendida;

        if (!encendida)
        {
            // Apagar completamente el haz
            if (meshRenderer != null)
                meshRenderer.enabled = false;

            // Apagar la luz 2D
            if (luzHaz != null)
                luzHaz.intensity = 0f;

            // ❗ LIMPIAR EL MESH PARA QUE NO SE VEA MÁS
            if (mesh != null)
                mesh.Clear();

            // Reset estados
            luzEncendida = false;
            timerTitileo = 0f;
            giroAcumulado = 0f;
            offsetOscilacion = 0f;

            return;
        }

        // SI ENCENDER
        if (meshRenderer != null)
            meshRenderer.enabled = true;

        if (luzHaz != null)
            luzHaz.intensity = intensidadHaz;

        // Forzar regeneración inmediata del haz
        GenerarLuzMesh();
    }




    // RESET TOTAL
    // ------------------------------------------------------
    public void ResetToInitialState()
    {
        transform.position = initialPosition;
        transform.rotation = initialRotation;

        tipoLuz = initialTipoLuz;
        anguloCono = initialAngulo;
        alcance = initialAlcance;
        dañoBase = initialDañoBase;
        rotacionConstante = initialRotacionConstante;
        titilar = initialTitilar;

        ActualizarEstadoLuz();

        if (luzHaz != null)
        {
            luzHaz.intensity = intensidadHaz;
            ActualizarColorLuzHaz();
        }

        GenerarLuzMesh();

        Debug.Log($"🔄 Spotlight {name} reseteado a su estado inicial.");
    }
}
