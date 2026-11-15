using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Elige un valor de una lista con izquierda y derecha: "&lt; 1024 x 576 &gt;".
/// Reemplaza al Dropdown, que abre una lista flotante incomoda con mando.
///
/// Va junto a un Selectable con navegacion Vertical: arriba y abajo pasan al
/// control vecino, izquierda y derecha cambian el valor. Las flechas van con
/// navegacion None para que el clic no le saque la seleccion al selector.
/// </summary>
[RequireComponent(typeof(Selectable))]
public class SelectorOpciones : MonoBehaviour, IMoveHandler
{
    public TMP_Text valor;
    public Button flechaIzquierda;
    public Button flechaDerecha;
    public UnityEvent<int> alCambiar = new UnityEvent<int>();

    private readonly List<string> opciones = new List<string>();

    public int Indice { get; private set; }

    void Awake()
    {
        if (flechaIzquierda != null) flechaIzquierda.onClick.AddListener(() => Mover(-1));
        if (flechaDerecha != null) flechaDerecha.onClick.AddListener(() => Mover(1));
    }

    /// <summary>Carga la lista sin avisar del cambio, como SetValueWithoutNotify.</summary>
    public void CargarOpciones(IEnumerable<string> nuevas, int indice)
    {
        opciones.Clear();
        opciones.AddRange(nuevas);
        Indice = Mathf.Clamp(indice, 0, Mathf.Max(0, opciones.Count - 1));
        Refrescar();
    }

    public void OnMove(AxisEventData e)
    {
        if (e.moveDir == MoveDirection.Left) Mover(-1);
        else if (e.moveDir == MoveDirection.Right) Mover(1);
    }

    private void Mover(int paso)
    {
        if (opciones.Count == 0) return;
        int nuevo = Mathf.Clamp(Indice + paso, 0, opciones.Count - 1);
        if (nuevo == Indice) return;

        Indice = nuevo;
        Refrescar();
        alCambiar.Invoke(Indice);
    }

    // En los extremos la flecha se apaga en vez de dar la vuelta: se ve que no hay mas.
    private void Refrescar()
    {
        if (valor != null) valor.text = opciones.Count > 0 ? opciones[Indice] : "";
        if (flechaIzquierda != null) flechaIzquierda.interactable = Indice > 0;
        if (flechaDerecha != null) flechaDerecha.interactable = Indice < opciones.Count - 1;
    }
}
