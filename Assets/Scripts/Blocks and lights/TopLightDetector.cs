using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Rendering.Universal;

[ExecuteAlways]
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class TopLightDetector : MonoBehaviour
{
    public enum TipoLuz { Amarilla, Roja }

    [Header("Configuración General")]
    public TipoLuz tipoLuz = TipoLuz.Amarilla;

    [Header("Movimiento")]
    public Transform[] puntosPatrulla;
    public float velocidadMovimiento = 2f;
    public bool idaYVuelta = true;
    private int indiceObjetivo = 0;
    private bool retrocediendo = false;

    [Header("Parámetros del haz")]
    public float radio = 4f;
    [Range(10, 100)] public int resolucion = 32;
    public float dañoBase = 1f;
    public AnimationCurve curvaIntensidad = AnimationCurve.EaseInOut(0, 1, 1, 0);

    [Header("Capas")]
    public LayerMask mascaraBloqueos;

    [Header("Materiales Mesh")]
    public Material materialAmarilla;
    public Material materialRoja;

    [Header("Titileo / Apagones")]
    public bool titilar = false;
    public Vector2 tiempoEncendida = new Vector2(2f, 4f);
    public Vector2 tiempoApagada = new Vector2(0.3f, 1.2f);
    private float timerTitileo = 0f;
    private bool luzEncendida = true;

    // -------------------------
    // NUEVO: LÁMPARA SPRITE
    // -------------------------
    [Header("Lámpara visual")]
    public Sprite lampSprite;
    public Vector3 lampOffset = Vector3.zero;
    public float lampScale = 1f;

    private SpriteRenderer lampRenderer;

    // -------------------------
    // NUEVO: LUZ 2D
    // -------------------------
    [Header("Luz 2D como Spotlight")]
    public bool usarLuz2D = true;
    [Range(0f, 2f)] public float intensidadLuz2D = 1f;
    public float multiplicadorRadioLuz = 1.1f;
    private Light2D luz2D;

    // Internos mesh
    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;
    private Mesh mesh;

    private HashSet<ShadowBlock> iluminadosPrev = new();

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
        else
        {
            mesh = meshFilter.sharedMesh;
        }

        // CREAR LÁMPARA
        CrearLampara();

        // CREAR LUZ 2D
        if (usarLuz2D)
            CrearLuz2D();
    }

    // ============================================================
    void Update()
    {
        if (!Application.isPlaying) return;

        meshRenderer.sharedMaterial = (tipoLuz == TipoLuz.Roja) ? materialRoja : materialAmarilla;

        // Patrulla
        if (puntosPatrulla != null && puntosPatrulla.Length > 1)
            MoverEntrePuntos();

        // Titileo
        ActualizarTitileo();

        // Generar mesh
        if (luzEncendida)
            GenerarLuzCircular();

        // Actualizar luz2D intensidad
        if (usarLuz2D && luz2D != null)
            luz2D.intensity = luzEncendida ? intensidadLuz2D : 0f;

        // Actualizar lámpara encendido
        if (lampRenderer != null)
            lampRenderer.enabled = luzEncendida;

        // Mantener lámpara en posición
        if (lampRenderer != null)
            lampRenderer.transform.localPosition = lampOffset;
    }

    // ============================================================
    // MOVIMIENTO
    // ============================================================
    private void MoverEntrePuntos()
    {
        Transform objetivo = puntosPatrulla[indiceObjetivo];
        transform.position = Vector2.MoveTowards(transform.position, objetivo.position, velocidadMovimiento * Time.deltaTime);

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

    // ============================================================
    // TITILEO
    // ============================================================
    private void ActualizarTitileo()
    {
        if (!titilar)
        {
            meshRenderer.enabled = true;
            luzEncendida = true;
            return;
        }

        timerTitileo -= Time.deltaTime;

        if (timerTitileo <= 0f)
        {
            luzEncendida = !luzEncendida;
            timerTitileo = luzEncendida ?
                Random.Range(tiempoEncendida.x, tiempoEncendida.y) :
                Random.Range(tiempoApagada.x, tiempoApagada.y);
        }

        meshRenderer.enabled = luzEncendida;
    }

    // ============================================================
    // MESH CIRCULAR
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

            if (hit.collider)
            {
                if (hit.collider.TryGetComponent(out Jugador j))
                    j.Matar();

                if (hit.collider.TryGetComponent(out ShadowBlock sb))
                {
                    float dist = Vector2.Distance(origen, punto);
                    float intensidad = curvaIntensidad.Evaluate(1f - dist / radio);
                    sb.RecibirLuz(dañoBase * intensidad * Time.deltaTime);
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

        // Salir de luz
        foreach (var sb in iluminadosPrev)
            if (!iluminadosEsteFrame.Contains(sb))
                sb.SalirDeLuz();

        iluminadosPrev = new(iluminadosEsteFrame);

        // Actualizar luz 2D
        ActualizarFormaLuz2D();
    }

    // ============================================================
    // LUZ 2D FREEFORM CIRCULAR
    // ============================================================
    private void CrearLuz2D()
    {
        var existentes = GetComponentsInChildren<Light2D>(true);
        foreach (var l in existentes)
        {
            if (Application.isPlaying)
                Destroy(l.gameObject);
            else
                DestroyImmediate(l.gameObject);
        }

        GameObject luzObj = new("Luz2D_TopLight");
        luzObj.transform.SetParent(transform);
        luzObj.transform.localPosition = Vector3.zero;

        luz2D = luzObj.AddComponent<Light2D>();
        luz2D.lightType = Light2D.LightType.Freeform;
        luz2D.shadowIntensity = 0.2f;
        luz2D.falloffIntensity = 0.35f;
        luz2D.intensity = intensidadLuz2D;

        ActualizarColorLuz2D();
        ActualizarFormaLuz2D();
    }

    private void ActualizarFormaLuz2D()
    {
        if (luz2D == null) return;

        int puntos = Mathf.Clamp(resolucion, 16, 256);
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

        luz2D.color = (tipoLuz == TipoLuz.Roja)
            ? new Color(1f, 0.25f, 0.2f)
            : new Color(1f, 0.95f, 0.7f);
    }

    // ============================================================
    // LÁMPARA SPRITE
    // ============================================================
    private void CrearLampara()
    {
        if (lampSprite == null) return;

        Transform existing = transform.Find("LampSprite");
        if (existing != null)
        {
            lampRenderer = existing.GetComponent<SpriteRenderer>();
            return;
        }

        GameObject lampObj = new("LampSprite");
        lampObj.transform.SetParent(transform);
        lampObj.transform.localPosition = lampOffset;

        lampRenderer = lampObj.AddComponent<SpriteRenderer>();
        lampRenderer.sprite = lampSprite;
        lampRenderer.sortingLayerID = meshRenderer.sortingLayerID;
        lampRenderer.sortingOrder = meshRenderer.sortingOrder + 5;

        lampObj.transform.localScale = Vector3.one * lampScale;

        // Color inicial
        if (tipoLuz == TipoLuz.Roja)
            lampRenderer.color = new Color(1f, 0.4f, 0.4f);
        else
            lampRenderer.color = new Color(1f, 1f, 0.85f);
    }

    // ============================================================
    // CAMBIO DE TIPO DE LUZ
    // ============================================================
    public void SetTipoLuz(TipoLuz nuevoTipo)
    {
        tipoLuz = nuevoTipo;
        meshRenderer.sharedMaterial = (tipoLuz == TipoLuz.Roja) ? materialRoja : materialAmarilla;

        ActualizarColorLuz2D();

        if (lampRenderer != null)
        {
            lampRenderer.color = (tipoLuz == TipoLuz.Roja)
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
        Gizmos.color = (tipoLuz == TipoLuz.Roja) ? Color.red : Color.yellow;
        Gizmos.DrawWireSphere(transform.position, radio);
    }
}
