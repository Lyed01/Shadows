using UnityEngine;

[RequireComponent(typeof(Collider2D), typeof(SpriteRenderer))]
public class LightReceptor : MonoBehaviour
{
    [Header("Acciones al activarse")]
    public Door[] puertas;
    public GameObject[] objetosParaActivar;

    [Header("Control individual de luces (mismo sistema que Switch)")]
    public LightControlSettings[] lucesControladas;

    [Header("Sprites visuales")]
    public Sprite spriteApagado;
    public Sprite spriteEncendido;

    [Header("Configuración")]
    public float tiempoDesactivacion = 0.5f;

    private SpriteRenderer spriteRenderer;
    private bool activado = false;
    private float tiempoSinLuz = 0f;
    private int lucesRecibiendo = 0;

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        spriteRenderer.sprite = spriteApagado;

        GetComponent<Collider2D>().isTrigger = true;
    }

    void Update()
    {
        if (activado && lucesRecibiendo == 0)
        {
            tiempoSinLuz += Time.deltaTime;
            if (tiempoSinLuz >= tiempoDesactivacion)
                Desactivar();
        }
        else if (lucesRecibiendo > 0)
        {
            tiempoSinLuz = 0f;
        }

        lucesRecibiendo = 0;
    }

    // ============================================================
    // 🔸 Activación del receptor
    // ============================================================
    public void RecibirLuz(SpotLightDetector.TipoLuz tipo)
    {
        lucesRecibiendo++;

        if (!activado)
            Activar();
    }

    private void Activar()
    {
        activado = true;
        spriteRenderer.sprite = spriteEncendido;

        AplicarAccionesEnLuces(true);

        foreach (var p in puertas)
            if (p != null) p.Open();

        foreach (var obj in objetosParaActivar)
            if (obj != null) obj.SetActive(true);

        Debug.Log($"🔆 Receptor {name} activado.");
    }

    private void Desactivar()
    {
        activado = false;
        spriteRenderer.sprite = spriteApagado;

        AplicarAccionesEnLuces(false);

        foreach (var p in puertas)
            if (p != null) p.Close();

        foreach (var obj in objetosParaActivar)
            if (obj != null) obj.SetActive(false);

        Debug.Log($"💤 Receptor {name} desactivado.");
    }

    // ============================================================
    // 🔥 CONTROL COMPLETO DE SPOTLIGHT (usando LightControlSettings)
    // ============================================================
    private void AplicarAccionesEnLuces(bool estadoON)
    {
        foreach (var control in lucesControladas)
        {
            if (control == null || control.luz == null) continue;

            SpotLightDetector luz = control.luz;

            // Cambiar tipo de luz
            if (control.cambiarTipoLuz && estadoON)
                luz.AlternarTipoLuz();

            // Titileo
            if (control.modificarTitileo)
                luz.titilar = estadoON ? control.titilarON : false;

            // Rotación constante
            if (control.modificarRotacionConstante)
                luz.rotacionConstante = estadoON ? control.rotacionConstanteON : false;

            // Oscilación
            if (control.modificarOscilacion)
            {
                luz.oscilacion = estadoON ? control.oscilacionON : false;
                if (estadoON && control.oscilacionON)
                    luz.rangoOscilacion = control.nuevoRangoOscilacion;
            }

            // Daño
            if (control.modificarDaño)
                luz.dañoBase = estadoON ? control.dañoON : control.dañoOFF;

            // Alcance
            if (control.modificarAlcance)
                luz.alcance = estadoON ? control.alcanceON : control.alcanceOFF;

            // Intensidad
            if (control.modificarIntensidad)
                luz.intensidadHaz = estadoON ? control.intensidadON : control.intensidadOFF;

            
        }
    }
}
