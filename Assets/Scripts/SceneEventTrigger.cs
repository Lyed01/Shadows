using UnityEngine;

public class SceneEventTrigger : MonoBehaviour
{
    public string flagToSet;
    public bool triggerOnce = true;
    private bool triggered = false;

    public void Activate()
    {
        if (triggerOnce && triggered) return;

        triggered = true;
        SceneStateManager.Instance.SetFlag(flagToSet);
    }
}
