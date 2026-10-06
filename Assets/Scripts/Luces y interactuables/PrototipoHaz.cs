using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Banco de pruebas para rediseñar el haz de luz. No toca el sistema del juego:
/// vive en su propia escena para poder comparar formas una al lado de la otra.
///
/// El problema del haz actual es geometrico. Nace de un punto, asi que su ancho
/// a una distancia d vale 2*d*tan(angulo/2): con los 90 grados y los 8 de
/// alcance que usa el juego, el final del cono mide 16 unidades de ancho. Eso
/// no se arregla bajando el angulo, porque entonces queda una linea.
///
/// La forma Trapecio lo resuelve separando dos cosas que el cono confunde: de
/// que ancho SALE la luz (anchoBoca) y cuanto se ABRE despues (divergencia).
/// Un reflector real se comporta asi.
/// </summary>
[ExecuteAlways]
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class PrototipoHaz : MonoBehaviour
{
    public enum Forma
    {
        /// <summary>Lo que hay hoy: nace de un punto y se abre. Esta para comparar.</summary>
        ConoActual,

        /// <summary>Nace con ancho propio y se abre poco. Ancho final acotado.</summary>
        Trapecio,

        /// <summary>Trapecio con el centro marcado: el nucleo mata, el borde avisa.</summary>
        NucleoYHalo
    }

    [Header("Que forma probar")]
    public Forma forma = Forma.Trapecio;

    [Header("De donde sale la luz")]
    [Tooltip("La lampara. El haz nace de su boca, no del centro del objeto. " +
             "En las luces del juego es el LamparaPivot.")]
    public Transform lampara;

    [Tooltip("Tomar el ancho de la boca del sprite de la lampara en vez de escribirlo a mano.")]
    public bool anchoDesdeLaLampara = true;

    [Header("Haz")]
    public Vector2 direccion = Vector2.down;
    public float alcance = 8f;

    [Tooltip("Ancho de la boca, en unidades. Solo lo usan Trapecio y NucleoYHalo.")]
    public float anchoBoca = 1.5f;

    [Tooltip("Cuanto se abre el haz, en grados. El cono actual usa 90.")]
    [Range(0f, 90f)] public float divergencia = 8f;

    [Tooltip("Angulo del cono viejo, para la forma ConoActual.")]
    [Range(1f, 180f)] public float anguloCono = 90f;

    [Range(6, 120)] public int muestras = 40;

    [Tooltip("Cuanto se acortan los rayos de los costados. En 0 la punta del haz " +
             "queda recta; subiendo, el final se curva como el arco que dejaba el cono viejo.")]
    [Range(0f, 1f)] public float redondezPunta = 0.35f;

    [Header("Nucleo (la parte que mata)")]
    [Tooltip("Que fraccion del ancho es nucleo letal. El resto es borde de aviso.")]
    [Range(0.1f, 1f)] public float fraccionNucleo = 0.55f;

    [Header("Deteccion")]
    public LayerMask mascaraBloqueos = ~0;
    public bool dibujarGizmos = true;

    private MeshFilter meshFilter;
    private Mesh mesh;

    /// <summary>
    /// De donde sale la luz: la boca de la lampara si hay una asignada. Que el
    /// haz nazca en el sprite y no en el centro del objeto es lo que hace que
    /// la luz se vea salir de la lampara.
    /// </summary>
    public Vector2 Origen => lampara != null ? (Vector2)lampara.position : (Vector2)transform.position;

    /// <summary>
    /// Ancho de la boca. Si se pide, sale del sprite mas ancho que cuelgue de
    /// la lampara, asi el haz arranca exactamente del tamaño del asset.
    /// </summary>
    public float AnchoDeLaBoca
    {
        get
        {
            if (!anchoDesdeLaLampara || lampara == null)
                return anchoBoca;

            float masAncho = 0f;
            foreach (SpriteRenderer sr in lampara.GetComponentsInChildren<SpriteRenderer>())
                if (sr.sprite != null)
                    masAncho = Mathf.Max(masAncho, sr.bounds.size.x);

            return masAncho > 0.01f ? masAncho : anchoBoca;
        }
    }

    /// <summary>Ancho del haz al final, para mostrarlo en el inspector y comparar.</summary>
    public float AnchoFinal
    {
        get
        {
            if (forma == Forma.ConoActual)
                return 2f * alcance * Mathf.Tan(anguloCono * 0.5f * Mathf.Deg2Rad);

            return AnchoDeLaBoca + 2f * alcance * Mathf.Tan(divergencia * 0.5f * Mathf.Deg2Rad);
        }
    }

    private void OnEnable()
    {
        meshFilter = GetComponent<MeshFilter>();

        if (mesh == null)
        {
            mesh = new Mesh { name = "PrototipoHaz" };
            mesh.MarkDynamic();
        }

        meshFilter.sharedMesh = mesh;
    }

    private void Update() => Generar();

    private void Generar()
    {
        if (mesh == null) return;

        Vector2 origen = Origen;
        Vector2 dirBase = direccion.sqrMagnitude < 0.0001f ? Vector2.down : direccion.normalized;
        Vector2 perpendicular = new Vector2(-dirBase.y, dirBase.x);

        bool desdePunto = forma == Forma.ConoActual;
        float apertura = desdePunto ? anguloCono : divergencia;
        float boca = desdePunto ? 0f : AnchoDeLaBoca;

        var vertices = new List<Vector3>();
        var uvs = new List<Vector2>();
        var triangulos = new List<int>();

        for (int i = 0; i <= muestras; i++)
        {
            float t = i / (float)muestras;

            // De donde sale este rayo: un punto para el cono, un tramo de la
            // boca para las otras formas.
            Vector2 salida = origen + perpendicular * ((t - 0.5f) * boca);

            // Hacia donde va: se abre segun la divergencia.
            float angulo = (t - 0.5f) * apertura;
            Vector2 dirRayo = (Quaternion.Euler(0, 0, angulo) * dirBase).normalized;

            // Los rayos de los costados llegan menos lejos que los del centro,
            // asi la punta del haz termina curva en vez de cortada en recto.
            // El cono viejo tenia esto de regalo, porque todos sus rayos salian
            // del mismo punto y el final le quedaba como un arco.
            float desdeElCentro = (t - 0.5f) * 2f;
            float alcanceDelRayo = alcance * (1f - redondezPunta * desdeElCentro * desdeElCentro);

            RaycastHit2D hit = Physics2D.Raycast(salida, dirRayo, alcanceDelRayo, mascaraBloqueos);
            Vector2 llegada = hit.collider != null ? hit.point : salida + dirRayo * alcanceDelRayo;

            vertices.Add(transform.InverseTransformPoint(salida));
            vertices.Add(transform.InverseTransformPoint(llegada));

            // u recorre el ancho del haz, v va de la boca a la punta. Con eso el
            // shader sabe que tan al borde esta cada pixel y que tan lejos.
            uvs.Add(new Vector2(t, 0f));
            uvs.Add(new Vector2(t, 1f));

            if (i == 0) continue;

            int b = (i - 1) * 2;
            triangulos.Add(b); triangulos.Add(b + 1); triangulos.Add(b + 2);
            triangulos.Add(b + 1); triangulos.Add(b + 3); triangulos.Add(b + 2);
        }

        mesh.Clear();
        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangulos, 0);
        mesh.SetUVs(0, uvs);
        mesh.RecalculateBounds();
    }

    /// <summary>
    /// Si un punto cae dentro del haz y, en ese caso, si esta en el nucleo o en
    /// el borde. Es lo que usaria el juego para decidir entre matar y avisar.
    /// </summary>
    public bool Alcanza(Vector2 punto, out bool enNucleo)
    {
        enNucleo = false;

        Vector2 origen = Origen;
        Vector2 dirBase = direccion.normalized;
        Vector2 perpendicular = new Vector2(-dirBase.y, dirBase.x);

        Vector2 relativo = punto - origen;
        float avance = Vector2.Dot(relativo, dirBase);

        if (avance < 0f || avance > alcance) return false;

        float desvio = Mathf.Abs(Vector2.Dot(relativo, perpendicular));
        float mitadAca = (forma == Forma.ConoActual ? 0f : AnchoDeLaBoca * 0.5f)
                       + avance * Mathf.Tan((forma == Forma.ConoActual ? anguloCono : divergencia) * 0.5f * Mathf.Deg2Rad);

        if (desvio > mitadAca) return false;

        // El nucleo es la franja central; el resto del ancho es borde.
        enNucleo = forma != Forma.NucleoYHalo || desvio <= mitadAca * fraccionNucleo;
        return true;
    }

    private void OnDrawGizmosSelected()
    {
        if (!dibujarGizmos) return;

        Vector2 origen = Origen;
        Vector2 dirBase = direccion.sqrMagnitude < 0.0001f ? Vector2.down : direccion.normalized;
        Vector2 perpendicular = new Vector2(-dirBase.y, dirBase.x);

        // El ancho al final, que es lo que se compara entre formas
        Vector2 puntaCentro = origen + dirBase * alcance;
        float mitad = AnchoFinal * 0.5f;

        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(puntaCentro - perpendicular * mitad, puntaCentro + perpendicular * mitad);

        if (forma == Forma.NucleoYHalo)
        {
            Gizmos.color = Color.red;
            float mitadNucleo = mitad * fraccionNucleo;
            Gizmos.DrawLine(puntaCentro - perpendicular * mitadNucleo, puntaCentro + perpendicular * mitadNucleo);
        }
    }
}
