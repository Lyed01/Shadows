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

    [Header("Animacion")]
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
        AjustarIcono(iconoSprite);
        titulo.text = tituloTxt;
        // Los textos de los assets traen un espacio despues de cada salto de linea.
        descripcion.text = descripcionTxt.Replace("\n ", "\n");

        gameObject.SetActive(true);
        animCoroutine = StartCoroutine(FadeInOut());
    }

    private const float LadoIcono = 32f;

    // Agranda el icono de a pixeles enteros hasta llenar la caja; los iconos
    // miden entre 10 y 46 px y estirarlos a mano los deformaba.
    private void AjustarIcono(Sprite sprite)
    {
        if (sprite == null) return;
        Vector2 nativo = sprite.rect.size;
        int k = Mathf.Max(1, Mathf.FloorToInt(Mathf.Min(LadoIcono / nativo.x, LadoIcono / nativo.y)));
        Vector2 tam = nativo * k;
        if (tam.x > LadoIcono || tam.y > LadoIcono)
            tam *= LadoIcono / Mathf.Max(tam.x, tam.y);
        icono.rectTransform.sizeDelta = tam;
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
    void OnDisable()
    {
        canvasGroup.alpha = 0f;
        transform.localScale = scaleOut;
        animCoroutine = null;
    }

}
