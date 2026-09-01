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


    // BOTONES
    // Jugar
    public void Jugar()
    {
        if (!string.IsNullOrEmpty(nombreEscenaJuego))
        {
            SceneManager.LoadScene(nombreEscenaJuego);
            Log.Info(this, "Cargando escena: " + nombreEscenaJuego);
        }
        else
        {
            Log.Aviso(this, "No se ha definido la escena del juego.");
        }
    }

    // Abrir panel de opciones
    public void Opciones()
    {
        if (panelOpciones != null)
        {
            panelOpciones.SetActive(true); // Muestra el panel
            gameObject.SetActive(false);   // Oculta el Main Menu
            Log.Info(this, "Abriendo menú de opciones...");
        }
    }

    // Salir del juego
    public void Salir()
    {
        Log.Info(this, "Saliendo del juego...");
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false; // Solo en editor
#else
        Application.Quit(); // Build
#endif
    }

    // VOLVER DEL PANEL DE OPCIONES
    public void VolverDesdeOpciones()
    {
        if (panelOpciones != null)
        {
            panelOpciones.SetActive(false); // Oculta el panel
            gameObject.SetActive(true);     // Muestra el menú principal
            Log.Info(this, "Volviendo al menú principal");
        }
    }

    public void ReiniciarProgresoTotal()
    {
        Log.Info(this, "Reiniciando TODO el progreso...");

        // Reset LevelScoreManager
        LevelScoreManager.Instance?.ResetProgresoNiveles();

        // Reset AbilityManager (usa tu función ya existente)
        AbilityManager.Instance?.ResetearProgresoDebug();

        SaveSystem.Guardar();

        Log.Info(this, "Progreso reseteado completamente.");
    }


}
