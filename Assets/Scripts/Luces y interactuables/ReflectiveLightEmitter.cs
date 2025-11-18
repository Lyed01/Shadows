using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class ReflectiveLightEmitter : MonoBehaviour
{
    [Header("Configuración general")]
    public SpotLightDetector.TipoLuz tipoLuz = SpotLightDetector.TipoLuz.Amarilla;
    public Vector2 direccion = Vector2.right;
    public float alcance = 6f;
    public float ancho = 0.25f;
    public float dañoBase = 1f;
    public AnimationCurve curvaIntensidad = AnimationCurve.EaseInOut(0, 1, 1, 0);
    public LayerMask mascaraBloqueos;

    [Header("Material visual (usar el mismo que el spotlight)")]
    public Material materialLuz;

    [Header("Ajuste UV")]
    public bool uvRotar90 = false;
    public bool flipU = false;
    public bool flipV = false;
    public float escalaULargo = 1f;
    public float escalaVAncho = 1f;

    private MeshFilter mf;
    private MeshRenderer mr;
    private Mesh mesh;
    private Color colorLuz;

    void Awake()
    {
        mf = GetComponent<MeshFilter>();
        mr = GetComponent<MeshRenderer>();

        mesh = new Mesh { name = "ReflectiveLightMesh" };
        mf.mesh = mesh;

        mr.material = materialLuz != null ? materialLuz : new Material(Shader.Find("Sprites/Default"));
        mr.sortingLayerName = "Default";
        mr.sortingOrder = 100;

        ActualizarColor();
    }

    void Update() => ActualizarHaz();

    private void ActualizarColor()
    {
        colorLuz = (tipoLuz == SpotLightDetector.TipoLuz.Roja)
            ? new Color(1f, 0.2f, 0.2f, 1f)
            : new Color(1f, 1f, 0.6f, 1f);

        if (mr.material != null)
            mr.material.color = colorLuz;
    }

    private void ActualizarHaz()
    {
        Vector2 origen = transform.position;
        Vector2 dir = direccion.normalized;

        // === RAYCAST ===
        Collider2D parentCol = transform.parent ? transform.parent.GetComponent<Collider2D>() : null;

        // Usamos RaycastAll para ignorar el bloque que emite y quedarnos con el siguiente válido
        RaycastHit2D[] hits = Physics2D.RaycastAll(origen + dir * 0.02f, dir, alcance, mascaraBloqueos);

        RaycastHit2D hitElegido = new RaycastHit2D();
        bool hayHitValido = false;

        for (int i = 0; i < hits.Length; i++)
        {
            var h = hits[i];
            if (h.collider == null) continue;

            // Ignorar el bloque padre (el emisor)
            if (parentCol != null && h.collider == parentCol) continue;

            // Ignorar otros reflectores (para evitar bucles infinitos)
            if (h.collider.GetComponent<ReflectiveLightEmitter>() != null) continue;

            // Primer impacto válido encontrado
            hitElegido = h;
            hayHitValido = true;
            break;
        }

        float distanciaReal = hayHitValido
            ? Vector2.Distance(origen, hitElegido.point)
            : alcance;

#if UNITY_EDITOR
    Color c = hayHitValido ? Color.red : Color.yellow;
    Debug.DrawRay(origen, dir * distanciaReal, c, 0.05f);
#endif

        if (hayHitValido)
        {
            // 💀 Golpea al jugador → muerte
            Jugador j = hitElegido.collider.GetComponent<Jugador>();
            if (j != null)
                j.Matar();

            // 💡 Golpea un receptor de luz
            LightReceptor receptor = hitElegido.collider.GetComponent<LightReceptor>();
            if (receptor != null)
            {
                receptor.RecibirLuz(tipoLuz);
            }

            // 🧱 Golpea un bloque de sombra (normal o reflectivo)
            ShadowBlock bloque = hitElegido.collider.GetComponent<ShadowBlock>();
            if (bloque != null)
            {
                float normalizado = Mathf.Clamp01(distanciaReal / alcance);
                float intensidad = curvaIntensidad.Evaluate(1f - normalizado);
                float daño = dañoBase * intensidad * Time.deltaTime;

                bloque.RecibirLuz(daño, tipoLuz);

                // Si es reflectivo → propaga el haz reflejado
                MirrorBlock mirror = bloque as MirrorBlock;
                if (mirror != null)
                    mirror.RecibirLuz(dir, daño, tipoLuz, hitElegido.normal, alcance, hitElegido.point);
            }
        }

        // === GEOMETRÍA DEL HAZ ===
        float mitadAncho = ancho * 0.5f;
        Vector3[] vertices = new Vector3[4];
        vertices[0] = new Vector3(0, mitadAncho, 0);
        vertices[1] = new Vector3(0, -mitadAncho, 0);
        vertices[2] = new Vector3(distanciaReal, mitadAncho, 0);
        vertices[3] = new Vector3(distanciaReal, -mitadAncho, 0);

        int[] triangles = { 0, 2, 1, 2, 3, 1 };

        // UVs
        float u0 = 0f;
        float u1 = (distanciaReal / Mathf.Max(0.0001f, alcance)) * Mathf.Max(0.0001f, escalaULargo);
        float vTop = 1f * Mathf.Max(0.0001f, escalaVAncho);
        float vBottom = 0f;

        if (flipU) (u0, u1) = (u1, u0);
        if (flipV) (vTop, vBottom) = (vBottom, vTop);

        Vector2 uv0 = new Vector2(u0, vTop);
        Vector2 uv1 = new Vector2(u0, vBottom);
        Vector2 uv2 = new Vector2(u1, vTop);
        Vector2 uv3 = new Vector2(u1, vBottom);

        if (uvRotar90)
        {
            uv0 = new Vector2(vTop, u0);
            uv1 = new Vector2(vBottom, u0);
            uv2 = new Vector2(vTop, u1);
            uv3 = new Vector2(vBottom, u1);
        }

        if (transform.parent)
            transform.position = transform.parent.position;

        float angulo = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        mr.material.color = colorLuz;

        Matrix4x4 matriz = Matrix4x4.TRS(Vector3.zero, Quaternion.Euler(0, 0, angulo), Vector3.one);
        Vector3[] verticesRotados = new Vector3[4];
        for (int i = 0; i < vertices.Length; i++)
            verticesRotados[i] = matriz.MultiplyPoint3x4(vertices[i]);

        mesh.Clear();
        mesh.vertices = verticesRotados;
        mesh.triangles = triangles;
        mesh.uv = new Vector2[] { uv0, uv1, uv2, uv3 };
        mesh.RecalculateBounds();
    }

    // === API ===
    public void SetDireccion(Vector2 nuevaDir) => direccion = nuevaDir.normalized;
    public void SetParametros(float nuevoAlcance, float nuevoAncho)
    {
        alcance = nuevoAlcance;
        ancho = nuevoAncho;
    }

    public void SetTipoLuz(SpotLightDetector.TipoLuz nuevoTipo)
    {
        tipoLuz = nuevoTipo;
        ActualizarColor();
    }
}
