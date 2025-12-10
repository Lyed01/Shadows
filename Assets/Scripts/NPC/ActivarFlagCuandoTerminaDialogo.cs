using UnityEngine;

public class ActivarFlagCuandoTerminaDialogo : MonoBehaviour
{
    [Header("Flag que se activa cuando ESTE NPC termina su diálogo")]
    public string flagAActivar;

    private void OnEnable()
    {
        DialogueSystemWorld.OnDialogueEnd += OnDialogueFinished;
    }

    private void OnDisable()
    {
        DialogueSystemWorld.OnDialogueEnd -= OnDialogueFinished;
    }

    private void OnDialogueFinished()
    {
        if (DialogueSystemWorld.Instance == null) return;

        Transform emisor = DialogueSystemWorld.Instance.ultimoNPCQueHablo;
        if (emisor == null) return;

        // compara usando root o hijos
        if (emisor == this.transform || emisor.IsChildOf(this.transform))
        {
            SceneStateManager.Instance.SetFlag(flagAActivar);
            Debug.Log("🏁 FLAG ACTIVADA: " + flagAActivar + " por " + name);
        }
        Debug.Log("DEBUG FLAG: ultimoNPCQueHablo = " + DialogueSystemWorld.Instance.ultimoNPCQueHablo);

    }
}
