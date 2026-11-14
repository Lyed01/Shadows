using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Rendering.Universal;

[ExecuteAlways]
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class SpotLightDetector : LightDetectorBase
{
    // CONFIGURACIÓN GENERAL

    [Header("Estado inicial")]
    public bool empezarApagada = false;

    [Header("Pivot de la lámpara / rotación conjunta")]
    public Transform pivotRotacion;
    public Transform lamparaPivot;
    public Vector2 offsetLampara = Vector2.zero;

    [Tooltip("Si la luz gira, el sprite de la lampara lo hace de a estos grados. Rotado de " +
             "a poco en baja resolucion pierde pixeles distintos en cada angulo. 0 lo desactiva.")]
    [Min(0f)] public float pasoGiroLampara = 15f;

    [Header("Parámetros del haz")]
    public Vector2 direccion = Vector2.up;
    [Range(1, 180)] public float anguloCono = 90f;

    [Tooltip("Ancho del que sale la luz. Con 0 nace de un punto y es un cono; con mas, " +
             "un trapecio que anguloCono abre hacia afuera.")]
    [Min(0f)] public float anchoBoca = 0f;

    [Tooltip("Cuanto avanza el origen del haz desde el pivot, en la direccion de la luz, " +
             "para que nazca en el foco de la lampara y no detras.")]
    [Min(0f)] public float salidaHaz = 0f;

    [Tooltip("La luz que pasa al costado de un obstaculo solo sigue si tiene este ancho. " +
             "Las franjas mas finas se cortan y tampoco matan.")]
    [Min(0f)] public float anchoMinimoFranja = 1f;

    public float alcance = 8f;
    [Range(6, 100)] public int cantidadRayos = 30;

    [Header("Rotación automática")]
    public bool rotacionConstante = false;
    public float velocidadRotacion = 45f;
    public bool oscilacion = false;
    public float rangoOscilacion = 45f;

    [Header("Luz 2D del haz")]
    public bool luzSigueHaz = true;
    [Range(0f, 2f)] public float intensidadHaz = 0.8f;

    [Tooltip("Cuanto se estira el resplandor mas alla del haz. Es solo decorativo: " +
             "la zona que mata la define fraccionLetal, no esto. Con 1 el resplandor " +
             "termina donde termina el haz dibujado.")]
    [Range(0.5f, 2f)] public float multiplicadorAlcanceLuz = 1.1f;

    // INTERNOS
    private MeshFilter meshFilter;
    private Mesh mesh;
    private Light2D luzHaz;

    // Buffers del haz: se reutilizan porque se arma una vez por frame y por foco.
    private readonly Dictionary<ShadowBlock, float> iluminadosEsteFrame = new();
    private readonly List<Vector3> vertices = new();
    private readonly List<Vector3> verticesHaz = new();
    private readonly List<Vector2> uvs = new();
    private readonly List<int> triangles = new();
    private RaycastHit2D[] impactos;
    private Vector2[] salidas;
    private Vector2[] direcciones;
    private float[] largos;
    private float[] largosFinales;
    private Vector3[] formaLuz;
    private bool generandoHaz;

    private float anguloActual;
    private float tiempoOscilacion;
    private float giroAcumulado;
    private float anguloBase;
    private float offsetOscilacion = 0f;

    // === ESTADO INICIAL REAL ===
    private Vector3 initPos;
    private Quaternion initRot;

    private Vector2 initDireccion;
    private float initAnguloCono;
    private float initAlcance;
    private float initDañoBase;

    private bool initRotacionConstante;
    private bool initOscilacion;
    private float initRangoOscilacion;
    private bool initTitilar;

    private float initAnguloBase;
    private bool initLuzActiva;
    private bool initLuzEncendida;

    // AWAKE
    void Awake()
    {
        if (empezarApagada)
            SetLuzActiva(false);

        meshFilter = GetComponentInChildren<MeshFilter>();
        meshRenderer = GetComponentInChildren<MeshRenderer>();

        mesh = meshFilter.sharedMesh ?? new Mesh { name = "SpotLightMesh" };
        meshFilter.sharedMesh = mesh;

        anguloActual = Mathf.Atan2(direccion.y, direccion.x) * Mathf.Rad2Deg;
        anguloBase = anguloActual;

        initPos = transform.position;
        initRot = transform.rotation;

        initDireccion = direccion;
        initAnguloCono = anguloCono;
        initAlcance = alcance;
        initDañoBase = dañoBase;
        initTipoLuz = tipoLuz;

        initRotacionConstante = rotacionConstante;
        initOscilacion = oscilacion;
        initRangoOscilacion = rangoOscilacion;
        initTitilar = titilar;

        initAnguloBase = anguloBase;
        initLuzActiva = luzActiva;
        initLuzEncendida = luzEncendida;

        InicializarTitileo();

        if (luzSigueHaz)
            CrearLuzHaz();
    }

    // UPDATE
    void Update()
    {
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            if (meshRenderer == null)
                meshRenderer = GetComponentInChildren<MeshRenderer>();

            meshRenderer.sharedMaterial =
                (tipoLuz == TipoLuz.Roja) ? materialRoja : materialAmarilla;

            // Fuera de play mode se dibuja el haz pero no se aplica nada: sin
            // esto la luz mata al jugador, activa receptores y daña bloques con
            // solo tener la escena abierta.
            GenerarLuzMesh(aplicarEfectos: false);
            ActualizarPivotVisual();
            return;
        }
#endif

        // Si la luz está apagada manualmente  no hacer nada
        if (!luzActiva)
        {
            if (meshRenderer != null)
                meshRenderer.enabled = false;

            if (luzHaz != null)
                luzHaz.intensity = 0f;

            return;
        }

        // Material según tipo de luz
        if (meshRenderer == null)
            meshRenderer = GetComponentInChildren<MeshRenderer>();

        meshRenderer.sharedMaterial =
            (tipoLuz == TipoLuz.Roja) ? materialRoja : materialAmarilla;

        // Rotación y oscilación
        ActualizarRotacionConstante();
        ActualizarOscilacion();
        AplicarRotacionFinal();

        ActualizarTitileo();

        // Generar mesh si está encendida
        if (luzEncendida)
            GenerarLuzMesh();

        // Pivot visual
        ActualizarPivotVisual();
    }

    // ROTACIÓN CONSTANTE + OSCILACIÓN
    private void ActualizarRotacionConstante()
    {
        if (!rotacionConstante) return;
        giroAcumulado += velocidadRotacion * Time.deltaTime;
    }

    private void ActualizarOscilacion()
    {
        if (!oscilacion)
        {
            offsetOscilacion = 0f;
            return;
        }

        tiempoOscilacion += Time.deltaTime;

        float halfCycle =
            (Mathf.Sin(tiempoOscilacion * velocidadRotacion * Mathf.Deg2Rad) + 1f) * 0.5f;

        offsetOscilacion = halfCycle * rangoOscilacion;
    }

    private void AplicarRotacionFinal()
    {
        anguloActual = anguloBase + giroAcumulado + offsetOscilacion;

        float rad = anguloActual * Mathf.Deg2Rad;
        direccion = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)).normalized;
    }

    private void ActualizarPivotVisual()
    {
        if (lamparaPivot == null && pivotRotacion == null) return;

        float z = anguloActual - 90f;

        // Una lampara quieta se queda en su angulo exacto: solo respira la que gira.
        if ((rotacionConstante || oscilacion) && pasoGiroLampara > 0f)
            z = Mathf.Round(z / pasoGiroLampara) * pasoGiroLampara;

        Transform piv = lamparaPivot != null ? lamparaPivot : pivotRotacion;
        piv.rotation = Quaternion.Euler(0, 0, z);

        if (lamparaPivot != null)
        {
            lamparaPivot.position =
                ((pivotRotacion != null) ? pivotRotacion.position : transform.position)
                + (Vector3)offsetLampara;
        }
    }

    // TITILEO

    // MESH + LUZ

    /// <summary>
    /// Arma la geometria del haz y, si se le pide, aplica lo que la luz le hace
    /// a lo que toca: matar al jugador, apagar llamas, avisar a los receptores y
    /// dañar bloques.
    ///
    /// Son dos trabajos distintos y por eso se pueden pedir por separado: en el
    /// editor interesa ver el haz, no que la escena reaccione.
    /// </summary>
    private void GenerarLuzMesh(bool aplicarEfectos = true)
    {
        // Un efecto puede volver a pedir el haz (Matar, un receptor) y pisaria los
        // buffers a mitad del bucle. La llamada de afuera rehace la malla igual.
        if (generandoHaz) return;

        generandoHaz = true;
        try { ArmarHaz(aplicarEfectos); }
        finally { generandoHaz = false; }
    }

    private void ArmarHaz(bool aplicarEfectos)
    {
        // 1. Apagada manualmente
        if (!luzActiva)
            return;

        // 2. Titileo la apagó en este frame
        if (!luzEncendida)
        {
            if (mesh != null)
                mesh.Clear();
            return;
        }

        Vector2 origen = (pivotRotacion != null)
            ? pivotRotacion.position
            : (Vector2)transform.position;

        Vector2 dirBase = direccion.normalized;
        origen += dirBase * salidaHaz;
        Vector2 perpendicular = new(-dirBase.y, dirBase.x);
        float angInicio = -anguloCono * 0.5f;

        iluminadosEsteFrame.Clear();
        vertices.Clear();
        vertices.Add(Vector3.zero);

        // El shader necesita saber de que rayo viene cada pixel, y un abanico
        // comparte un solo vertice en la punta: por eso cada rayo lleva el suyo.
        verticesHaz.Clear();
        uvs.Clear();
        triangles.Clear();

        int totalRayos = cantidadRayos + 1;

        if (largos == null || largos.Length != totalRayos)
        {
            impactos = new RaycastHit2D[totalRayos];
            salidas = new Vector2[totalRayos];
            direcciones = new Vector2[totalRayos];
            largos = new float[totalRayos];
            largosFinales = new float[totalRayos];
        }

        for (int i = 0; i < totalRayos; i++)
        {
            float t = i / (float)cantidadRayos;
            direcciones[i] = Quaternion.Euler(0, 0, angInicio + t * anguloCono) * dirBase;
            salidas[i] = origen + perpendicular * ((t - 0.5f) * anchoBoca);

            impactos[i] = Physics2D.Raycast(salidas[i], direcciones[i], alcance, mascaraBloqueos);
            largos[i] = impactos[i].collider ? impactos[i].distance : alcance;
        }

        RecortarFranjas(salidas, direcciones, largos, anchoMinimoFranja, largosFinales);

        for (int i = 0; i < totalRayos; i++)
        {
            float t = i / (float)cantidadRayos;
            Vector2 salida = salidas[i];

            // Un rayo recortado termina antes de llegar a lo que tenia adelante.
            bool recortado = largosFinales[i] < largos[i] - 0.001f;
            RaycastHit2D hit = recortado ? default : impactos[i];

            Vector2 puntoMundo =
                hit.collider ? hit.point : salida + direcciones[i] * largosFinales[i];

            // Que tan al costado del eje va este rayo: 0 en el centro del haz,
            // 1 en el borde. Define si lo que toque muere o solo se ilumina.
            float desvioDelEje = Mathf.Abs(t - 0.5f) * 2f;
            bool rayoLetal = desvioDelEje <= fraccionLetal;

            if (aplicarEfectos && hit.collider)
            {
                // Al jugador solo lo mata el nucleo. El borde se ve pero no hace
                // daño: antes moria igual rozando el filo tenue que parado en el
                // centro.
                if (rayoLetal && hit.collider.TryGetComponent(out Jugador j))
                    j.Matar();

                // Luz roja mata AbyssFlame
                if (tipoLuz == TipoLuz.Roja &&
                    hit.collider.TryGetComponent(out AbyssFlame flame))
                {
                    flame.Extinguir();
                }

                // Enviar luz a receptores
                if (hit.collider.TryGetComponent(out LightReceptor receptor))
                    receptor.RecibirLuz(tipoLuz);

                // ShadowBlocks: se anota el mas cercano de cada uno para no
                // aplicarle el daño una vez por rayo.
                if (hit.collider.TryGetComponent(out ShadowBlock sb))
                {
                    float distancia = Vector2.Distance(salida, puntoMundo);

                    if (!iluminadosEsteFrame.ContainsKey(sb) ||
                        distancia < iluminadosEsteFrame[sb])
                    {
                        iluminadosEsteFrame[sb] = distancia;
                    }
                }
            }

            Vector3 puntoLocal = transform.InverseTransformPoint(puntoMundo);
            vertices.Add(puntoLocal);

            verticesHaz.Add(transform.InverseTransformPoint(salida));
            verticesHaz.Add(puntoLocal);
            uvs.Add(new Vector2(t, 0f));
            uvs.Add(new Vector2(t, Vector2.Distance(salida, puntoMundo) / alcance));

            if (i == 0) continue;

            int b = (i - 1) * 2;
            triangles.Add(b); triangles.Add(b + 1); triangles.Add(b + 2);
            triangles.Add(b + 1); triangles.Add(b + 3); triangles.Add(b + 2);
        }

        foreach (var kvp in iluminadosEsteFrame)
        {
            float dist = kvp.Value;
            float intensidad = 1f - Mathf.Clamp01(dist / alcance);

            // La roja lo mata de una, pero por RecibirLuz para respetar la
            // proteccion del bloque recien colocado.
            float daño = tipoLuz == TipoLuz.Roja
                ? kvp.Key.vidaBajoLuz
                : dañoBase * intensidad * Time.deltaTime;

            kvp.Key.RecibirLuz(daño, tipoLuz);
        }

        mesh.Clear();
        mesh.SetVertices(verticesHaz);
        mesh.SetTriangles(triangles, 0);
        mesh.SetUVs(0, uvs);
        mesh.RecalculateBounds();

        // Luz 2D freeform
        if (luzSigueHaz && luzHaz != null)
        {
            int count = vertices.Count;

            // Con boca la luz cierra por sus dos esquinas; sin boca, por un solo punto.
            int puntosForma = anchoBoca > 0f ? count + 1 : count;

            // Light2D se queda con la referencia y detecta cambios por contenido.
            if (formaLuz == null || formaLuz.Length != puntosForma)
                formaLuz = new Vector3[puntosForma];

            Vector2 origenLuz = origen;
            Vector2 mitadBoca = perpendicular * (anchoBoca * 0.5f);
            formaLuz[0] = transform.InverseTransformPoint(origenLuz - mitadBoca);

            if (anchoBoca > 0f)
                formaLuz[count] = transform.InverseTransformPoint(origenLuz + mitadBoca);

            for (int i = 1; i < count; i++)
            {
                Vector3 v = vertices[i];
                Vector3 mundo = transform.TransformPoint(v);
                Vector2 dir = ((Vector2)mundo - origenLuz).normalized;
                float dist = Vector2.Distance(mundo, origenLuz);
                Vector2 extendido = origenLuz + dir * dist * multiplicadorAlcanceLuz;
                formaLuz[i] = transform.InverseTransformPoint(extendido);
            }

            luzHaz.SetShapePath(formaLuz);
            luzHaz.intensity = luzEncendida ? intensidadHaz : 0f;
        }
    }

    /// <summary>
    /// Escribe en finales el largo de cada rayo, sin las franjas de luz mas finas que
    /// anchoMinimo que pasan al costado de un obstaculo. El ancho se mide donde la
    /// franja deja atras al obstaculo, porque despues se abre con la distancia.
    /// </summary>
    public static void RecortarFranjas(Vector2[] salidas, Vector2[] direcciones,
                                       float[] largos, float anchoMinimo, float[] finales)
    {
        int total = largos.Length;

        for (int i = 0; i < total; i++)
        {
            int izq = i, der = i;
            float alto = largos[i];

            while (izq > 0 || der < total - 1)
            {
                float vecinoIzq = izq > 0 ? largos[izq - 1] : -1f;
                float vecinoDer = der < total - 1 ? largos[der + 1] : -1f;
                float baseFranja = Mathf.Max(vecinoIzq, vecinoDer);

                // Un escalon chico no es un borde: contra una pared plana los rayos
                // difieren apenas, y eso no debe frenar la busqueda ni acortar el rayo.
                if (alto - baseFranja >= anchoMinimo)
                {
                    Vector2 puntoIzq = salidas[izq] + direcciones[izq] * baseFranja;
                    Vector2 puntoDer = salidas[der] + direcciones[der] * baseFranja;

                    // Un haz angosto entero no llega al minimo cerca de la lampara: lo
                    // que ocupa la mitad del haz o mas nunca cuenta como franja.
                    float anchoHaz = Vector2.Distance(
                        salidas[0] + direcciones[0] * baseFranja,
                        salidas[total - 1] + direcciones[total - 1] * baseFranja);

                    if (Vector2.Distance(puntoIzq, puntoDer) >= Mathf.Min(anchoMinimo, anchoHaz * 0.5f))
                        break;

                    alto = baseFranja;
                }

                if (vecinoIzq >= vecinoDer)
                    izq--;
                else
                    der++;
            }

            finales[i] = alto;
        }
    }

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

    protected override void AplicarIntensidadLuz2D(bool encendida)
    {
        if (luzHaz != null)
            luzHaz.intensity = encendida ? intensidadHaz : 0f;
    }

    private void ActualizarColorLuzHaz()
    {
        if (luzHaz == null) return;

        if (tipoLuz == TipoLuz.Roja)
            luzHaz.color = new Color(1f, 0.25f, 0.2f);
        else
            luzHaz.color = new Color(1f, 0.95f, 0.7f);
    }

    // TIPO DE LUZ (API PÚBLICA)
    protected override void ActualizarPorTipoDeLuz()
    {
        if (meshRenderer == null)
            meshRenderer = GetComponentInChildren<MeshRenderer>();

        base.ActualizarPorTipoDeLuz();
        ActualizarColorLuzHaz();

#if UNITY_EDITOR
        UnityEditor.SceneView.RepaintAll();
#endif
    }

    // ENCENDER / APAGAR COMPLETAMENTE
    public override void SetLuzActiva(bool encendida)
    {
        luzActiva = encendida;

        if (!encendida)
        {
            if (meshRenderer != null)
                meshRenderer.enabled = false;

            if (luzHaz != null)
                luzHaz.intensity = 0f;

            if (mesh != null)
                mesh.Clear();

            luzEncendida = false;
            giroAcumulado = 0f;
            offsetOscilacion = 0f;

            return;
        }

        if (meshRenderer != null)
            meshRenderer.enabled = true;

        if (luzHaz != null)
            luzHaz.intensity = intensidadHaz;

        InicializarTitileo();
        GenerarLuzMesh();
    }

    /// <summary>
    /// Dibuja en el editor donde empieza y donde termina lo que mata, que no
    /// coincide con lo que se ve: el haz se dibuja entero y el resplandor va
    /// todavia mas lejos, pero solo el nucleo hace daño.
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        Vector2 origen = pivotRotacion != null
            ? (Vector2)pivotRotacion.position
            : (Vector2)transform.position;

        Vector2 dirBase = direccion.sqrMagnitude < 0.0001f ? Vector2.up : direccion.normalized;
        origen += dirBase * salidaHaz;

        // Borde del haz: hasta aca se ve, pero no mata
        Gizmos.color = new Color(1f, 0.95f, 0.5f, 0.35f);
        DibujarBorde(origen, dirBase, anguloCono, anchoBoca);

        // Borde del nucleo: de aca para adentro, mata
        Gizmos.color = new Color(1f, 0.3f, 0.2f, 0.9f);
        DibujarBorde(origen, dirBase, anguloCono * fraccionLetal, anchoBoca * fraccionLetal);
    }

    private void DibujarBorde(Vector2 origen, Vector2 dirBase, float apertura, float boca)
    {
        Vector2 izq = Quaternion.Euler(0, 0, -apertura * 0.5f) * dirBase;
        Vector2 der = Quaternion.Euler(0, 0, apertura * 0.5f) * dirBase;

        Vector2 mitadBoca = new Vector2(-dirBase.y, dirBase.x) * (boca * 0.5f);
        Vector2 salidaIzq = origen - mitadBoca;
        Vector2 salidaDer = origen + mitadBoca;

        Gizmos.DrawLine(salidaIzq, salidaIzq + izq * alcance);
        Gizmos.DrawLine(salidaDer, salidaDer + der * alcance);
        Gizmos.DrawLine(salidaIzq + izq * alcance, salidaDer + der * alcance);
    }

    // RESET TOTAL
    public override void ResetToInitialState()
    {
        if (noReset) return;
        StartCoroutine(ProcesarReset());
    }

    private System.Collections.IEnumerator ProcesarReset()
    {
        // Transform
        transform.position = initPos;
        transform.rotation = initRot;

        // Parámetros
        direccion = initDireccion;
        anguloCono = initAnguloCono;
        alcance = initAlcance;
        dañoBase = initDañoBase;

        rotacionConstante = initRotacionConstante;
        oscilacion = initOscilacion;
        rangoOscilacion = initRangoOscilacion;
        titilar = initTitilar;
        InicializarTitileo();

        anguloBase = initAnguloBase;
        giroAcumulado = 0f;
        offsetOscilacion = 0f;
        tiempoOscilacion = 0f;

        luzActiva = initLuzActiva;
        luzEncendida = initLuzEncendida;

        // Tipo de luz vuelve al inicial
        tipoLuz = initTipoLuz;
        ActualizarPorTipoDeLuz();

        // Luz 2D
        if (luzHaz != null)
        {
            luzHaz.intensity = intensidadHaz;
            ActualizarColorLuzHaz();
        }

        // Reconstrucción visual
        GenerarLuzMesh();
        ActualizarPivotVisual();

        yield return null;

        Log.Info(this, $"Spotlight {name} reseteado a su estado inicial.");
    }
}
