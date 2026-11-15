using UnityEngine;

/// <summary>
/// Singleton que sobrevive a los cambios de escena. Los managers del juego
/// viven en CoreManagers y se acceden desde cualquier escena por Instance.
///
/// La clase derivada pone su inicializacion en OnBoot(), no en Awake(): asi
/// solo corre en la instancia que realmente queda activa, y no en la copia que
/// se descarta cuando ya habia una.
/// </summary>
public abstract class PersistentSingleton<T> : MonoBehaviour where T : MonoBehaviour
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
        // DontDestroyOnLoad solo acepta raices: si el singleton cuelga de otro
        // objeto (el HUD dentro del UIManager), persiste toda su jerarquia.
        DontDestroyOnLoad(transform.root.gameObject);
        OnBoot();
    }

    /// <summary>
    /// Inicializacion de la clase derivada. Corre una sola vez, en la instancia
    /// que queda viva.
    /// </summary>
    protected virtual void OnBoot() { }

    /// <summary>
    /// Las derivadas que necesiten limpiar suscripciones deben sobreescribirlo
    /// y llamar a base.OnDestroy(), para que Instance no quede apuntando a un
    /// objeto ya destruido.
    /// </summary>
    protected virtual void OnDestroy()
    {
        if (Instance == this as T)
            Instance = null;
    }
}
