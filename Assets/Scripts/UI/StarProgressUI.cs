using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using UnityEngine.SceneManagement;

public class StarProgressUI : MonoBehaviour
{
    [Header("Referencias UI")]
    public Image iconoEstrella;
    public TextMeshProUGUI textoProgreso;

    [Header("Config")]
    public int totalEstrellasPosibles = 45;
    public float tiempoRefresco = 2.0f;
    public float delayInicialAnim = 1.2f;
    public float duracionConteo = 1.8f; // duración de la animación de conteo
    public float escalaMax = 1.3f; // pulso de la estrella

    private int estrellasTotales = 0;
    private bool enHub;
    private Coroutine refrescoCoroutine;

    void Awake()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void Start()
    {
        if (SceneManager.GetActiveScene().name == "Hub")
        {
            enHub = true;
            Inicializar();
        }
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        if (refrescoCoroutine != null)
            StopCoroutine(refrescoCoroutine);
    }

    private void OnSceneLoaded(Scene s, LoadSceneMode m)
    {
        enHub = s.name == "Hub";
        if (enHub)
            Inicializar();
        else
            Ocultar();
    }

    private void Inicializar()
    {
        Debug.Log("🟢 Inicializando StarProgressUI en Hub...");

        // 🧩 Reactiva jerarquía por seguridad
        Transform actual = transform;
        while (actual != null)
        {
            if (!actual.gameObject.activeSelf)
            {
                Debug.LogWarning($"⚠️ Reactivando objeto desactivado en jerarquía: {actual.name}");
                actual.gameObject.SetActive(true);
            }
            actual = actual.parent;
        }

        // Si no hay referencias, intenta encontrarlas
        if (iconoEstrella == null || textoProgreso == null)
        {
            var anchor = FindAnyObjectByType<StarProgressAnchor>(FindObjectsInactive.Include);
            if (anchor != null)
            {
                if (iconoEstrella == null) iconoEstrella = anchor.iconoEstrella;
                if (textoProgreso == null) textoProgreso = anchor.textoProgreso;
            }
        }

        if (iconoEstrella == null || textoProgreso == null)
        {
            Debug.LogWarning("⚠️ StarProgressUI: faltan referencias (Image/TMP).");
            return;
        }

        // Limpia texto e inicia animación
        textoProgreso.text = "";
        iconoEstrella.enabled = true;

        if (refrescoCoroutine != null)
            StopCoroutine(refrescoCoroutine);

        refrescoCoroutine = StartCoroutine(AnimacionInicial());
    }

    private IEnumerator AnimacionInicial()
    {
        // Pequeño delay antes de comenzar (como al volver al Hub)
        yield return new WaitForSecondsRealtime(delayInicialAnim);

        int totalFinal = CalcularEstrellasTotales();
        int actual = 0;
        float tiempo = 0f;

        Vector3 escalaBase = iconoEstrella.transform.localScale;
        textoProgreso.gameObject.SetActive(true);

        // Animación de conteo progresivo
        while (tiempo < duracionConteo)
        {
            tiempo += Time.unscaledDeltaTime;
            float progreso = Mathf.Clamp01(tiempo / duracionConteo);

            // Conteo suave (ease-out)
            int nuevoValor = Mathf.RoundToInt(Mathf.Lerp(0, totalFinal, Mathf.SmoothStep(0, 1, progreso)));

            if (nuevoValor != actual)
            {
                actual = nuevoValor;
                textoProgreso.text = $"{actual} / {totalEstrellasPosibles}";

                // Pulso visual
                StartCoroutine(PulsoVisual(iconoEstrella, escalaBase));
            }

            yield return null;
        }

        // Finaliza en el valor real exacto
        textoProgreso.text = $"{totalFinal} / {totalEstrellasPosibles}";
        iconoEstrella.transform.localScale = escalaBase;

        // Inicia ciclo de refresco normal
        refrescoCoroutine = StartCoroutine(RefrescarPeriodicamente());
    }

    private IEnumerator PulsoVisual(Image img, Vector3 escalaBase)
    {
        float duracionPulso = 0.2f;
        float t = 0f;

        while (t < duracionPulso)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Sin((t / duracionPulso) * Mathf.PI);
            img.transform.localScale = Vector3.Lerp(escalaBase, escalaBase * escalaMax, k);
            img.color = Color.Lerp(Color.white, new Color(1f, 0.9f, 0.5f), k);
            yield return null;
        }

        img.transform.localScale = escalaBase;
        img.color = Color.white;
    }

    private void Ocultar()
    {
        if (textoProgreso != null) textoProgreso.gameObject.SetActive(false);
        if (iconoEstrella != null) iconoEstrella.enabled = false;

        if (refrescoCoroutine != null)
        {
            StopCoroutine(refrescoCoroutine);
            refrescoCoroutine = null;
        }
    }

    private IEnumerator RefrescarPeriodicamente()
    {
        while (enHub)
        {
            ActualizarProgreso();
            yield return new WaitForSecondsRealtime(tiempoRefresco);
        }
    }

    public void ActualizarProgreso()
    {
        if (textoProgreso == null || iconoEstrella == null)
            return;

        estrellasTotales = CalcularEstrellasTotales();
        textoProgreso.text = $"{estrellasTotales} / {totalEstrellasPosibles}";
        iconoEstrella.enabled = true;
    }

    private int CalcularEstrellasTotales()
    {
        int total = 0;
        for (int i = 1; i <= 50; i++)
        {
            string clave = $"Nivel_Nivel{i}_Estrellas";
            if (PlayerPrefs.HasKey(clave))
                total += PlayerPrefs.GetInt(clave, 0);
        }
        return total;
    }
}
