using System.Collections.Generic;
using UnityEngine;

public class SceneStateManager : SceneSingleton<SceneStateManager>
{
    private HashSet<string> flags = new HashSet<string>();

    public delegate void SceneFlagEvent(string flag);
    public static event SceneFlagEvent OnFlagAdded;

    public void SetFlag(string flag)
    {
        if (flags.Add(flag))
        {
            Log.Info(this, $"[SCENE FLAG] Activada: {flag}");
            OnFlagAdded?.Invoke(flag);
        }
    }

    public void RemoveFlag(string flag)
    {
        if (flags.Remove(flag))
            Log.Info(this, $"[SCENE FLAG] Removida: {flag}");
    }


    public bool HasFlag(string flag) => flags.Contains(flag);
}
