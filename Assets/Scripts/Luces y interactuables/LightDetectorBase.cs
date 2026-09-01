using UnityEngine;

/// <summary>
/// Lo que comparten los dos emisores de luz del juego: el foco conico
/// (SpotLightDetector) y el cenital circular (TopLightDetector).
///
/// Aca vive el estado que ambos tenian duplicado —tipo de luz, daño, capas,
/// materiales y titileo— y la logica del parpadeo, que estaba escrita dos
/// veces y de dos formas distintas: una de ellas sorteaba la duracion del
/// ciclo en cada frame y hacia que el patron fuera imposible de leer.
///
/// La geometria del haz y la luz 2D quedan en cada clase concreta, porque el
/// cono y el circulo no se parecen y sus campos de Light2D tienen nombres
/// propios ya guardados en las escenas.
/// </summary>
public abstract class LightDetectorBase : MonoBehaviour
{
    [HideInInspector] public bool noReset = false;

    [Header("Configuración general")]
    public TipoLuz tipoLuz = TipoLuz.Amarilla;

    [Header("Daño")]
    public float dañoBase = 1f;

    [Header("Capas")]
    public LayerMask mascaraBloqueos;

    [Header("Materiales Mesh")]
    public Material materialAmarilla;
    public Material materialRoja;

    [Header("Titileo")]
    public bool titilar = false;
    public Vector2 tiempoEncendida = new(2f, 4f);
    public Vector2 tiempoApagada = new(0.3f, 1.2f);

    [Tooltip("Punto del ciclo en el que arranca el titileo. Con 0.5, dos luces de la misma cadencia se alternan.")]
    [Range(0f, 1f)]
    public float fase = 0f;

    /// <summary>Tipo de luz con el que arranco la escena, para poder restaurarlo.</summary>
    [HideInInspector] public TipoLuz initTipoLuz;

    protected MeshRenderer meshRenderer;

    /// <summary>Estado del parpadeo: false mientras el titileo la tiene apagada.</summary>
    protected bool luzEncendida = true;

    /// <summary>Encendido manual, el que controlan los switches y receptores.</summary>
    protected bool luzActiva = true;

    private float timerTitileo;

    protected Material MaterialSegunTipo =>
        tipoLuz == TipoLuz.Roja ? materialRoja : materialAmarilla;

    // ------------------------------------------------------
    // TITILEO
    // ------------------------------------------------------

    /// <summary>
    /// Coloca el titileo en un punto del ciclo segun la fase configurada, para
    /// que dos luces de la misma cadencia puedan alternarse entre si.
    /// </summary>
    protected void InicializarTitileo()
    {
        float duracionOn = Random.Range(tiempoEncendida.x, tiempoEncendida.y);
        float duracionOff = Random.Range(tiempoApagada.x, tiempoApagada.y);
        float posicion = Mathf.Repeat(fase, 1f) * (duracionOn + duracionOff);

        luzEncendida = posicion < duracionOn;
        timerTitileo = luzEncendida
            ? duracionOn - posicion
            : duracionOn + duracionOff - posicion;
    }

    /// <summary>
    /// Avanza el ciclo de parpadeo. La duracion de cada tramo se sortea una sola
    /// vez, al entrar en el: sorteandola en cada frame el periodo nunca se
    /// estabiliza y el jugador no puede cronometrar su paso por la luz.
    /// </summary>
    protected void ActualizarTitileo()
    {
        if (!titilar)
        {
            luzEncendida = true;
            if (meshRenderer != null)
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

        if (meshRenderer != null)
            meshRenderer.enabled = luzEncendida;

        AplicarIntensidadLuz2D(luzEncendida);
    }

    /// <summary>
    /// Cada detector ajusta su propia Light2D cuando el titileo cambia de
    /// estado. Los campos de intensidad tienen nombres distintos en cada uno.
    /// </summary>
    protected virtual void AplicarIntensidadLuz2D(bool encendida) { }

    // ------------------------------------------------------
    // TIPO DE LUZ
    // ------------------------------------------------------

    public void SetTipoLuz(TipoLuz nuevoTipo)
    {
        tipoLuz = nuevoTipo;
        ActualizarPorTipoDeLuz();
    }

    public void AlternarTipoLuz() =>
        SetTipoLuz(tipoLuz == TipoLuz.Amarilla ? TipoLuz.Roja : TipoLuz.Amarilla);

    /// <summary>
    /// Aplica el material del tipo actual. Las hijas amplian esto para ajustar
    /// tambien el color de su luz 2D y de la lampara.
    /// </summary>
    protected virtual void ActualizarPorTipoDeLuz()
    {
        if (meshRenderer != null)
            meshRenderer.sharedMaterial = MaterialSegunTipo;
    }

    // ------------------------------------------------------
    // ENCENDIDO MANUAL Y RESET
    // ------------------------------------------------------

    /// <summary>Enciende o apaga la luz por completo. La usan switches y receptores.</summary>
    public abstract void SetLuzActiva(bool encendida);

    /// <summary>Devuelve la luz a su estado inicial. La llama GameManager al reiniciar el nivel.</summary>
    public abstract void ResetToInitialState();
}
