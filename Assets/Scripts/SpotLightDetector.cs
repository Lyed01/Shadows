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
    public float anguloCono = 90f;
    public float alcance = 8f;
    public int cantidadRayos = 30;

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
    public float multiplicadorAnchoUV = 2f;
    [Tooltip("Invertir degradado verticalmente (true = empieza en el origen)")]
    public bool invertirDegradado = true;
    [Tooltip("Desplaza la textura horizontalmente (izquierda/derecha)")]
    public float offsetU = 0f;

    [Tooltip("Escala automática según el ángulo del cono")]
    public bool usarAutoEscalaUV = true;


    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;
    private Mesh mesh;
    private HashSet<ShadowBlock> iluminadosPrev = new HashSet<ShadowBlock>();

    void Awake()
{
    meshFilter = GetComponent<MeshFilter>();
    if (meshFilter == null)
        meshFilter = gameObject.AddComponent<MeshFilter>();

    meshRenderer = GetComponent<MeshRenderer>();
    if (meshRenderer == null)
        meshRenderer = gameObject.AddComponent<MeshRenderer>();

    mesh = new Mesh();
    mesh.name = "SpotLightMesh";
    meshFilter.mesh = mesh;

}

    void Update()
    {
        if (!Application.isPlaying) return;

        // Actualizar material si el tipo de luz cambia
        meshRenderer.material = (tipoLuz == TipoLuz.Roja) ? materialRoja : materialAmarilla;

        if (seguirJugador && jugador != null)
            transform.position = jugador.position;

        Vector2 origen = transform.position;
        Vector2 dirBase = direccion.normalized;
        float angInicio = -anguloCono * 0.5f;

        HashSet<ShadowBlock> iluminadosEsteFrame = new HashSet<ShadowBlock>();

        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();
        vertices.Add(Vector3.zero);

        int raycastMask = ~0;

        // 🔸 Lanzar rayos y generar vértices del mesh
        for (int i = 0; i <= cantidadRayos; i++)
        {
            float t = (float)i / cantidadRayos;
            float angActual = angInicio + t * anguloCono;
            Vector2 dirRay = Quaternion.Euler(0, 0, angActual) * dirBase;

            RaycastHit2D hit = Physics2D.Raycast(origen, dirRay, alcance, raycastMask);
            Vector2 puntoMundo = hit.collider != null ? hit.point : origen + dirRay * alcance;

            // --- Matar jugador ---
            Jugador j = hit.collider ? hit.collider.GetComponent<Jugador>() : null;
            if (j != null) j.Matar();

            LightReceptor receptor = hit.collider ? hit.collider.GetComponent<LightReceptor>() : null;
            if (receptor != null)
            {
                receptor.RecibirLuz(tipoLuz);
            }
            // --- ShadowBlock ---
            ShadowBlock sb = hit.collider ? hit.collider.GetComponent<ShadowBlock>() : null;
            if (sb != null && !iluminadosEsteFrame.Contains(sb))
            {
                iluminadosEsteFrame.Add(sb);

                float distancia = Vector2.Distance(origen, hit.point);
                float normalizado = Mathf.Clamp01(distancia / alcance);
                float intensidad = curvaIntensidad.Evaluate(1f - normalizado);
                float daño = dañoBase * intensidad * Time.deltaTime;
                sb.RecibirLuz(daño, tipoLuz);

                // Si el bloque es reflectivo, le informamos del impacto
                if (sb is MirrorBlock reflect)
                {
                    reflect.RecibirLuz(dirRay, daño, tipoLuz, hit.normal, alcance, hit.point);
                }
                else
                {
                    sb.RecibirLuz(daño, tipoLuz);
                }
            }

            vertices.Add(transform.InverseTransformPoint(puntoMundo));
        }

        // --- Crear triángulos del cono ---
        for (int i = 1; i < vertices.Count - 1; i++)
        {
            triangles.Add(0);
            triangles.Add(i);
            triangles.Add(i + 1);
        }

        // --- Generar UVs para mostrar correctamente la textura ---
        List<Vector2> uvs = new List<Vector2>();

        // Calculamos los límites horizontales del cono en espacio local
        float halfWidth = alcance * Mathf.Tan(anguloCono * 0.5f * Mathf.Deg2Rad);

        // Usamos la rotación del cono para proyectar las coordenadas en un plano 2D
        Matrix4x4 localToWorld = transform.localToWorldMatrix;
        Quaternion rot = Quaternion.FromToRotation(Vector3.up, direccion.normalized);
        Matrix4x4 rotMatrix = Matrix4x4.Rotate(rot);

        // 🔸 Vertex 0: base del cono (origen)
        float origenV = invertirDegradado ? 0f : 1f;
        uvs.Add(new Vector2(0.5f + offsetU, origenV));

        // 🔸 Los demás vértices
        for (int i = 1; i < vertices.Count; i++)
        {
            Vector3 local = vertices[i];

            // Convertimos a "espacio del cono"
            Vector3 rotated = rotMatrix.MultiplyPoint3x4(local);

            // Eje vertical (Y) = distancia desde el origen / alcance
            float vTex = Mathf.Clamp01(rotated.y / alcance);
            if (invertirDegradado)
                vTex = 1f - vTex;

            // Eje horizontal (X) = posición relativa dentro del ancho total del cono
            float u = Mathf.InverseLerp(-halfWidth, halfWidth, rotated.x);
            u = (u - 0.5f) / multiplicadorAnchoUV + 0.5f + offsetU;

            uvs.Add(new Vector2(u, vTex));
        }




        // --- Aplicar datos al mesh ---
        mesh.Clear();
        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangles, 0);
        mesh.SetUVs(0, uvs);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        // --- Apagar bloques fuera de luz ---
        foreach (var sbAnt in iluminadosPrev)
        {
            if (sbAnt == null) continue;
            if (!iluminadosEsteFrame.Contains(sbAnt))
                sbAnt.SalirDeLuz();
        }

        iluminadosPrev = new HashSet<ShadowBlock>(iluminadosEsteFrame);
    }

    public void SetDireccion(Vector2 nuevaDir) => direccion = nuevaDir.normalized;
    public void SetAngulo(float a) => anguloCono = a;
    public void SetAlcance(float r) => alcance = r;

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
            Gizmos.DrawRay(origen, dirRay * alcance);
        }
    }
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
        // 🔹 Cambia automáticamente el material y daño
        if (meshRenderer == null)
            meshRenderer = GetComponent<MeshRenderer>();

        if (tipoLuz == TipoLuz.Roja)
        {
            meshRenderer.material = materialRoja;
            dañoBase = 10f; // podés ajustar este valor
        }
        else
        {
            meshRenderer.material = materialAmarilla;
            dañoBase = 1f;
        }

        // 🔹 Actualiza color de Gizmos inmediatamente en el editor
#if UNITY_EDITOR
    UnityEditor.SceneView.RepaintAll();
#endif
    }
}
