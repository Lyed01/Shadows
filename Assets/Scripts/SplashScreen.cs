using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

public class SplashScreen : MonoBehaviour
{
    public Image splashImage;       // La imagen que vas a mostrar
    public float fadeDuration = 2f; // Tiempo de fade in y out
    public float displayTime = 3f;  // Tiempo total que se ve la splash

    private void Start()
    {
        if (splashImage != null)
            splashImage.color = new Color(1, 1, 1, 0); // transparente al inicio

        StartCoroutine(ShowSplash());
    }

    private IEnumerator ShowSplash()
    {
        // 🔹 Fade in
        float t = 0f;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            float alpha = Mathf.Lerp(0f, 1f, t / fadeDuration);
            splashImage.color = new Color(1, 1, 1, alpha);
            yield return null;
        }

        // 🔹 Esperar el displayTime
        yield return new WaitForSeconds(displayTime);

        // 🔹 Fade out
        t = 0f;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            float alpha = Mathf.Lerp(1f, 0f, t / fadeDuration);
            splashImage.color = new Color(1, 1, 1, alpha);
            yield return null;
        }

        // 🔹 Cambiar a MainMenu
        SceneManager.LoadScene("MainMenu");
    }
}
