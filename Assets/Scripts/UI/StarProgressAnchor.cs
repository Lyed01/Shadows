using UnityEngine;
using UnityEngine.UI;
using TMPro;

[ExecuteAlways] // permite verificar referencias también en modo editor
public class StarProgressAnchor : MonoBehaviour
{
    [Header("Referencias del Hub")]
    public Image iconoEstrella;
    public TextMeshProUGUI textoProgreso;

    [Header("Configuración visual")]
    [Tooltip("Forzar visibilidad de los elementos al iniciar la escena (útil en el Hub).")]
    public bool forzarVisibilidad = true;

    void Awake()
    {
        // Repara referencias si no están asignadas
        if (iconoEstrella == null)
            iconoEstrella = GetComponentInChildren<Image>(includeInactive: true);

        if (textoProgreso == null)
            textoProgreso = GetComponentInChildren<TextMeshProUGUI>(includeInactive: true);

        // Asegura que ambos elementos existan
        if (iconoEstrella == null || textoProgreso == null)
        {
            Debug.LogWarning($"⚠️ StarProgressAnchor en {name}: faltan referencias a UI (icono o texto).");
            return;
        }

        // Fuerza visibilidad si se pidió
        if (forzarVisibilidad)
        {
            if (!gameObject.activeSelf)
            {
                Debug.Log($"🔵 Reactivando {name} (StarProgressAnchor) porque estaba desactivado.");
                gameObject.SetActive(true);
            }

            if (!iconoEstrella.gameObject.activeSelf)
                iconoEstrella.gameObject.SetActive(true);

            if (!textoProgreso.gameObject.activeSelf)
                textoProgreso.gameObject.SetActive(true);

            iconoEstrella.enabled = true;
        }
    }

#if UNITY_EDITOR
    // Verificación en modo editor
    void OnValidate()
    {
        if (iconoEstrella == null)
            iconoEstrella = GetComponentInChildren<Image>(includeInactive: true);
        if (textoProgreso == null)
            textoProgreso = GetComponentInChildren<TextMeshProUGUI>(includeInactive: true);
    }
#endif
}
