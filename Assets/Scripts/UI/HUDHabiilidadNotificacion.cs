using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class HUDHabilidadNotificacion : MonoBehaviour
{
    public static HUDHabilidadNotificacion Instance { get; private set; }

    [Header("Referencias UI")]
    public CanvasGroup canvasGroup;      // para fade in/out
    public TextMeshProUGUI texto;
    public Image icono;

    [Header("Animación")]
    public float duracionVisible = 2f;
    public float duracionFade = 0.5f;

    [Header("Audio opcional")]
    public AudioSource audioSource;
    public AudioClip sonidoNotificacion;

    private Coroutine rutinaActual;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (canvasGroup != null)
            canvasGroup.alpha = 0f;
    }

    void OnEnable()
    {
        // Escuchar cuando se desbloquea una habilidad
        if (AbilityManager.Instance != null)
            AbilityManager.Instance.OnAbilityUnlocked.AddListener(MostrarNotificacion);
    }

    void OnDisable()
    {
        if (AbilityManager.Instance != null)
            AbilityManager.Instance.OnAbilityUnlocked.RemoveListener(MostrarNotificacion);
    }

    public void MostrarNotificacion(AbilityType tipo)
    {
        string nombre = tipo.ToString();

        // Sonido
        if (audioSource != null && sonidoNotificacion != null)
            audioSource.PlayOneShot(sonidoNotificacion, 0.9f);

        // Texto
        if (texto != null)
            texto.text = $" Habilidad obtenida: {nombre}";

        // Icono (si querés agregarlo después)
        if (icono != null)
            icono.enabled = false;

        // Reiniciar animación
        if (rutinaActual != null)
            StopCoroutine(rutinaActual);

        rutinaActual = StartCoroutine(FadeSequence());
    }

    private IEnumerator FadeSequence()
    {
        // Fade in
        float t = 0f;
        while (t < duracionFade) ;
        {
            t += Time.unscaledDeltaTime;
            canvasGroup.alpha = Mathf.Lerp(0, 1, t / duracionFade);
            yield return null;
        }
        canvasGroup.alpha = 1;

        yield return new WaitForSecondsRealtime(duracionVisible);

        // Fade out
        t = 0f;
        while (t < duracionFade)
        {
            t += Time.unscaledDeltaTime;
            canvasGroup.alpha = Mathf.Lerp(1, 0, t / duracionFade);
            yield return null;
        }
        canvasGroup.alpha = 0;
    }
}
