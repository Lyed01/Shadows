using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Tecla que aparece sobre un objeto interactuable. Va en su propio Canvas de
/// pantalla con la escala de la UI: en el mundo la camara tiene mas zoom y la
/// tecla quedaba enorme. La maneja InteractableAlphaOutline.
/// </summary>
[RequireComponent(typeof(Canvas))]
public class PromptTecla : MonoBehaviour
{
    public Image tecla;

    [Header("Pulso")]
    [Tooltip("Latidos por segundo.")]
    public float velocidadPulso = 0.8f;
    [Range(0f, 0.3f)] public float fuerzaPulso = 0.06f;

    [Header("Fundido")]
    public float velocidadFundido = 6f;

    [Header("Al apretar")]
    public float duracionApretado = 0.12f;
    [Range(0.5f, 1f)] public float escalaApretado = 0.85f;
    public Color colorApretado = new Color(0.65f, 0.65f, 0.65f, 1f);
    [Tooltip("Segundos que queda oculta despues de apretar, aunque el jugador siga cerca.")]
    public float tiempoOculta = 0.6f;

    private Canvas canvas;
    private float alfa;
    private float ocultaHasta;
    private bool apretando;

    void Awake()
    {
        canvas = GetComponent<Canvas>();
        tecla.enabled = false;
    }

    /// <summary>Se llama en cada LateUpdate con la posicion en el mundo donde va la tecla.</summary>
    public void Actualizar(Vector3 posicionMundo, bool cerca)
    {
        var dialogo = DialogueSystemWorld.Instance;
        bool mostrar = apretando ||
            (cerca && Time.time >= ocultaHasta && !(dialogo != null && dialogo.EstaActivo));

        alfa = Mathf.MoveTowards(alfa, mostrar ? 1f : 0f, Time.deltaTime * velocidadFundido);
        tecla.enabled = alfa > 0f;
        if (!tecla.enabled) return;

        Camera cam = Camera.main;
        if (cam == null) return;
        Vector2 pantalla = cam.WorldToScreenPoint(posicionMundo);
        float k = canvas.scaleFactor;
        // Pixel entero de la UI: entre pixeles el sprite se ve borroso.
        tecla.rectTransform.anchoredPosition = new Vector2(Mathf.Round(pantalla.x / k), Mathf.Round(pantalla.y / k));

        if (!apretando)
        {
            float pulso = 1f + (Mathf.Sin(Time.time * velocidadPulso * Mathf.PI * 2f) * 0.5f + 0.5f) * fuerzaPulso;
            tecla.rectTransform.localScale = new Vector3(pulso, pulso, 1f);
            tecla.color = new Color(1f, 1f, 1f, alfa);
        }
    }

    /// <summary>El jugador apreto la tecla: se hunde un instante y se va.</summary>
    public void Apretar()
    {
        if (apretando || alfa < 0.5f) return;
        StartCoroutine(Hundir());
    }

    private IEnumerator Hundir()
    {
        apretando = true;
        tecla.rectTransform.localScale = new Vector3(escalaApretado, escalaApretado, 1f);
        tecla.color = colorApretado;
        yield return new WaitForSeconds(duracionApretado);
        apretando = false;
        ocultaHasta = Time.time + tiempoOculta;
    }
}
