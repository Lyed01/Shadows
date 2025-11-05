using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using UnityEngine.SceneManagement;

public class StarProgressUI : MonoBehaviour
{
    [Header("UI (se rellenan dinámicamente si están en null)")]
    public Image iconoEstrella;
    public TextMeshProUGUI textoProgreso;

    [Header("Config")]
    public int totalEstrellasPosibles = 45;
    public float duracionAnimacion = 0.6f;
    public float escalaMax = 1.4f;
    public float escalaTextoMax = 1.2f;

    private int estrellasTotales = 0;
    private int estrellasPrevias = 0;

    void Awake()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene s, LoadSceneMode m)
    {
        // Solo nos importa re-bindeo cuando volvemos al Hub
        if (s.name == "Hub")
            StartCoroutine(TryBindInHubAndRefresh());
    }

    private IEnumerator TryBindInHubAndRefresh()
    {
        // Esperá 1 frame para que el Canvas e hijos estén instanciados
        yield return null;

        // Busca el anchor incluso si está inactivo
        var anchor = FindAnyObjectByType<StarProgressAnchor>(FindObjectsInactive.Include);
        if (anchor == null)
        {
            Debug.LogWarning("StarProgressUI: no encontré StarProgressAnchor en el Hub.");
            yield break;
        }

        // Si no están seteadas desde el inspector, enlazá desde el Anchor
        if (iconoEstrella == null) iconoEstrella = anchor.iconoEstrella;
        if (textoProgreso == null) textoProgreso = anchor.textoProgreso;

        if (iconoEstrella == null || textoProgreso == null)
        {
            Debug.LogWarning("StarProgressUI: el Anchor existe pero faltan referencias (Image/TMP).");
            yield break;
        }

        // Refrescar y animar si subió
        ActualizarProgreso(animarSiSube: true);
    }

    public void ActualizarProgreso(bool animarSiSube = false)
    {
        if (textoProgreso == null || iconoEstrella == null) return;

        estrellasPrevias = estrellasTotales;
        estrellasTotales = CalcularEstrellasTotales();

        textoProgreso.text = $"{estrellasTotales} / {totalEstrellasPosibles}";
        iconoEstrella.enabled = true;

        if (animarSiSube && estrellasTotales > estrellasPrevias)
            StartCoroutine(AnimarContador());
    }

    private IEnumerator AnimarContador()
    {
        float t = 0f;
        Vector3 escalaBaseIcono = iconoEstrella.transform.localScale;
        Vector3 escalaBaseTexto = textoProgreso.transform.localScale;
        Color colorBase = textoProgreso.color;
        Color colorDestacado = new Color(1f, 1f, 0.5f);

        while (t < duracionAnimacion)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Sin((t / duracionAnimacion) * Mathf.PI);
            iconoEstrella.transform.localScale = Vector3.Lerp(escalaBaseIcono, escalaBaseIcono * escalaMax, k);
            textoProgreso.transform.localScale = Vector3.Lerp(escalaBaseTexto, escalaBaseTexto * escalaTextoMax, k);
            textoProgreso.color = Color.Lerp(colorBase, colorDestacado, k);
            yield return null;
        }

        iconoEstrella.transform.localScale = escalaBaseIcono;
        textoProgreso.transform.localScale = escalaBaseTexto;
        textoProgreso.color = colorBase;
    }

    private int CalcularEstrellasTotales()
    {
        int total = 0;
        // Ajustá el rango a tu cantidad real de niveles / IDs
        for (int i = 1; i <= 50; i++)
        {
            string clave = $"Nivel_Nivel{i}_Estrellas";
            if (PlayerPrefs.HasKey(clave))
                total += PlayerPrefs.GetInt(clave, 0);
        }
        return total;
    }
}
