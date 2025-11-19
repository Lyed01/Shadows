using UnityEngine;

[RequireComponent(typeof(SpriteRenderer), typeof(Collider2D))]
public class Switch : MonoBehaviour
{
    [Header("Puertas a activar")]
    public Door[] puertas;

    [Header("Spotlights que controla este switch")]
    public LightControlSettings[] lucesConfiguradas;

    

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

        AplicarAccionesDeLuz(activado);
        ControlPuertas();

        Debug.Log($"🔘 Switch {(activado ? "ON" : "OFF")}");
    }

    // ============================================================
    // 🔥 CONTROL PROFESIONAL DE CADA SPOTLIGHT
    // ============================================================
    private void AplicarAccionesDeLuz(bool estadoON)
    {
        foreach (var cfg in lucesConfiguradas)
        {
            if (cfg == null || cfg.luz == null) continue;
            var luz = cfg.luz;

            // ---------- Encender / Apagar ----------
            if (cfg.modificarEncendido)
            {
                bool encender = estadoON ? cfg.encendidoON : cfg.encendidoOFF;
                luz.SetLuzActiva(encender);

                // Si está apagada, no aplicar el resto de cambios
                if (!encender)
                    continue;
            }

            // ---------- Cambiar tipo de luz ----------
            if (cfg.cambiarTipoLuz && estadoON)
            {
                luz.AlternarTipoLuz();
            }

            // ---------- Titileo ----------
            if (cfg.modificarTitileo)
            {
                luz.titilar = estadoON ? cfg.titilarON : cfg.titilarOFF;
            }

            // ---------- Rotación constante ----------
            if (cfg.modificarRotacion)
            {
                luz.rotacionConstante = estadoON ? cfg.rotacionON : cfg.rotacionOFF;
            }

            // ---------- Oscilación ----------
            if (cfg.modificarOscilacion)
            {
                luz.oscilacion = estadoON ? cfg.oscilacionON : cfg.oscilacionOFF;
                if (estadoON && cfg.oscilacionON)
                    luz.rangoOscilacion = cfg.rangoOscilacion;
            }

            // ---------- Alcance ----------
            if (cfg.modificarAlcance)
            {
                luz.alcance = estadoON ? cfg.alcanceON : cfg.alcanceOFF;
            }
        }
    }

    // ============================================================


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
        foreach (var cfg in lucesConfiguradas)
            if (cfg != null && cfg.luz != null)
                cfg.luz.ResetToInitialState();

    
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
