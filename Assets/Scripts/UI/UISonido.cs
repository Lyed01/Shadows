using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

[RequireComponent(typeof(Selectable))]
public class UISonido : MonoBehaviour, IPointerEnterHandler, IPointerClickHandler
{
    private Selectable elemento;

    void Awake()
    {
        elemento = GetComponent<Selectable>();

        // Si es Toggle → reproducir sonido al cambiar estado
        if (TryGetComponent(out Toggle toggle))
            toggle.onValueChanged.AddListener(_ => AudioManager.Instance?.ReproducirUIToggle());

        // Si es Slider → reproducir sonido al moverlo
        if (TryGetComponent(out Slider slider))
            slider.onValueChanged.AddListener(_ => AudioManager.Instance?.ReproducirUISlider());
    }

    // === Hover ===
    public void OnPointerEnter(PointerEventData eventData)
    {
        AudioManager.Instance?.ReproducirUIHover();
    }

    // === Click ===
    public void OnPointerClick(PointerEventData eventData)
    {
        AudioManager.Instance?.ReproducirUIClick();
    }
}
