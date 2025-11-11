using UnityEngine;
using System.Collections.Generic;

[ExecuteAlways]
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class TopLightDetector : MonoBehaviour
{
    public enum TipoLuz { Amarilla, Roja }

    [Header("Configuración General")]
    public TipoLuz tipoLuz = TipoLuz.Amarilla;

    [Header("Movimiento")]
    [Tooltip("Puntos de patrulla entre los que se moverá la luz")]
    public Transform[] puntosPatrulla;
    [Tooltip("Velocidad del movimiento")]
    public float velocidadMovimiento = 2f;
    [Tooltip("Si está activo, patrulla ida y vuelta")]
    public bool idaYVuelta = true;
    private int indiceObjetivo = 0;
    private bool retrocediendo = false;

    [Header("Parámetros del haz")]
    public float radio = 4f;          // radio del círculo
    [Range(10, 100)] public int resolucion = 32;
    public float dañoBase = 1f;
    public AnimationCurve curvaIntensidad = AnimationCurve.EaseInOut(0, 1, 1, 0);

    [Header("Capas")]
    public LayerMask mascaraBloqueos;

    [Header("Materiales")]
    public Material materialAmarilla;
    public Material materialRoja;

    [Header("Titileo / Apagones")]
    public bool titilar = false;
    public Vector2 tiempoEncendida = new Vector2(2f, 4f);
    public Vector2 tiempoApagada = new Vector2(0.3f, 1.2f);
    private float timerTitileo = 0f;
    private bool luzEncendida = true;

    // Internos
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
            mesh = new Mesh { name = "TopLightMesh" };
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

        // 🔹 Patrulla entre puntos
        if (puntosPatrulla != null && puntosPatrulla.Length > 1)
            MoverEntrePuntos();

        // 🔹 Titileo
        ActualizarTitileo();

        // 🔹 Generar mesh solo si está encendida
        if (luzEncendida)
            GenerarLuzCircular();
    }

    // --- 🔸 Movimiento tipo patrulla ---
    private void MoverEntrePuntos()
    {
        if (puntosPatrulla.Length == 0) return;

        Transform objetivo = puntosPatrulla[indiceObjetivo];
        transform.position = Vector2.MoveTowards(transform.position, objetivo.position, velocidadMovimiento * Time.deltaTime);

        // Si llegó al objetivo
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
            else
            {
                indiceObjetivo = (indiceObjetivo + 1) % puntosPatrulla.Length;
            }
        }
    }

    // --- 🔸 Titileo (igual que el spotlight) ---
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

    // --- 🔸 Generar mesh circular ---
    private void GenerarLuzCircular()
    {
        Vector2 origen = transform.position;

        List<Vector3> vertices = new() { Vector3.zero };
        List<int> triangles = new();
        List<Vector2> uvs = new();

        HashSet<ShadowBlock> iluminadosEsteFrame = new();

        for (int i = 0; i <= resolucion; i++)
        {
            float ang = i * Mathf.PI * 2f / resolucion;
            Vector2 dir = new(Mathf.Cos(ang), Mathf.Sin(ang));
            RaycastHit2D hit = Physics2D.Raycast(origen, dir, radio, mascaraBloqueos);

            Vector2 punto = hit.collider ? hit.point : origen + dir * radio;

            if (hit.collider)
            {
                if (hit.collider.TryGetComponent(out Jugador j))
                    j.Matar();

                if (hit.collider.TryGetComponent(out ShadowBlock sb))
                {
                    float dist = Vector2.Distance(origen, punto);
                    float intensidad = curvaIntensidad.Evaluate(1f - dist / radio);
                    sb.RecibirLuz(dañoBase * intensidad * Time.deltaTime, tipoLuz);
                    iluminadosEsteFrame.Add(sb);
                }
            }

            vertices.Add(transform.InverseTransformPoint(punto));
        }

        // Triángulos
        for (int i = 1; i < vertices.Count - 1; i++)
        {
            triangles.Add(0);
            triangles.Add(i);
            triangles.Add(i + 1);
        }

        // UVs radiales
        for (int i = 0; i < vertices.Count; i++)
        {
            Vector3 v = vertices[i].normalized * 0.5f + Vector3.one * 0.5f;
            uvs.Add(new Vector2(v.x, v.y));
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

    // --- 🔸 Cambiar tipo de luz ---
    public void SetTipoLuz(TipoLuz nuevoTipo)
    {
        tipoLuz = nuevoTipo;
        if (meshRenderer == null)
            meshRenderer = GetComponent<MeshRenderer>();

        meshRenderer.sharedMaterial = (tipoLuz == TipoLuz.Roja) ? materialRoja : materialAmarilla;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = (tipoLuz == TipoLuz.Roja) ? Color.red : Color.yellow;
        Gizmos.DrawWireSphere(transform.position, radio);
    }
}
