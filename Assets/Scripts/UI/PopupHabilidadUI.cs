using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class PopupHabilidadUI : MonoBehaviour
{
    [Header("Referencias UI")]
    public CanvasGroup canvasGroup;
    public Image icono;
    public TMP_Text titulo;
    public TMP_Text descripcion;

    [Header("Animación")]
    public float fadeSpeed = 2f;
    public float tiempoVisible = 3f;
    public Vector3 scaleIn = Vector3.one;
    public Vector3 scaleOut = Vector3.one * 0.7f;

    private Coroutine animCoroutine;

    void Awake()
    {
        if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
        canvasGroup.alpha = 0f;
        gameObject.SetActive(false);
    }

    public void Mostrar(Sprite iconoSprite, string tituloTxt, string descripcionTxt)
    {
        if (animCoroutine != null)
            StopCoroutine(animCoroutine);

        icono.sprite = iconoSprite;
        titulo.text = tituloTxt;
        descripcion.text = descripcionTxt;

        gameObject.SetActive(true);
        animCoroutine = StartCoroutine(FadeInOut());
    }

    private IEnumerator FadeInOut()
    {
        transform.localScale = scaleOut;
        canvasGroup.alpha = 0f;

        // Fade In
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime * fadeSpeed;
            canvasGroup.alpha = Mathf.SmoothStep(0f, 1f, t);
            transform.localScale = Vector3.Lerp(scaleOut, scaleIn, t);
            yield return null;
        }

        canvasGroup.alpha = 1f;
        transform.localScale = scaleIn;
        yield return new WaitForSeconds(tiempoVisible);

        // Fade Out
        t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime * fadeSpeed;
            canvasGroup.alpha = Mathf.SmoothStep(1f, 0f, t);
            transform.localScale = Vector3.Lerp(scaleIn, scaleOut, t);
            yield return null;
        }

        gameObject.SetActive(false);
    }
}
