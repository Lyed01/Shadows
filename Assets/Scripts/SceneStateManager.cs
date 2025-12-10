using System.Collections.Generic;
using UnityEngine;

public class SceneStateManager : MonoBehaviour
{
    public static SceneStateManager Instance;

    private HashSet<string> flags = new HashSet<string>();

    public delegate void SceneFlagEvent(string flag);
    public static event SceneFlagEvent OnFlagAdded;

    void Awake()
    {
        Instance = this;
    }

    public void SetFlag(string flag)
    {
        if (flags.Add(flag))
        {
            Debug.Log($"[SCENE FLAG] Activada: {flag}");
            OnFlagAdded?.Invoke(flag);
        }
    }

    public void RemoveFlag(string flag)
    {
        if (flags.Remove(flag))
            Debug.Log($"[SCENE FLAG] Removida: {flag}");
    }


    public bool HasFlag(string flag) => flags.Contains(flag);
}
