using UnityEngine;

public class LevelScoreManagerSpawner : MonoBehaviour
{
    [Header("Prefab del LevelScoreManager")]
    public LevelScoreManager prefabManager;

    void Awake()
    {
        // Si no hay un LevelScoreManager activo, instanciamos uno nuevo
        if (LevelScoreManager.Instance == null && prefabManager != null)
        {
            Instantiate(prefabManager, Vector3.zero, Quaternion.identity);
            Debug.Log("🧾 LevelScoreManager instanciado en el HUB.");
        }
    }
}
