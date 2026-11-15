using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Encadena arriba y abajo los controles hijos, en el orden en que se ven en
/// pantalla. La navegacion automatica de Unity elige por distancia al centro y
/// en paneles con filas anchas y botones sueltos se salteaba controles.
///
/// Izquierda y derecha quedan libres: las usan el selector y los sliders.
/// </summary>
public class ListaNavegable : MonoBehaviour
{
    [Tooltip("Controles sobre la lista (las pestañas): bajan al primero.")]
    public Selectable[] arriba;
    [Tooltip("Control debajo de la lista (Volver): sube al ultimo.")]
    public Selectable abajo;

    private readonly List<Selectable> items = new List<Selectable>();
    private bool pendiente;

    // Se arma un frame despues: otros scripts prenden o apagan controles en su
    // propio OnEnable (el reinicio de progreso) y el orden entre ellos no es fijo.
    void OnEnable() => pendiente = true;

    void LateUpdate()
    {
        if (!pendiente) return;
        pendiente = false;
        Encadenar();
    }

    private void Encadenar()
    {
        items.Clear();
        foreach (Transform hijo in transform)
        {
            var s = hijo.GetComponent<Selectable>();
            if (s != null && hijo.gameObject.activeInHierarchy && s.IsInteractable())
                items.Add(s);
        }
        items.Sort((a, b) => b.transform.position.y.CompareTo(a.transform.position.y));

        Selectable techo = arriba != null && arriba.Length > 0 ? arriba[0] : null;
        for (int i = 0; i < items.Count; i++)
        {
            Selectable anterior = i > 0 ? items[i - 1] : techo;
            Selectable siguiente = i < items.Count - 1 ? items[i + 1] : abajo;
            Enlazar(items[i], anterior, siguiente);
        }

        Selectable primero = items.Count > 0 ? items[0] : abajo;
        Selectable ultimo = items.Count > 0 ? items[items.Count - 1] : techo;
        if (arriba != null)
            foreach (Selectable a in arriba)
                if (a != null) Enlazar(a, a.navigation.selectOnUp, primero, true);
        if (abajo != null)
            Enlazar(abajo, ultimo, abajo.navigation.selectOnDown, true);
    }

    // Los controles de afuera (pestañas, Volver) conservan su izquierda y derecha.
    private static void Enlazar(Selectable s, Selectable sube, Selectable baja, bool conservarLados = false)
    {
        Navigation n = s.navigation;
        n.mode = Navigation.Mode.Explicit;
        n.selectOnUp = sube;
        n.selectOnDown = baja;
        if (!conservarLados)
        {
            n.selectOnLeft = null;
            n.selectOnRight = null;
        }
        s.navigation = n;
    }
}
