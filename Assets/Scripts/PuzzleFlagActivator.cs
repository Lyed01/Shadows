using UnityEngine;

public class PuzzleFlagActivator : MonoBehaviour
{
    [Header("Receptores de luz requeridos")]
    public LightReceptor[] receptores;

    [Header("Switches requeridos")]
    public Switch[] switches;

    [Header("Flag a activar cuando el puzzle se resuelva")]
    public string flag = "LUZ_ACTIVADA";

    private bool flagActivada = false;

    void Update()
    {
        if (flagActivada) return;

        if (TodosLosReceptoresActivos() && TodosLosSwitchesActivos())
        {
            SceneStateManager.Instance.SetFlag(flag);
            flagActivada = true;
            Debug.Log("💡 FLAG ACTIVADA: " + flag);
        }
    }

    private bool TodosLosReceptoresActivos()
    {
        if (receptores == null || receptores.Length == 0) return true;

        foreach (var r in receptores)
        {
            if (r == null || !r.estaActivo)
                return false;
        }
        return true;
    }

    private bool TodosLosSwitchesActivos()
    {
        if (switches == null || switches.Length == 0) return true;

        foreach (var s in switches)
        {
            if (s == null || !s.activado)
                return false;
        }
        return true;
    }
}
