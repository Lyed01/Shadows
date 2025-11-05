using UnityEngine;
using TMPro;
using UnityEngine.EventSystems;

public class TextHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public TextMeshProUGUI texto;
    public Color colorNormal = Color.white;
    public Color colorHover = new Color(1f, 0.85f, 0.4f);

    void Awake()
    {
        if (texto == null) texto = GetComponent<TextMeshProUGUI>();
        texto.color = colorNormal;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        texto.color = colorHover;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        texto.color = colorNormal;
    }
}
