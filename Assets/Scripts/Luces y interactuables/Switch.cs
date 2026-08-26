using UnityEngine;

[RequireComponent(typeof(SpriteRenderer), typeof(Collider2D))]
public class Switch : MonoBehaviour
{
    [Header("Puertas a activar")]
    public Door[] puertas;

    [Header("Luces controladas")]
    public LightControlSettings[] lucesConfiguradas;

    [Header("Sprites")]
    public Sprite spriteApagado;
    public Sprite spriteEncendido;

    [Header("Controles")]
    public KeyCode activationKey = KeyCode.E;

    private SpriteRenderer spriteRenderer;
    private bool _activado = false; // 👈 ahora privado

    public bool activado => _activado; // 👈 lectura pública

    private bool jugadorEnContacto = false;

    private bool activadoPorNPC = false;

    [HideInInspector] public bool noReset = false;


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
        _activado = !_activado;
        spriteRenderer.sprite = _activado ? spriteEncendido : spriteApagado;

        AplicarAccionesDeLuz(_activado);
        EvaluarPuertas();

        Debug.Log($"🔘 Switch {(_activado ? "ON" : "OFF")}");
    }

    private void EvaluarPuertas()
    {
        foreach (Door puerta in puertas)
        {
            if (puerta == null) continue;
            puerta.Evaluar();
        }
    }

    private void AplicarAccionesDeLuz(bool estadoON)
    {
        foreach (var cfg in lucesConfiguradas)
            if (cfg != null) cfg.Aplicar(estadoON);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
            jugadorEnContacto = true;

        if (other.CompareTag("CutsceneCharacter") && !_activado)
        {
            activadoPorNPC = true;
            ActivarSwitch();

            // Hacer permanente
            noReset = true;

            foreach (var p in puertas)
                if (p != null) p.noReset = true;

            foreach (var cfg in lucesConfiguradas)
                if (cfg.spotSettings != null && cfg.spotSettings.spot != null)
                    cfg.spotSettings.spot.noReset = true;

            foreach (var cfg in lucesConfiguradas)
                if (cfg.topSettings != null && cfg.topSettings.top != null)
                    cfg.topSettings.top.noReset = true;
        }

    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
            jugadorEnContacto = false;
    }

    public void ResetSwitch()

    {

        if (noReset) return;
        _activado = false;
        spriteRenderer.sprite = spriteApagado;

        foreach (var cfg in lucesConfiguradas)
            if (cfg != null) cfg.Reset();

        foreach (var p in puertas)
            if (p != null) p.ResetToInitialState();

        EvaluarPuertas();

        Debug.Log("🔄 Switch reseteado");
    }
}
