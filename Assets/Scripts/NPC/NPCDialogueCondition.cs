using UnityEngine;

[System.Serializable]
public class NPCDialogueCondition
{
    [Tooltip("Nombre o etiqueta de la condición (ej: 'Inicio', 'PostHabilidad', 'EnLaboratorio')")]
    public string idCondicion;

    [Tooltip("Dialogo que se mostrará si se cumple esta condición.")]
    public DialogueData dialogo;

    [Tooltip("¿Solo puede reproducirse una vez?")]
    public bool unaSolaVez = false;

    [HideInInspector] public bool yaUsado = false;
}
