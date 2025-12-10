using UnityEngine;

public class SceneFinalResetFlags : MonoBehaviour
{
    void Start()
    {
        var manager = SceneStateManager.Instance;
        if (manager == null) return;

        // Lista exacta de flags SOLO de esta escena
        string[] flagsFinal = {
            "HABLE_UMBRA",
            "HABLE_IGNOS",
            "HABLE_SILHUETTE",
            "NOXEL_1",
            "NOXEL_2",
        };

        foreach (var f in flagsFinal)
            manager.RemoveFlag(f);
    }
}
