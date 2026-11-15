using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Al abrirse el panel deja seleccionado su primer control. Sin un control
/// seleccionado, el teclado y el mando no tienen desde donde moverse y el menu
/// solo se puede usar con el mouse.
/// </summary>
public class FocoInicial : MonoBehaviour
{
    [Tooltip("El control que queda seleccionado al abrir el panel.")]
    public Selectable primero;

    void OnEnable()
    {
        if (primero != null && EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(primero.gameObject);
    }
}
