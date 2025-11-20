using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Rendering.Universal;

[ExecuteAlways]
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class TopLightDetector : MonoBehaviour
{
    // Usamos el MISMO enum que el Spotlight
    public SpotLightDetector.TipoLuz tipoLuz = SpotLightDetector.TipoLuz.Amarilla;

    // ============================================================
    // CONFIGURACIÓN GENERAL
    // ============================================================
    [Header("Configuración General")]
    public float dañoBase = 1f;
    public AnimationCurve curvaIntensidad = AnimationCurve.EaseInOut(0, 1, 1, 0);

    [Header("Capas de impacto")]
    public LayerMask mascaraBloqueos;

    // ============================================================
    // MOVIMIENTO ENTRE PUNTOS
    // ============================================================
    [Header("Movimiento en Patrulla")]
    public Transform[] puntosPatrulla;
    public float velocidadMovimiento = 2f;
    public bool idaYVuelta = true;

    private int indiceObjetivo = 0;
    private bool retrocediendo = false;

    // ============================================================
    // PARÁMETROS DEL HAZ (CIRCULAR)
    // ============================================================
    [Header("Haz Cenital Circular")]
    public float radio = 4f;
    [Range(12, 128)] public int resolucion = 48;

    // ============================================================
    // MATERIAL VISUAL
    // ============================================================
    [Header("Materiales Mesh")]
    public Material materialAmarilla;
    public Material materialRoja;

    // ============================================================
    // TITILEO / APAGONES
    // ============================================================
    [Header("Titileo")]
    public bool titilar = false;
    public Vector2 tiempoEncendida = new(2f, 4f);
    public Vector2 tiempoApagada = new(0.3f, 1.2f);

    private bool luzEncendida = true;
    private float timerTitileo;

    // ============================================================
    // LÁMPARA VISUAL
    // ============================================================
    [Header("Sprite Lámpara")]
    public Sprite lampSprite;
    public Vector3 lampOffset = Vector3.zero;
    public float lampScale = 1f;

    private SpriteRenderer lampRenderer;

    // ============================================================
    // LUZ 2D
    // ============================================================
    [Header("Luz 2D")]
    public bool usarLuz2D = true;
    [Range(0f, 2f)] public float intensidadLuz2D = 0.8f;
    public float multiplicadorRadioLuz = 1.1f;

    private Light2D luz2D;

    // ============================================================
    // INTERNOS MESH
    // ============================================================
    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;
    private Mesh mesh;

    private HashSet<ShadowBlock> iluminadosPrev = new();

    // ============================================================
    // AWAKE
    // ============================================================
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

        if (usarLuz2D)
            CrearLuz2D();
    }

    // ============================================================
    // UPDATE
    // ============================================================
    void Update()
    {
        if (!Application.isPlaying)
            return;

        meshRenderer.sharedMaterial =
            tipoLuz == SpotLightDetector.TipoLuz.Roja ? materialRoja : materialAmarilla;

        ActualizarMovimiento();
        ActualizarTitileo();

        if (luzEncendida)
            GenerarLuzCircular();

        if (usarLuz2D && luz2D != null)
            luz2D.intensity = luzEncendida ? intensidadLuz2D : 0f;

        if (lampRenderer != null)
        {
            lampRenderer.enabled = luzEncendida;
            lampRenderer.transform.localPosition = lampOffset;
        }
    }

    // ============================================================
    // MOVIMIENTO ENTRE PUNTOS
    // ============================================================
    private void ActualizarMovimiento()
    {
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

    // ============================================================
    // TITILEO
    // ============================================================
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
    }

    // ============================================================
    // LÓGICA PRINCIPAL DEL HAZ CIRCULAR
    // ============================================================
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

            // --- JUGADOR ---
            if (hit.collider && hit.collider.TryGetComponent(out Jugador j))
                j.Matar();

            // --- BLOQUES ---
            if (hit.collider && hit.collider.TryGetComponent(out ShadowBlock sb))
            {
                float dist = Vector2.Distance(origen, punto);
                float intensidad = curvaIntensidad.Evaluate(1f - dist / radio);
                float daño = dañoBase * intensidad * Time.deltaTime;

                // LUZ ROJA DESTRUYE
                if (tipoLuz == SpotLightDetector.TipoLuz.Roja)
                {
                    sb.RecibirLuz(9999f, tipoLuz);
                }
                else
                {
                    sb.RecibirLuz(daño, tipoLuz);
                    iluminadosEsteFrame.Add(sb);
                }

                // MirrorBlock → lo agregamos en la próxima versión
                // (si querés ya mismo te lo integro)
            }

            vertices.Add(transform.InverseTransformPoint(punto));
        }

        // Triángulos del mesh
        for (int i = 1; i < vertices.Count - 1; i++)
        {
            triangles.Add(0);
            triangles.Add(i);
            triangles.Add(i + 1);
        }

        // UVs
        foreach (var v in vertices)
        {
            Vector3 nv = v.normalized * 0.5f + Vector3.one * 0.5f;
            uvs.Add(new Vector2(nv.x, nv.y));
        }

        mesh.Clear();
        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangles, 0);
        mesh.SetUVs(0, uvs);
        mesh.RecalculateBounds();

        // Salir de la luz
        foreach (var sb in iluminadosPrev)
            if (!iluminadosEsteFrame.Contains(sb))
                sb.SalirDeLuz();

        iluminadosPrev = new(iluminadosEsteFrame);

        ActualizarFormaLuz2D();
    }

    // ============================================================
    // LUZ 2D
    // ============================================================
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
        ActualizarFormaLuz2D();
    }

    private void ActualizarFormaLuz2D()
    {
        if (luz2D == null) return;

        int puntos = Mathf.Clamp(resolucion, 12, 256);
        Vector3[] shape = new Vector3[puntos];

        float r = radio * multiplicadorRadioLuz;

        for (int i = 0; i < puntos; i++)
        {
            float ang = i * Mathf.PI * 2f / puntos;
            shape[i] = new Vector3(Mathf.Cos(ang) * r, Mathf.Sin(ang) * r, 0f);
        }

        luz2D.SetShapePath(shape);
    }

    private void ActualizarColorLuz2D()
    {
        if (luz2D == null) return;

        luz2D.color = tipoLuz == SpotLightDetector.TipoLuz.Roja
            ? new Color(1f, 0.2f, 0.2f)
            : new Color(1f, 0.95f, 0.7f);
    }

    // ============================================================
    // LÁMPARA VISUAL
    // ============================================================
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

        if (tipoLuz == SpotLightDetector.TipoLuz.Roja)
            lampRenderer.color = new Color(1f, 0.4f, 0.4f);
        else
            lampRenderer.color = new Color(1f, 1f, 0.85f);
    }

    // ============================================================
    // CAMBIO DE TIPO DE LUZ
    // ============================================================
    public void SetTipoLuz(SpotLightDetector.TipoLuz nuevoTipo)
    {
        tipoLuz = nuevoTipo;

        meshRenderer.sharedMaterial =
            tipoLuz == SpotLightDetector.TipoLuz.Roja ? materialRoja : materialAmarilla;

        ActualizarColorLuz2D();

        if (lampRenderer != null)
        {
            lampRenderer.color =
                tipoLuz == SpotLightDetector.TipoLuz.Roja
                ? new Color(1f, 0.4f, 0.4f)
                : new Color(1f, 1f, 0.85f);
        }

#if UNITY_EDITOR
        UnityEditor.SceneView.RepaintAll();
#endif
    }

    // ============================================================
    void OnDrawGizmos()
    {
        Gizmos.color =
            tipoLuz == SpotLightDetector.TipoLuz.Roja ? Color.red : Color.yellow;

        Gizmos.DrawWireSphere(transform.position, radio);
    }
}
