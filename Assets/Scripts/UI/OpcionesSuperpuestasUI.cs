using UnityEngine;

public class OpcionesSuperpuestasUI : MonoBehaviour
{
    [Header("Paneles de pestañas")]
    public GameObject panelGeneral;
    public GameObject panelControles;
    public GameObject panelSonido;

    [Tooltip("Botones de las pestañas, en el orden General, Controles, Sonido.")]
    public UnityEngine.UI.Button[] pestanas;

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
        MarcarPestana(panel);
        Log.Info(this, $"OpcionesSuperpuestasUI: pestaña activa  {panel.name}");
    }

    // La pestaña abierta en color pleno; las otras, apagadas.
    private void MarcarPestana(GameObject panel)
    {
        if (pestanas == null) return;
        GameObject[] paneles = { panelGeneral, panelControles, panelSonido };
        for (int i = 0; i < pestanas.Length && i < paneles.Length; i++)
            if (pestanas[i] != null)
                pestanas[i].colors = EstiloUI.Colores(paneles[i] == panel ? EstiloUI.Texto : EstiloUI.TextoSecundario);
    }

    public void MostrarGeneral() => TraerAlFrente(panelGeneral);
    public void MostrarControles() => TraerAlFrente(panelControles);
    public void MostrarSonido() => TraerAlFrente(panelSonido);
}
