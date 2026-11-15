using UnityEngine;
using UnityEngine.SceneManagement;

public class BootstrapLoader : MonoBehaviour
{
    /// <summary>
    /// Donde el editor deja la escena a abrir al darle Play desde un nivel.
    /// La escribe Assets/Editor/ArranqueEnNivel.cs; en el build no existe.
    /// </summary>
    public const string ClaveNivelDeArranque = "Shadows.NivelDeArranque";

    void Start()
    {
#if UNITY_EDITOR
        // Play desde un nivel en el editor: se entra directo a ese nivel, con
        // los managers ya cargados como si se viniera del menu.
        string nivel = UnityEditor.SessionState.GetString(ClaveNivelDeArranque, "");
        if (nivel != "")
        {
            UnityEditor.SessionState.EraseString(ClaveNivelDeArranque);
            SceneManager.LoadScene(nivel);
            return;
        }
#endif

        // Carga automática del Main Menu
        SceneManager.LoadScene(Escenas.MainMenu);
    }
}
