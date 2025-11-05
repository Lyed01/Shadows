using UnityEngine;

[RequireComponent(typeof(Collider2D), typeof(SpriteRenderer))]
public class LightReceptor : MonoBehaviour
{
    [Header("Acciones al activarse")]
    public Door[] puertas;
    public SpotLightDetector[] luces; // luces que pueden cambiar de tipo
    public GameObject[] objetosParaActivar;

    [Header("Sprites visuales")]
    public Sprite spriteApagado;
    public Sprite spriteEncendido;

    [Header("Configuración")]
    public float tiempoDesactivacion = 0.5f; // tiempo para apagarse si deja de recibir luz
    public bool alternarTipoDeLuz = false;   // si true, cambia amarillo/rojo como switch

    private SpriteRenderer spriteRenderer;
    private bool activado = false;
    private float tiempoSinLuz = 0f;
    private int lucesRecibiendo = 0; // cuántos haces están tocando al receptor

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        spriteRenderer.sprite = spriteApagado;
        GetComponent<Collider2D>().isTrigger = true;
    }

    void Update()
    {
        // Si deja de recibir luz por un tiempo, se apaga
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

        // Reiniciar contador cada frame
        lucesRecibiendo = 0;
    }

    // ============================================================
    // 🔸 Activación por luz (SpotLight o ReflectiveLight)
    // ============================================================
    public void RecibirLuz(SpotLightDetector.TipoLuz tipo)
    {
        lucesRecibiendo++;
        if (!activado)
            Debug.Log($"💡 Receptor {name} recibiendo {tipo} luz.");

        if (!activado)
            Activar();
    }


    private void Activar()
    {
        CambiarTipoDeLuz();
        activado = true;
        spriteRenderer.sprite = spriteEncendido;
        
        foreach (Door puerta in puertas)
            if (puerta != null) puerta.Open();

        foreach (GameObject obj in objetosParaActivar)
            if (obj != null) obj.SetActive(true);

       

        Debug.Log($"🔆 Receptor {name} activado por luz.");
    }

    private void Desactivar()
    {
        activado = false;
        spriteRenderer.sprite = spriteApagado;

        foreach (Door puerta in puertas)
            if (puerta != null) puerta.Close();

        foreach (GameObject obj in objetosParaActivar)
            if (obj != null) obj.SetActive(false);

        Debug.Log($"💤 Receptor {name} desactivado (sin luz).");
    }
    private void CambiarTipoDeLuz()
    {
        foreach (SpotLightDetector luz in luces)
        {
            if (luz == null) continue;
            luz.AlternarTipoLuz(); // ✅ Usa su método interno
        }
    }
}
