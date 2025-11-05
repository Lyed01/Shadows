using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class PulseEffect : MonoBehaviour
{
    [Header("Configuración")]
    public float duracionExpansión = 0.6f;  // tiempo que tarda en crecer
    public float rangoObjetivo = 3f;        // rango final (en unidades del mundo)
    public Color colorBase = new Color(0.6f, 0.5f, 0.9f, 0.3f);
    public bool mantenerVisible = true;     // si se mantiene luego de la expansión

    private SpriteRenderer sr;
    private float tiempo;
    private bool terminado = false;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        sr.color = colorBase;
        transform.localScale = Vector3.zero;
    }

    void Update()
    {
        if (terminado) return;

        tiempo += Time.deltaTime;
        float t = Mathf.Clamp01(tiempo / duracionExpansión);

        // === Escala precisa según sprite ===
        float radioSprite = sr.sprite.bounds.extents.x;
        float factor = (rangoObjetivo / radioSprite) * Mathf.SmoothStep(0f, 1f, t);
        transform.localScale = new Vector3(factor, factor, 1f);

        // === Opacidad ===
        Color c = sr.color;
        c.a = Mathf.Lerp(0f, colorBase.a, t);
        sr.color = c;

        if (t >= 1f)
        {
            terminado = true;

            if (!mantenerVisible)
                Destroy(gameObject, 0.5f);
        }
    }

    public void Configurar(float rango, Color color)
    {
        sr = GetComponent<SpriteRenderer>();
        colorBase = color;
        rangoObjetivo = rango;
    }

    public void DestruirSuavemente()
    {
        if (!mantenerVisible) return;
        mantenerVisible = false;
        StartCoroutine(DesvanecerYDestruir());
    }

    private System.Collections.IEnumerator DesvanecerYDestruir()
    {
        float t = 0f;
        float duracionFade = 0.4f;
        Color inicial = sr.color;
        while (t < 1f)
        {
            t += Time.deltaTime / duracionFade;
            Color c = inicial;
            c.a = Mathf.Lerp(inicial.a, 0f, t);
            sr.color = c;
            yield return null;
        }
        Destroy(gameObject);
    }
}
