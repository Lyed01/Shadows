using UnityEngine;
using UnityEngine.UI;
using System.Collections;

[RequireComponent(typeof(CanvasGroup))]
public class ScreenFader : MonoBehaviour
{
    private CanvasGroup canvasGroup;
    private Coroutine currentFade;

    [Header("Configuración")]
    public float duracion = 1f;
    public Color color = Color.black;

    private Image fondo;

    void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();

        // Crear imagen de fondo
        fondo = gameObject.AddComponent<Image>();
        fondo.color = color;
        fondo.rectTransform.anchorMin = Vector2.zero;
        fondo.rectTransform.anchorMax = Vector2.one;
        fondo.rectTransform.offsetMin = Vector2.zero;
        fondo.rectTransform.offsetMax = Vector2.zero;

        canvasGroup.alpha = 0f;
        canvasGroup.blocksRaycasts = false;
    }

    public void FadeIn(float duracionPersonalizada = -1)
    {
        if (currentFade != null) StopCoroutine(currentFade);
        currentFade = StartCoroutine(FadeRoutine(1f, duracionPersonalizada));
    }

    public void FadeOut(float duracionPersonalizada = -1)
    {
        if (currentFade != null) StopCoroutine(currentFade);
        currentFade = StartCoroutine(FadeRoutine(0f, duracionPersonalizada));
    }

    private IEnumerator FadeRoutine(float objetivo, float duracionPersonalizada)
    {
        float duracionUsada = (duracionPersonalizada > 0) ? duracionPersonalizada : duracion;
        float inicio = canvasGroup.alpha;
        float tiempo = 0f;

        while (tiempo < duracionUsada)
        {
            tiempo += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(inicio, objetivo, tiempo / duracionUsada);
            yield return null;
        }

        canvasGroup.alpha = objetivo;
        currentFade = null;
    }
}
