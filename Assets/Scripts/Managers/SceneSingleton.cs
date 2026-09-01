using UnityEngine;

/// <summary>
/// Singleton que vive dentro de una escena y muere con ella. Sirve para los
/// elementos que son unicos por escena, como los popups del Hub o el selector
/// de habilidades, y que no deben sobrevivir a un cambio de nivel.
///
/// La diferencia con PersistentSingleton es que este no llama a
/// DontDestroyOnLoad, y que limpia Instance al destruirse: la escena siguiente
/// trae la suya.
/// </summary>
public abstract class SceneSingleton<T> : MonoBehaviour where T : MonoBehaviour
{
    public static T Instance { get; private set; }

    protected virtual void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this as T;
        OnAwake();
    }

    /// <summary>
    /// Inicializacion de la clase derivada. Corre una sola vez, en la instancia
    /// que queda viva.
    /// </summary>
    protected virtual void OnAwake() { }

    /// <summary>
    /// Las derivadas que necesiten limpiar suscripciones deben sobreescribirlo
    /// y llamar a base.OnDestroy().
    /// </summary>
    protected virtual void OnDestroy()
    {
        if (Instance == this as T)
            Instance = null;
    }
}
