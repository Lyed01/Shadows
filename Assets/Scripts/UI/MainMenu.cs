using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    [Header("Escena del juego")]
    public string nombreEscenaJuego = "Nivel1"; // Cambiar por tu escena

    [Header("Opciones")]
    public GameObject panelOpciones; // Panel de opciones a activar/desactivar

    [Header("controles")]

    public GameObject panelControles;


    // ==================== BOTONES ====================

    // Jugar
    public void Jugar()
    {
        if (!string.IsNullOrEmpty(nombreEscenaJuego))
        {
            SceneManager.LoadScene(nombreEscenaJuego);
            Debug.Log("🎮 Cargando escena: " + nombreEscenaJuego);
        }
        else
        {
            Debug.LogWarning("⚠️ No se ha definido la escena del juego.");
        }
    }

    // Abrir panel de opciones
    public void Opciones()
    {
        if (panelOpciones != null)
        {
            panelOpciones.SetActive(true); // Muestra el panel
            gameObject.SetActive(false);   // Oculta el Main Menu
            Debug.Log("⚙️ Abriendo menú de opciones...");
        }
    }

    // Salir del juego
    public void Salir()
    {
        Debug.Log("🚪 Saliendo del juego...");
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false; // Solo en editor
#else
        Application.Quit(); // Build
#endif
    }

    // ==================== VOLVER DEL PANEL DE OPCIONES ====================
    public void VolverDesdeOpciones()
    {
        if (panelOpciones != null)
        {
            panelOpciones.SetActive(false); // Oculta el panel
            gameObject.SetActive(true);     // Muestra el menú principal
            Debug.Log("⬅️ Volviendo al menú principal");
        }
    }
    public void Nivel1()
    {
        SceneManager.LoadScene("Nivel1");
    }
    public void Nivel2()
    {
        SceneManager.LoadScene("Nivel2");
    }
    public void Nivel3()
    {
        SceneManager.LoadScene("Nivel3");
    }
    public void mostrarControles()
    {
        panelControles.SetActive(true);
    }
    public void cerrarControles()
    {
        panelControles.SetActive(false);
    }
}
