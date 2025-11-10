using UnityEngine;

public class OpcionesSuperpuestasUI : MonoBehaviour
{
    [Header("Paneles de pestañas")]
    public GameObject panelGeneral;
    public GameObject panelControles;
    public GameObject panelSonido;

    private GameObject panelFrontal;

    void OnEnable()
    {
        // Cuando se abre el menú de opciones, mostrar la pestaña "General"
        if (panelGeneral != null)
            TraerAlFrente(panelGeneral);
    }

    public void TraerAlFrente(GameObject panel)
    {
        if (panel == null) return;

        panel.transform.SetAsLastSibling();
        panelFrontal = panel;

        // Ocultar las demás pestañas (opcional pero limpio visualmente)
        if (panelGeneral != null && panel != panelGeneral)
            panelGeneral.SetActive(false);
        if (panelControles != null && panel != panelControles)
            panelControles.SetActive(false);
        if (panelSonido != null && panel != panelSonido)
            panelSonido.SetActive(false);

        panel.SetActive(true);
        Debug.Log($"🟣 OpcionesSuperpuestasUI: pestaña activa → {panel.name}");
    }

    public void MostrarGeneral() => TraerAlFrente(panelGeneral);
    public void MostrarControles() => TraerAlFrente(panelControles);
    public void MostrarSonido() => TraerAlFrente(panelSonido);
}
