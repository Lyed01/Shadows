using UnityEngine;

[System.Serializable]
public class DialogueLine
{
    [TextArea(2, 5)]
    public string texto;
    public Sprite retrato;
    public string nombreNPC;
    public DialogueAction accionPorLinea;
}

[CreateAssetMenu(fileName = "NuevoDialogo", menuName = "Dialogos/DialogueData")]
public class DialogueData : ScriptableObject
{
    public string idDialogo;
    public DialogueLine[] lineas;
    public DialogueAction accionFinal;
}
