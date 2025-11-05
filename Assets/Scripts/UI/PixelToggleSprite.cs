using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Toggle))]
public class PixelToggleSprite : MonoBehaviour
{
    public Image imagenFondo;           // Asigná la Image del toggle
    public Sprite spriteOn;
    public Sprite spriteOff;

    private Toggle toggle;

    void Awake()
    {
        toggle = GetComponent<Toggle>();
        if (imagenFondo == null)
            imagenFondo = GetComponent<Image>();

        toggle.onValueChanged.AddListener(ActualizarSprite);
        ActualizarSprite(toggle.isOn);
    }

    void ActualizarSprite(bool estado)
    {
        if (imagenFondo != null)
            imagenFondo.sprite = estado ? spriteOn : spriteOff;
    }
}
