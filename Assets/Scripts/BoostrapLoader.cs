using UnityEngine;
using UnityEngine.SceneManagement;

public class BootstrapLoader : MonoBehaviour
{
    void Start()
    {
        // Carga automática del Main Menu
        SceneManager.LoadScene("MainMenu");
    }
}
