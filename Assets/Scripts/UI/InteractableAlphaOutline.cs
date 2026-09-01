using UnityEngine;
using TMPro;
using UnityEngine.UI;

[RequireComponent(typeof(SpriteRenderer), typeof(Collider2D))]
public class InteractableAlphaOutline : MonoBehaviour
{
    [Header("Material de contorno (Custom/SpriteAlphaOutline)")]
    public Material outlineMaterial;

    [Header("Ajustes visuales")]
    public Color outlineColor = new Color(1f, 0.95f, 0.6f, 1f);
    [Range(0.5f, 3f)] public float thicknessPx = 1f;
    public bool renderEncima = true;
    public int orderOffset = 1;

    [Header("Animación Outline")]
    public float fadeSpeed = 8f;
    public float pulseSpeed = 2f;
    [Range(0f, 0.6f)] public float pulseStrength = 0.15f;

    [Header("Prompt de interacción")]
    public GameObject promptPrefab;
    public Vector3 promptOffset = new Vector3(0, 1.2f, 0);
    public float promptFadeSpeed = 6f;

    [Header("Detección")]
    public string playerTag = "Player";

    private SpriteRenderer baseSR;
    private SpriteRenderer outlineSR;
    private Material runtimeMat;

    private bool playerNear;
    private float currentIntensity;

    // Prompt
    private CanvasGroup promptCanvas;
    private Transform promptTransform;
    private Camera cam;

    void Awake()
    {
        cam = Camera.main;
        baseSR = GetComponent<SpriteRenderer>();

        // ============================================
        // OUTLINE (NO DUPLICAR)
        // ============================================
        Transform existingOutline = transform.Find("Outline");

        if (existingOutline != null)
        {
            outlineSR = existingOutline.GetComponent<SpriteRenderer>();
            runtimeMat = outlineSR.material;
        }
        else
        {
            GameObject go = new GameObject("Outline");
            go.transform.SetParent(transform);
            go.transform.localPosition = Vector3.zero;

            outlineSR = go.AddComponent<SpriteRenderer>();
            outlineSR.sprite = baseSR.sprite;
            outlineSR.sortingLayerID = baseSR.sortingLayerID;

            outlineSR.sortingOrder = baseSR.sortingOrder +
                (renderEncima ? Mathf.Abs(orderOffset) : -Mathf.Abs(orderOffset));

            runtimeMat = new Material(outlineMaterial);
            outlineSR.material = runtimeMat;

            runtimeMat.SetColor("_OutlineColor", outlineColor);
            runtimeMat.SetFloat("_ThicknessPx", thicknessPx);
            runtimeMat.SetFloat("_AlphaCutoff", 0.1f);
            runtimeMat.SetFloat("_Intensity", 0f);
        }

        // ============================================
        // PROMPT (CREAR UNA SOLA VEZ)
        // ============================================
        if (promptPrefab != null)
        {
            Canvas canvas = FindAnyObjectByType<Canvas>(FindObjectsInactive.Include);

            if (canvas == null)
            {
                Debug.LogError(" InteractableAlphaOutline: No se encontró Canvas.");
                return;
            }

            GameObject p = Instantiate(promptPrefab, canvas.transform);

            promptTransform = p.transform;
            promptCanvas = p.GetComponent<CanvasGroup>() ?? p.AddComponent<CanvasGroup>();

            promptCanvas.alpha = 0f;
            promptCanvas.gameObject.SetActive(false);

            // Evita que persista entre escenas o duplicaciones raras
            p.hideFlags = HideFlags.DontSave;
        }
    }

    void Update()
    {
        // Mantener sprite en sync
        if (outlineSR.sprite != baseSR.sprite)
            outlineSR.sprite = baseSR.sprite;

        // ============================================
        // OUTLINE FADE + PULSE
        // ============================================
        float target = playerNear ? 1f : 0f;

        float pulse = (playerNear && pulseStrength > 0f)
            ? (Mathf.Sin(Time.time * Mathf.PI * 2f * pulseSpeed) * 0.5f + 0.5f) * pulseStrength
            : 0f;

        currentIntensity = Mathf.Lerp(currentIntensity, target, Time.deltaTime * fadeSpeed);
        runtimeMat.SetFloat("_Intensity", Mathf.Clamp01(currentIntensity + pulse));

        // ============================================
        // PROMPT UI
        // ============================================
        if (promptCanvas != null && promptTransform != null)
        {
            Vector3 screenPos = cam.WorldToScreenPoint(transform.position + promptOffset);
            promptTransform.position = screenPos;

            if (playerNear)
            {
                if (!promptCanvas.gameObject.activeSelf)
                    promptCanvas.gameObject.SetActive(true);

                promptCanvas.alpha = Mathf.MoveTowards(promptCanvas.alpha, 1f,
                    Time.deltaTime * promptFadeSpeed);
            }
            else
            {
                promptCanvas.alpha = Mathf.MoveTowards(promptCanvas.alpha, 0f,
                    Time.deltaTime * promptFadeSpeed);

                if (promptCanvas.alpha <= 0.01f)
                    promptCanvas.gameObject.SetActive(false);
            }
        }
    }

    // ============================================
    // DETECCIÓN
    // ============================================
    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag(playerTag))
            playerNear = true;
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag(playerTag))
            playerNear = false;
    }

    // ============================================
    // LIMPIEZA AL DESTRUIR
    // ============================================
    void OnDestroy()
    {
        if (promptTransform != null)
            Destroy(promptTransform.gameObject);
    }

    // API pública opcional
    public void SetActiveBySystem(bool canInteract) => playerNear = canInteract;
}
