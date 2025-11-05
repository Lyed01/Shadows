using UnityEngine;

public class OpcionesSuperpuestasUI : MonoBehaviour
{
    [Header("Paneles de pestañas")]
    public GameObject panelGeneral;
    public GameObject panelControles;
    public GameObject panelSonido;

    // Lleva un registro del panel actualmente al frente
    private GameObject panelFrontal;

    void Start()
    {
        // Arranca con General al frente
        TraerAlFrente(panelGeneral);
    }

    public void TraerAlFrente(GameObject panel)
    {
        if (panel == null) return;

        // Mueve el panel al final del orden de hermanos (al frente visualmente)
        panel.transform.SetAsLastSibling();
        panelFrontal = panel;
    }
}
