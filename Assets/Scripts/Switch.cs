using UnityEngine;

[RequireComponent(typeof(SpriteRenderer), typeof(Collider2D))]
public class Switch : MonoBehaviour
{
    [Header("Puertas a activar")]
    public Door[] puertas;

    [Header("Spotlights que controla este switch")]
    public LightControlSettings[] lucesConfiguradas;

    [Header("Luces externas a encender/apagar")]
    public GameObject[] lucesParaApagar;

    [Header("Sprites del switch")]
    public Sprite spriteApagado;
    public Sprite spriteEncendido;

    [Header("Controles")]
    public KeyCode activationKey = KeyCode.E;

    private SpriteRenderer spriteRenderer;
    private bool activado = false;
    private bool jugadorEnContacto = false;

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        spriteRenderer.sprite = spriteApagado;
        GetComponent<Collider2D>().isTrigger = true;
    }

    void Update()
    {
        if (jugadorEnContacto && Input.GetKeyDown(activationKey))
            ActivarSwitch();
    }

    private void ActivarSwitch()
    {
        activado = !activado;
        spriteRenderer.sprite = activado ? spriteEncendido : spriteApagado;

        AplicarAccionesDeLuz();
        ControlLucesExtra();
        ControlPuertas();

        Debug.Log($"🔘 Switch {(activado ? "ON" : "OFF")}");
    }

    // ============================================================
    // 🔥 CONTROL PROFESIONAL DE CADA SPOTLIGHT
    // ============================================================
    private void AplicarAccionesDeLuz()
    {
        foreach (var config in lucesConfiguradas)
        {
            if (config.luz == null) continue;
            var luz = config.luz;

            // Cambiar tipo de luz
            if (config.cambiarTipoLuz)
                luz.AlternarTipoLuz();

            // Titileo
            if (config.modificarTitileo)
                luz.titilar = activado ? config.titilarON : false;

            // Rotación constante
            if (config.modificarRotacionConstante)
                luz.rotacionConstante = activado ? config.rotacionConstanteON : false;

            // Oscilación
            if (config.modificarOscilacion)
            {
                luz.oscilacion = activado ? config.oscilacionON : false;
                if (config.oscilacionON)
                    luz.rangoOscilacion = config.nuevoRangoOscilacion;
            }

            // Daño base
            if (config.modificarDaño)
                luz.dañoBase = activado ? config.dañoON : config.dañoOFF;

            // Alcance
            if (config.modificarAlcance)
                luz.alcance = activado ? config.alcanceON : config.alcanceOFF;

            // Intensidad de luz 2D
            if (config.modificarIntensidad)
                luz.intensidadHaz = activado ? config.intensidadON : config.intensidadOFF;
        }
    }

    // ============================================================
    private void ControlLucesExtra()
    {
        foreach (GameObject luz in lucesParaApagar)
            if (luz != null)
                luz.SetActive(!activado);
    }

    // ============================================================
    private void ControlPuertas()
    {
        foreach (Door puerta in puertas)
        {
            if (puerta == null) continue;
            if (activado) puerta.Open();
            else puerta.Close();
        }
    }

    // ============================================================
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
            jugadorEnContacto = true;

        if (other.CompareTag("CutsceneCharacter") && !activado)
            ActivarSwitch();
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
            jugadorEnContacto = false;
    }

    // ============================================================
    public void ResetSwitch()
    {
        activado = false;
        spriteRenderer.sprite = spriteApagado;

        // Resetear luces a su estado inicial usando su propio método
        foreach (var config in lucesConfiguradas)
            if (config.luz != null)
                config.luz.ResetToInitialState();

        // Luz externa ON
        foreach (GameObject luz in lucesParaApagar)
            if (luz != null)
                luz.SetActive(true);

        ControlPuertas();

        Debug.Log("🔄 Switch reseteado (spotlights restauradas)");
    }

    public static void ResetearTodos()
    {
        var switches = FindObjectsByType<Switch>(FindObjectsSortMode.None);
        foreach (var s in switches)
            s.ResetSwitch();
    }
}
