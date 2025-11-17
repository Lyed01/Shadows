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
    private float faseOscilacion;
    private float timerTitileo;
    private bool luzEncendida = true;

    // ------------------------------------------------------
    void Awake()
    {
        meshFilter = GetComponent<MeshFilter>();
        meshRenderer = GetComponent<MeshRenderer>();

        mesh = meshFilter.sharedMesh ?? new Mesh { name = "SpotLightMesh" };
        meshFilter.sharedMesh = mesh;

        anguloActual = Mathf.Atan2(direccion.y, direccion.x) * Mathf.Rad2Deg;

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

            if (lamparaPivot != null)
            {
                float z = Mathf.Atan2(direccion.y, direccion.x) * Mathf.Rad2Deg - 90f;
                lamparaPivot.rotation = Quaternion.Euler(0, 0, z);
                lamparaPivot.position = ((pivotRotacion != null) ? pivotRotacion.position : transform.position) + (Vector3)offsetLampara;
            }

            return;
        }
#endif
        meshRenderer.sharedMaterial = (tipoLuz == TipoLuz.Roja) ? materialRoja : materialAmarilla;

    

        ActualizarRotacionAutomatica();
        direccion = AnguloAGradosAUnidad(anguloActual);
        ActualizarRotacion();
        ActualizarTitileo();

        if (luzEncendida)
            GenerarLuzMesh();

        if (lamparaPivot != null)
            lamparaPivot.position = ((pivotRotacion != null) ? pivotRotacion.position : transform.position) + (Vector3)offsetLampara;
    }

    // ------------------------------------------------------
    // ROTACIÓN
    // ------------------------------------------------------
    private void ActualizarRotacionAutomatica()
    {
        if (!rotacionConstante) return;

        if (oscilacion)
        {
            faseOscilacion += Time.deltaTime * velocidadRotacion;
            float offset = Mathf.Sin(faseOscilacion * Mathf.Deg2Rad) * rangoOscilacion;
            anguloActual += offset * Time.deltaTime;
        }
        else
        {
            anguloActual += velocidadRotacion * Time.deltaTime;
        }
    }

    private void ActualizarRotacion()
    {
        float z = Mathf.Atan2(direccion.y, direccion.x) * Mathf.Rad2Deg - 90f;
        transform.rotation = Quaternion.Euler(0, 0, z);

        Transform pivVisual = lamparaPivot != null ? lamparaPivot : pivotRotacion;
        if (pivVisual != null)
            pivVisual.rotation = Quaternion.Euler(0, 0, z);
    }

    private static Vector2 AnguloAGradosAUnidad(float grados)
    {
        float rad = grados * Mathf.Deg2Rad;
        return new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)).normalized;
    }

    // ------------------------------------------------------
    // TITILEO
    // ------------------------------------------------------
    private void ActualizarTitileo()
    {
        if (!titilar)
        {
            if (!meshRenderer.enabled) meshRenderer.enabled = true;
            luzEncendida = true;
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
        Vector2 origen = (pivotRotacion != null) ? (Vector2)pivotRotacion.position : (Vector2)transform.position;
        Vector2 dirBase = direccion.normalized;
        float angInicio = -anguloCono * 0.5f;

        // 👉 Aquí está el cambio importante:
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

            // Jugador
            if (hit.collider && hit.collider.TryGetComponent(out Jugador j))
                j.Matar();

            // Light receptors
            if (hit.collider && hit.collider.TryGetComponent(out LightReceptor receptor))
                receptor.RecibirLuz(tipoLuz);

            // ShadowBlocks → registro sin aplicar daño aún
            if (hit.collider && hit.collider.TryGetComponent(out ShadowBlock sb))
            {
                float distancia = Vector2.Distance(origen, puntoMundo);

                if (iluminadosEsteFrame.TryGetValue(sb, out float distGuardada))
                {
                    if (distancia < distGuardada)
                        iluminadosEsteFrame[sb] = distancia;
                }
                else
                {
                    iluminadosEsteFrame.Add(sb, distancia);
                }
            }

            vertices.Add(transform.InverseTransformPoint(puntoMundo));
        }

        // 👉 Aplicar daño solo una vez por bloque
        foreach (var kvp in iluminadosEsteFrame)
        {
            ShadowBlock sb = kvp.Key;
            float distancia = kvp.Value;

            float normalizado = Mathf.Clamp01(distancia / alcance);
            float intensidad = 1f - Mathf.Clamp01(distancia / alcance);
            float daño = dañoBase * intensidad * Time.deltaTime;

            // 🔥 DEBUG AQUÍ
            Debug.Log(
                $"🟡 [Spotlight] {sb.name} | " +
                $"Dist: {distancia:F2}/{alcance:F2} | " +
                $"Norm: {normalizado:F2} | " +
                $"Int:{intensidad:F3} | " +
                $"Daño:{daño:F4}"
            );

            sb.RecibirLuz(daño, tipoLuz);
        }

        // Triangles
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

        // LUZ 2D
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
}
