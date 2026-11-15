using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Muestra el sprite ON u OFF segun el estado del Toggle. Se compara cada
/// frame en vez de escuchar onValueChanged porque SetIsOnWithoutNotify (al
/// cargar las opciones guardadas) no dispara el evento y el sprite quedaba mal.
/// </summary>
[RequireComponent(typeof(Toggle))]
public class PixelToggleSprite : MonoBehaviour
{
    public Image imagenFondo;
    public Sprite spriteOn;
    public Sprite spriteOff;

    private Toggle toggle;

    void Awake()
    {
        toggle = GetComponent<Toggle>();
        if (imagenFondo == null)
            imagenFondo = GetComponent<Image>();
    }

    void LateUpdate()
    {
        Sprite sprite = toggle.isOn ? spriteOn : spriteOff;
        if (imagenFondo != null && imagenFondo.sprite != sprite)
            imagenFondo.sprite = sprite;
    }
}
