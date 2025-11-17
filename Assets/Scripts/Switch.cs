using UnityEngine;

[RequireComponent(typeof(SpriteRenderer), typeof(Collider2D))]
public class Switch : MonoBehaviour
{
    [Header("Puertas a activar")]
    public Door[] puertas;

    [Header("Luces que alternarán de tipo (SpotLightDetector)")]
    public SpotLightDetector[] luces; // ← 🔹 ahora sí está declarada

    [Header("Luces adicionales a apagar/encender")]
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

        CambiarTipoDeLuz();
        ControlLucesExtra();
        ControlPuertas();

        Debug.Log($"🔘 Switch {(activado ? "activado" : "desactivado")}: luces {(activado ? "ROJAS" : "AMARILLAS")}");
    }

    // ============================================================
    // 🔸 Alternar tipo de luz usando su método interno
    // ============================================================
    private void CambiarTipoDeLuz()
    {
        foreach (SpotLightDetector luz in luces)
        {
            if (luz == null) continue;
            luz.AlternarTipoLuz(); // ✅ Usa su método interno
        }
    }

    // ============================================================
    // 🔸 Control de luces externas
    // ============================================================
    private void ControlLucesExtra()
    {
        foreach (GameObject luz in lucesParaApagar)
        {
            if (luz != null)
                luz.SetActive(!activado);
        }
    }

    // ============================================================
    // 🔸 Control de puertas
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
    // 🔸 Colisiones e interacción
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
    // 🔸 Reset total
    // ============================================================
    public void ResetSwitch()
    {
        activado = false;
        spriteRenderer.sprite = spriteApagado;

     

        foreach (GameObject luz in lucesParaApagar)
            if (luz != null)
                luz.SetActive(true);

        ControlPuertas();

        Debug.Log("Switch reseteado (luces amarillas)");
    }
    public static void ResetearTodos()
    {
        // Buscar todos los switches sin ordenar (más rápido)
        var switches = FindObjectsByType<Switch>(FindObjectsSortMode.None);

        foreach (var s in switches)
            s.ResetSwitch();
    }


    public void Activar() => ActivarSwitch();
}
