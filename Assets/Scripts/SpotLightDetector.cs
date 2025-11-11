using UnityEngine;
using System.Collections.Generic;

[ExecuteAlways]
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class SpotLightDetector : MonoBehaviour
{
    public enum TipoLuz { Amarilla, Roja }

    [Header("Configuración General")]
    public TipoLuz tipoLuz = TipoLuz.Amarilla;

    [Header("Origen / Movimiento")]
    public Transform jugador;
    public bool seguirJugador = true;

    [Header("Parámetros del haz")]
    public Vector2 direccion = Vector2.up;
    [Range(10, 180)] public float anguloCono = 90f;
    public float alcance = 8f;
    [Range(6, 100)] public int cantidadRayos = 30;

    [Header("Daño / Intensidad")]
    public float dañoBase = 1f;
    public AnimationCurve curvaIntensidad = AnimationCurve.EaseInOut(0, 1, 1, 0);

    [Header("Capas")]
    public LayerMask mascaraBloqueos;

    [Header("Materiales Mesh")]
    public Material materialAmarilla;
    public Material materialRoja;

    [Header("Ajuste visual de textura")]
    [Tooltip("Multiplicador para ajustar el ancho de la textura del haz")]
    public float multiplicadorAnchoUV = 1f;
    [Tooltip("Invertir degradado verticalmente (true = empieza en el origen)")]
    public bool invertirDegradado = false;
    [Tooltip("Desplaza la textura horizontalmente (izquierda/derecha)")]
    public float offsetU = 0f;

    [Header("Referencia visual (opcional)")]
    [Tooltip("Pivot que contiene los sprites de la lámpara (Front/Back)")]
    public Transform lamparaPivot;
    public Vector2 offsetLampara = Vector2.zero;

    [Header("Comportamiento automático")]
    [Tooltip("Si está activo, la luz gira constantemente")]
    public bool rotacionConstante = false;
    [Tooltip("Velocidad de rotación en grados por segundo")]
    public float velocidadRotacion = 45f;
    [Tooltip("Si está activo, la luz oscila entre dos ángulos")]
    public bool oscilacion = false;
    [Tooltip("Rango máximo de oscilación en grados")]
    public float rangoOscilacion = 45f;
    private float oscilacionOffset = 0f;

    [Header("Titileo / Apagones")]
    [Tooltip("Si está activo, la luz se apaga aleatoriamente o por ciclos")]
    public bool titilar = false;
    [Tooltip("Duración encendida (segundos)")]
    public Vector2 tiempoEncendida = new Vector2(2f, 4f);
    [Tooltip("Duración apagada (segundos)")]
    public Vector2 tiempoApagada = new Vector2(0.3f, 1.2f);
    private float timerTitileo = 0f;
    private bool luzEncendida = true;

    // --- Internos ---
    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;
    private Mesh mesh;
    private HashSet<ShadowBlock> iluminadosPrev = new HashSet<ShadowBlock>();

    void Awake()
    {
        meshFilter = GetComponent<MeshFilter>();
        meshRenderer = GetComponent<MeshRenderer>();

        if (meshFilter.sharedMesh == null)
        {
            mesh = new Mesh { name = "SpotLightMesh" };
            meshFilter.sharedMesh = mesh;
        }
        else
        {
            mesh = meshFilter.sharedMesh;
        }
    }

    void Update()
    {
        if (!Application.isPlaying) return;

        // 🔹 Actualizar material
        meshRenderer.sharedMaterial = (tipoLuz == TipoLuz.Roja) ? materialRoja : materialAmarilla;

        // 🔹 Seguir jugador si corresponde
        if (seguirJugador && jugador != null)
            transform.position = jugador.position;

        // 🔹 Comportamiento de rotación automática
        ActualizarRotacionAutomatica();

        // 🔹 Control de titileo (apagones)
        ActualizarTitileo();

        // 🔹 Actualizar rotación general según dirección
        ActualizarRotacion();

        // 🔹 Generar el mesh de la luz
        if (luzEncendida)
            GenerarLuzMesh();

        // 🔹 Sincronizar la lámpara visual si existe
        if (lamparaPivot != null)
        {
            lamparaPivot.position = transform.position + (Vector3)offsetLampara;
            lamparaPivot.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(direccion.y, direccion.x) * Mathf.Rad2Deg - 90f);
        }
    }

    // --- 🔸 ROTACIÓN AUTOMÁTICA / OSCILANTE ---
    private void ActualizarRotacionAutomatica()
    {
        if (!rotacionConstante) return;

        if (oscilacion)
        {
            // Oscila entre -rangoOscilacion y +rangoOscilacion
            oscilacionOffset += Time.deltaTime * velocidadRotacion;
            float anguloOsc = Mathf.Sin(oscilacionOffset * Mathf.Deg2Rad) * rangoOscilacion;
            Vector2 baseDir = Quaternion.Euler(0, 0, anguloOsc) * Vector2.up;
            direccion = baseDir.normalized;
        }
        else
        {
            // Rotación constante 360°
            direccion = Quaternion.Euler(0, 0, velocidadRotacion * Time.deltaTime) * direccion;
        }
    }

    // --- 🔸 TITILEO ---
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

            if (luzEncendida)
                timerTitileo = Random.Range(tiempoEncendida.x, tiempoEncendida.y);
            else
                timerTitileo = Random.Range(tiempoApagada.x, tiempoApagada.y);
        }

        meshRenderer.enabled = luzEncendida;
    }

    // --- 🔸 GENERAR MESH ---
    private void GenerarLuzMesh()
    {
        Vector2 origen = transform.position;
        Vector2 dirBase = direccion.normalized;
        float angInicio = -anguloCono * 0.5f;

        HashSet<ShadowBlock> iluminadosEsteFrame = new();
        List<Vector3> vertices = new() { Vector3.zero };
        List<int> triangles = new();

        int raycastMask = ~0;

        for (int i = 0; i <= cantidadRayos; i++)
        {
            float t = i / (float)cantidadRayos;
            float angActual = angInicio + t * anguloCono;
            Vector2 dirRay = Quaternion.Euler(0, 0, angActual) * dirBase;

            RaycastHit2D hit = Physics2D.Raycast(origen, dirRay, alcance, mascaraBloqueos);
            Vector2 puntoMundo = hit.collider ? hit.point : origen + dirRay * alcance;

            if (hit.collider)
            {
                if (hit.collider.TryGetComponent(out Jugador j))
                    j.Matar();

                if (hit.collider.TryGetComponent(out LightReceptor receptor))
                    receptor.RecibirLuz(tipoLuz);

                if (hit.collider.TryGetComponent(out ShadowBlock sb))
                {
                    float distancia = Vector2.Distance(origen, puntoMundo);
                    float intensidad = curvaIntensidad.Evaluate(1f - distancia / alcance);
                    sb.RecibirLuz(dañoBase * intensidad * Time.deltaTime, tipoLuz);
                    iluminadosEsteFrame.Add(sb);
                }
            }

            vertices.Add(transform.InverseTransformPoint(puntoMundo));
        }

        // Crear triángulos
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

        // Apagar bloques fuera de luz
        foreach (var sbAnt in iluminadosPrev)
        {
            if (sbAnt == null) continue;
            if (!iluminadosEsteFrame.Contains(sbAnt))
                sbAnt.SalirDeLuz();
        }

        iluminadosPrev = new HashSet<ShadowBlock>(iluminadosEsteFrame);
    }

    // --- 🔸 ROTACIÓN GENERAL ---
    private void ActualizarRotacion()
    {
        float angulo = Mathf.Atan2(direccion.y, direccion.x) * Mathf.Rad2Deg - 90f;
        transform.rotation = Quaternion.Euler(0, 0, angulo);
    }

    // --- 🔸 INTERFAZ PÚBLICA ---
    public void SetDireccion(Vector2 nuevaDir)
    {
        direccion = nuevaDir.normalized;
        ActualizarRotacion();
    }

    public void SetAngulo(float a) => anguloCono = a;
    public void SetAlcance(float r) => alcance = r;

    // --- 🔸 GIZMOS ---
    private void OnDrawGizmos()
    {
        Vector2 origen = transform.position;
        Vector2 dirBase = direccion.normalized;
        float angInicio = -anguloCono * 0.5f;
        Color colorBase = (tipoLuz == TipoLuz.Roja) ? Color.red : Color.yellow;

        for (int i = 0; i <= cantidadRayos; i++)
        {
            float t = (float)i / cantidadRayos;
            float angActual = angInicio + t * anguloCono;
            Vector2 dirRay = Quaternion.Euler(0, 0, angActual) * dirBase;
            Gizmos.color = colorBase * 0.4f;
            Gizmos.DrawRay((Vector3)origen, (Vector3)(dirRay * alcance));
        }
    }

    // --- 🔸 TIPOS DE LUZ ---
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
            dañoBase = 1f;
        }

#if UNITY_EDITOR
        UnityEditor.SceneView.RepaintAll();
#endif
    }
}
