using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class BotonUISpriteSwap : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    public Sprite normalSprite;
    public Sprite presionadoSprite;

    private Image img;

    void Awake()
    {
        img = GetComponent<Image>();
        img.sprite = normalSprite;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        img.sprite = presionadoSprite;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        img.sprite = normalSprite;
    }
}
