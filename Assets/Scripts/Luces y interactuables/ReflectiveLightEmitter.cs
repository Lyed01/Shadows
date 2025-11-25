using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class ReflectiveLightEmitter : MonoBehaviour
{
    public SpotLightDetector.TipoLuz tipoLuz = SpotLightDetector.TipoLuz.Amarilla;
    public Vector2 direccion = Vector2.right;
    public float alcance = 6f;
    public float ancho = 0.25f;
    public float dañoBase = 1f;
    public AnimationCurve curvaIntensidad = AnimationCurve.EaseInOut(0, 1, 1, 0);
    public LayerMask mascaraBloqueos;
    public Material materialLuz;

    private MeshFilter mf;
    private MeshRenderer mr;
    private Mesh mesh;
    private Collider2D padreCol;

    void Awake()
    {
        mf = GetComponent<MeshFilter>();
        mr = GetComponent<MeshRenderer>();
        padreCol = transform.parent.GetComponent<Collider2D>();

        mesh = new Mesh();
        mf.mesh = mesh;

        if (materialLuz == null)
            materialLuz = new Material(Shader.Find("Sprites/Default"));

        mr.material = materialLuz;
        mr.sortingOrder = 300;
    }

    void Update()
    {
        GenerarRayo();
    }

    private void GenerarRayo()
    {
        if (transform.parent == null) return;

        Vector2 dir = direccion.normalized;
        Collider2D col = padreCol;

        // --- ORIGEN EXACTO DESDE EL BORDE DEL MIRRORBLOCK ---
        Vector2 ext = col.bounds.extents;

        Vector2 borde = new Vector2(
            Mathf.Abs(dir.x) > Mathf.Abs(dir.y) ? Mathf.Sign(dir.x) * ext.x : 0,
            Mathf.Abs(dir.y) > Mathf.Abs(dir.x) ? Mathf.Sign(dir.y) * ext.y : 0
        );

        // ❗ IMPORTANTE: NO mover el transform, solo usar borde para el raycast
        // (esto arregla el mesh recortado y el rayo desalineado)

        Vector2 origen = (Vector2)col.bounds.center + borde;


        // --- RAYCAST IDENTICO AL SPOTLIGHT ---
        RaycastHit2D hit = new RaycastHit2D();
        float minDist = alcance;
        bool hayHit = false;

        RaycastHit2D[] hits = Physics2D.RaycastAll(origen, dir, alcance, mascaraBloqueos);

        foreach (var h in hits)
        {
            if (h.collider == null) continue;
            if (h.collider == col) continue;  // ignorar al padre
            if (h.collider.GetComponent<ReflectiveLightEmitter>() != null) continue;

            float d = h.distance; // distancia REAL del raycast

            if (d < minDist)
            {
                minDist = d;
                hit = h;
                hayHit = true;
            }
        }

        float dist = minDist;
        Vector2 punto = hayHit ? hit.point : origen + dir * alcance;


#if UNITY_EDITOR
    Debug.DrawLine(origen, punto, hayHit ? Color.red : Color.yellow);
#endif



        // --- LÓGICA DE IMPACTO ---
        if (hayHit)
        {
            if (hit.collider.TryGetComponent(out Jugador j))
                j.Matar();

            if (hit.collider.TryGetComponent(out LightReceptor rec))
                rec.RecibirLuz(tipoLuz);

            if (hit.collider.TryGetComponent(out ShadowBlock sb))
            {
                float intensidad = curvaIntensidad.Evaluate(1f - dist / alcance);
                float daño = dañoBase * intensidad * Time.deltaTime;

                sb.RecibirLuz(daño, tipoLuz);

                if (sb is MirrorBlock m)
                    m.RecibirLuz(dir, daño, tipoLuz, hit.normal, alcance, punto);
            }
        }



        // --- MESH EXACTO AL RAYO ---
        float half = ancho * 0.5f;

        Vector3[] verts = new Vector3[]
        {
        new Vector3(0,     half, 0),
        new Vector3(0,    -half, 0),
        new Vector3(dist,  half, 0),
        new Vector3(dist, -half, 0)
        };

        int[] tris = new int[] { 0, 2, 1, 2, 3, 1 };

        Vector2[] uvs = new Vector2[]
        {
        new Vector2(0,1),
        new Vector2(0,0),
        new Vector2(1,1),
        new Vector2(1,0)
        };

        float ang = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        Matrix4x4 rot = Matrix4x4.TRS(Vector3.zero, Quaternion.Euler(0, 0, ang), Vector3.one);

        Vector3 offsetLocal = (Vector3)borde;   // ← el borde en coordenadas locales

        Vector3[] rotados = new Vector3[4];
        for (int i = 0; i < 4; i++)
        {
            rotados[i] = offsetLocal + rot.MultiplyPoint3x4(verts[i]);
        }

        mesh.Clear();
        mesh.vertices = rotados;
        mesh.triangles = tris;
        mesh.uv = uvs;

        mesh.RecalculateBounds();
        mesh.RecalculateNormals();
    }


    // === API USADA POR MIRRORBLOCK ===
    public void SetDireccion(Vector2 d)
    {
        direccion = d.normalized;
    }

    public void SetParametros(float nuevoAlcance, float nuevoAncho)
    {
        alcance = nuevoAlcance;
        ancho = nuevoAncho;
    }

    public void SetTipoLuz(SpotLightDetector.TipoLuz nuevoTipo)
    {
        tipoLuz = nuevoTipo;

        mr.material.color = tipoLuz == SpotLightDetector.TipoLuz.Roja
            ? new Color(1f, 0.2f, 0.2f)
            : new Color(1f, 1f, 0.6f);
    }
}
