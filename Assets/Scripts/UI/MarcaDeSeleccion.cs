using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Pone un '>' a la izquierda del texto del control seleccionado, y hace que
/// pasar el mouse lo seleccione. Asi hay un solo control resaltado por vez,
/// se use mouse, teclado o mando, y la seleccion no depende solo del color.
///
/// Al agregarlo en el editor le aplica al control los colores de EstiloUI.
/// </summary>
[RequireComponent(typeof(Selectable))]
public class MarcaDeSeleccion : MonoBehaviour, ISelectHandler, IDeselectHandler, IPointerEnterHandler
{
    [Tooltip("Texto al que se le pone la marca. Si queda vacio, el primero de los hijos.")]
    public TMP_Text texto;
    public string marca = ">";
    [Tooltip("Pixeles entre la marca y el texto.")]
    public int separacion = 3;

    private Selectable control;
    private TextMeshProUGUI instancia;

    void Awake()
    {
        control = GetComponent<Selectable>();
        if (texto == null) texto = GetComponentInChildren<TMP_Text>(true);
    }

    public void OnPointerEnter(PointerEventData e)
    {
        if (control.IsInteractable() && EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(gameObject);
    }

    public void OnSelect(BaseEventData e) => Mostrar(true);
    public void OnDeselect(BaseEventData e) => Mostrar(false);
    void OnDisable() => Mostrar(false);

    // Si el panel padre lo selecciono al activarse (FocoInicial), OnSelect llego
    // antes de que este componente estuviera activo y no se recibio.
    void OnEnable()
    {
        if (EventSystem.current != null && EventSystem.current.currentSelectedGameObject == gameObject)
            Mostrar(true);
    }

    private void Mostrar(bool mostrar)
    {
        if (texto == null) return;
        if (instancia == null)
        {
            if (!mostrar) return;
            instancia = CrearMarca();
        }

        if (mostrar)
        {
            // Pegada al primer caracter, este alineado como este el texto.
            texto.ForceMeshUpdate();
            Bounds b = texto.textBounds;
            instancia.rectTransform.anchoredPosition =
                new Vector2(Mathf.Round(b.min.x - separacion), Mathf.Round(b.center.y));
            instancia.color = texto.color * control.colors.selectedColor;
        }
        instancia.gameObject.SetActive(mostrar);
    }

    private TextMeshProUGUI CrearMarca()
    {
        var go = new GameObject("Marca", typeof(RectTransform));
        go.transform.SetParent(texto.transform, false);
        var t = go.AddComponent<TextMeshProUGUI>();
        t.font = texto.font;
        t.fontSize = texto.fontSize;
        t.text = marca;
        t.alignment = TextAlignmentOptions.MidlineRight;
        t.textWrappingMode = TextWrappingModes.NoWrap;
        t.raycastTarget = false;

        // Anclada al pivote del texto: textBounds viene en coordenadas locales.
        var rt = t.rectTransform;
        rt.anchorMin = rt.anchorMax = texto.rectTransform.pivot;
        rt.pivot = new Vector2(1f, 0.5f);
        rt.sizeDelta = new Vector2(texto.fontSize, texto.fontSize);
        return t;
    }

#if UNITY_EDITOR
    void Reset()
    {
        var c = GetComponent<Selectable>();
        var t = GetComponentInChildren<TMP_Text>(true);
        c.transition = Selectable.Transition.ColorTint;
        if (t != null)
        {
            c.targetGraphic = t;
            t.color = Color.white;
        }
        c.colors = EstiloUI.Colores(EstiloUI.Texto);
    }
#endif
}
