using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseMenuUI : MonoBehaviour
{
    [Header("Configuración del menú")]
    [Tooltip("Nombre de la escena del menú principal")]
    public string nombreEscenaMenuPrincipal = "MainMenu";

    private bool bloqueado;

    // === BOTONES PRINCIPALES ===
    public void Reanudar()
    {
        if (bloqueado) return;
        bloqueado = true;

        GameManager.Instance?.ReanudarJuego();
        UIManager.Instance?.MostrarHUD();
        AudioManager.Instance?.PlaySfx(UIManager.Instance?.panelPausa.GetComponent<AudioSource>()?.clip);
        Invoke(nameof(Desbloquear), 0.3f);
    }

    public void ReiniciarNivel()
    {
        if (bloqueado) return;
        bloqueado = true;

        GameManager.Instance?.ReanudarJuego();
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        Invoke(nameof(Desbloquear), 0.5f);
    }

    public void AbrirOpciones()
    {
        if (bloqueado) return;
        bloqueado = true;

        UIManager.Instance?.MostrarOpciones();
        AudioManager.Instance?.PlaySfx(UIManager.Instance?.panelOpciones.GetComponent<AudioSource>()?.clip);
        Invoke(nameof(Desbloquear), 0.2f);
    }

    public void VolverAlMenuPrincipal()
    {
        if (bloqueado) return;
        bloqueado = true;

        GameManager.Instance?.ReanudarJuego();
        SceneManager.LoadScene(nombreEscenaMenuPrincipal);
        Invoke(nameof(Desbloquear), 0.5f);
    }

    private void Desbloquear() => bloqueado = false;
}
