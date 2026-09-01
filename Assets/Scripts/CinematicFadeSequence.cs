using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.SceneManagement;

public class CinematicFadeSequence : MonoBehaviour
{
    [Header("Imágenes a mostrar en orden")]
    public Image imagen1;
    public Image imagen2;
    public Image imagen3;

    [Header("Duraciones")]
    public float fadeDuration = 1.5f;
    public float delayBetween = 1.0f;

    private void Start()
    {
        StartCoroutine(Secuencia());
    }

    private IEnumerator Secuencia()
    {
        yield return FadeIn(imagen1);
        yield return new WaitForSeconds(delayBetween);

        yield return FadeIn(imagen2);
        yield return new WaitForSeconds(delayBetween);

        yield return FadeIn(imagen3);
        yield return new WaitForSeconds(delayBetween);

        
        SceneManager.LoadScene(Escenas.MainMenu);
    }

    IEnumerator FadeIn(Image img)
    {
        img.gameObject.SetActive(true);
        Color c = img.color;
        c.a = 0;
        img.color = c;

        float t = 0;

        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            float alpha = t / fadeDuration;
            img.color = new Color(c.r, c.g, c.b, alpha);
            yield return null;
        }
    }
}
