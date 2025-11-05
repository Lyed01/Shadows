using UnityEngine;
using System.Collections.Generic;

[ExecuteAlways]
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class CircularSpotLight : MonoBehaviour
{
    public enum TipoLuz { Amarilla, Roja }

    [Header("Configuración General")]
    public TipoLuz tipoLuz = TipoLuz.Amarilla;
    public bool seguirJugador = false;
    public Transform jugador; // opcional, si seguirJugador=true

    [Header("Parámetros del círculo")]
    public float radio = 5f;
    public int segmentos = 50;

    [Header("Daño / Intensidad")]
    public float dañoBase = 1f;
    public AnimationCurve curvaIntensidad = AnimationCurve.EaseInOut(0, 1, 1, 0);

    [Header("Debug / Capas")]
    public LayerMask mascaraBloqueos; // ShadowBlocks

    [Header("Movimiento Predefinido")]
    public Transform[] waypoints;
    public float velocidadMovimiento = 2f;
    public bool loopWaypoints = true;
    private int waypointIndex = 0;
    private int direccionWaypoint = 1; // 1 = adelante, -1 = atrás

    private MeshFilter meshFilter;
    private Mesh mesh;
    private HashSet<ShadowBlock> iluminadosPrev = new HashSet<ShadowBlock>();

    void Awake()
    {
        meshFilter = GetComponent<MeshFilter>();
        mesh = new Mesh();
        mesh.name = "CircularSpotLightMesh";
        meshFilter.mesh = mesh;

        MeshRenderer mr = GetComponent<MeshRenderer>();
        if (mr.sharedMaterial == null)
            mr.sharedMaterial = new Material(Shader.Find("Sprites/Default"))
            { color = (tipoLuz == TipoLuz.Roja ? Color.red : Color.yellow) };
    }

    void Update()
    {
        if (!Application.isPlaying) return;

        // --- Movimiento ---
        if (seguirJugador && jugador != null)
        {
            transform.position = jugador.position;
        }
        else if (waypoints != null && waypoints.Length > 0)
        {
            Transform target = waypoints[waypointIndex];
            transform.position = Vector3.MoveTowards(transform.position, target.position, velocidadMovimiento * Time.deltaTime);

            if (Vector3.Distance(transform.position, target.position) < 0.05f)
            {
                waypointIndex += direccionWaypoint;

                if (loopWaypoints)
                {
                    if (waypointIndex >= waypoints.Length)
                    {
                        waypointIndex = waypoints.Length - 2;
                        direccionWaypoint = -1;
                    }
                    else if (waypointIndex < 0)
                    {
                        waypointIndex = 1;
                        direccionWaypoint = 1;
                    }
                }
                else
                {
                    waypointIndex = Mathf.Clamp(waypointIndex, 0, waypoints.Length - 1);
                }
            }
        }

        Vector2 origen = transform.position;

        // --- Detectar jugador por tag "Player" ---
        Collider2D[] colJugadores = Physics2D.OverlapCircleAll(origen, radio);
        foreach (var col in colJugadores)
        {
            if (col.CompareTag("Player"))
            {
                Jugador j = col.GetComponent<Jugador>();
                if (j != null) j.Matar();
            }
        }

        // --- Detectar ShadowBlocks ---
        Collider2D[] hits = Physics2D.OverlapCircleAll(origen, radio, mascaraBloqueos);
        HashSet<ShadowBlock> iluminadosEsteFrame = new HashSet<ShadowBlock>();

        foreach (var col in hits)
        {
            ShadowBlock sb = col.GetComponent<ShadowBlock>();
            if (sb != null)
            {
                iluminadosEsteFrame.Add(sb);
                float distancia = Vector2.Distance(origen, sb.transform.position);
                float intensidad = curvaIntensidad.Evaluate(1f - Mathf.Clamp01(distancia / radio));

                if (tipoLuz == TipoLuz.Roja)
                    sb.RecibirLuz(float.MaxValue);
                else
                    sb.RecibirLuz(dañoBase * intensidad * Time.deltaTime);
            }
        }

        // Apagar bloques que ya no están bajo luz
        foreach (var sbAnt in iluminadosPrev)
            if (sbAnt != null && !iluminadosEsteFrame.Contains(sbAnt))
                sbAnt.SalirDeLuz();

        iluminadosPrev = new HashSet<ShadowBlock>(iluminadosEsteFrame);

        // --- Generar mesh circular ---
        GenerarMesh();
    }

    void GenerarMesh()
    {
        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();
        vertices.Add(Vector3.zero);

        for (int i = 0; i <= segmentos; i++)
        {
            float ang = i * Mathf.PI * 2 / segmentos;
            vertices.Add(new Vector3(Mathf.Cos(ang), Mathf.Sin(ang), 0) * radio);
        }

        for (int i = 1; i <= segmentos; i++)
        {
            triangles.Add(0);
            triangles.Add(i);
            triangles.Add(i + 1);
        }

        mesh.Clear();
        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = (tipoLuz == TipoLuz.Roja) ? Color.red : Color.yellow;
        Gizmos.DrawWireSphere(transform.position, radio);
    }
}
