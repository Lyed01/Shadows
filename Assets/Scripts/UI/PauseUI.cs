using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseMenuUI : MonoBehaviour
{
    [Header("Configuración del menú")]
    [Tooltip("Nombre de la escena del menú principal")]
    public string nombreEscenaMenuPrincipal = "MainMenu";

    // === BOTONES PRINCIPALES ===

    public void Reanudar()
    {
        // Reanuda el juego y vuelve al HUD
        GameManager.Instance?.ReanudarJuego();
        UIManager.Instance?.MostrarHUD();
        
    }

    public void ReiniciarNivel()
    {
        // Reanuda el tiempo por si estaba pausado
        GameManager.Instance?.ReanudarJuego();
        // Reinicia la escena actual
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void AbrirOpciones()
    {
        // Muestra el menú de opciones dentro del UIManager
        UIManager.Instance?.MostrarOpciones();
    }

    public void VolverAlMenuPrincipal()
    {
        // Asegura que el juego no quede pausado y carga la escena del menú principal
        GameManager.Instance?.ReanudarJuego();
        SceneManager.LoadScene(nombreEscenaMenuPrincipal);
    }
}
