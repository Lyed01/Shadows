using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class PauseI : MonoBehaviour
{
    [Header("Configuración del menú")]
    [Tooltip("Nombre de la escena del menú principal")]
    public string nombreEscenaMenuPrincipal = "MainMenu";

    private bool bloqueado;
    private bool uiActiva;

    void OnEnable()
    {
        uiActiva = true;
        Debug.Log("<color=yellow>🟡 PauseMenuUI → ACTIVADA</color>");
    }

    void OnDisable()
    {
        uiActiva = false;
        bloqueado = false;
        Debug.Log("<color=gray>⚫ PauseMenuUI → DESACTIVADA</color>");
    }

    // === MÉTODOS ===
    public void Reanudar()
    {
        if (!PuedeEjecutar("Reanudar")) return;

        bloqueado = true;
        Debug.Log("<color=green>▶ Reanudando juego...</color>");

        GameManager.Instance?.ReanudarJuego();
        UIManager.Instance?.MostrarHUD();

        StartCoroutine(DesbloquearEnRealtime(0.25f));
    }

    public void ReiniciarNivel()
    {
        if (!PuedeEjecutar("ReiniciarNivel")) return;

        bloqueado = true;
        Debug.Log("<color=cyan>🔁 Reiniciando nivel...</color>");

        GameManager.Instance?.ReanudarJuego();
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);

        StartCoroutine(DesbloquearEnRealtime(0.5f));
    }

    public void AbrirOpciones()
    {
        if (!PuedeEjecutar("AbrirOpciones")) return;

        bloqueado = true;
        Debug.Log("<color=magenta>⚙️ Abriendo menú de opciones...</color>");

        UIManager.Instance?.MostrarOpciones();

        StartCoroutine(DesbloquearEnRealtime(0.3f));
    }

    public void VolverAlMenuPrincipal()
    {
        if (!PuedeEjecutar("VolverAlMenuPrincipal")) return;

        bloqueado = true;
        Debug.Log("<color=red>🏠 Volviendo al menú principal...</color>");

        GameManager.Instance?.ReanudarJuego();
        SceneManager.LoadScene(nombreEscenaMenuPrincipal);

        StartCoroutine(DesbloquearEnRealtime(0.5f));
    }

    // === UTILS ===
    private bool PuedeEjecutar(string accion)
    {
        if (!uiActiva)
        {
            Debug.LogWarning($"⛔ [{accion}] cancelado → UI inactiva.");
            return false;
        }

        if (bloqueado)
        {
            Debug.LogWarning($"⚠️ [{accion}] bloqueado temporalmente.");
            return false;
        }

        // Extra: si el canvas o el event system están apagados, avisar
        if (UIManager.Instance != null && UIManager.Instance.gameObject.activeSelf == false)
        {
            Debug.LogError($"❌ [{accion}] — UIManager inactivo, posible bug de canvas.");
            return false;
        }

        return true;
    }

    private IEnumerator DesbloquearEnRealtime(float delay)
    {
        yield return new WaitForSecondsRealtime(delay);
        bloqueado = false;
        Debug.Log("<color=gray>✅ Botones desbloqueados</color>");
    }
}
