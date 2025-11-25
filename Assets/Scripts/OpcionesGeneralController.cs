using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class OpcionesGeneralController : MonoBehaviour
{
    [Header("Referencias UI")]
    public Toggle mostrarHUDToggle;
    public Toggle pantallaCompletaToggle;
    public TMP_Dropdown resolucionDropdown;
    public Button reiniciarProgresoButton;

    private Resolution[] resoluciones;

    void Start()
    {
        // --- HUD ---
        if (mostrarHUDToggle != null)
        {
            mostrarHUDToggle.isOn = UIManager.Instance.panelHUD.activeSelf;
            mostrarHUDToggle.onValueChanged.AddListener(ToggleHUD);
        }

        // --- Pantalla Completa ---
        if (pantallaCompletaToggle != null)
        {
            pantallaCompletaToggle.isOn = Screen.fullScreen;
            pantallaCompletaToggle.onValueChanged.AddListener(TogglePantallaCompleta);
        }

        // --- Resoluciones ---
        if (resolucionDropdown != null)
        {
            CargarResoluciones();
            resolucionDropdown.onValueChanged.AddListener(CambiarResolucion);
        }

        // --- Reiniciar Progreso ---
        if (reiniciarProgresoButton != null)
        {
            reiniciarProgresoButton.onClick.AddListener(ReiniciarProgreso);
        }
    }

    // ======================================================
    // FUNCIONES
    // ======================================================

    // HUD
    private void ToggleHUD(bool mostrar)
    {
        if (mostrar) UIManager.Instance.MostrarHUD();
        else UIManager.Instance.OcultarTodo();
    }

    // Pantalla Completa
    private void TogglePantallaCompleta(bool fullscreen)
    {
        Screen.fullScreen = fullscreen;
    }

    // Resoluciones
    private void CargarResoluciones()
    {
        resoluciones = Screen.resolutions;
        resolucionDropdown.ClearOptions();

        var opciones = new System.Collections.Generic.List<string>();
        int resolucionActualIndex = 0;

        for (int i = 0; i < resoluciones.Length; i++)
        {
            string opcion = resoluciones[i].width + " x " + resoluciones[i].height;
            opciones.Add(opcion);

            if (resoluciones[i].width == Screen.currentResolution.width &&
                resoluciones[i].height == Screen.currentResolution.height)
            {
                resolucionActualIndex = i;
            }
        }

        resolucionDropdown.AddOptions(opciones);
        resolucionDropdown.value = resolucionActualIndex;
        resolucionDropdown.RefreshShownValue();
    }

    private void CambiarResolucion(int indice)
    {
        Resolution res = resoluciones[indice];
        Screen.SetResolution(res.width, res.height, Screen.fullScreen);
    }

    // Reiniciar progreso
    private void ReiniciarProgreso()
    {
        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();
        Debug.Log("🎯 Progreso reiniciado.");

    }
}
