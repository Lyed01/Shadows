using UnityEngine;

public enum DialogueActionType
{
    Ninguna,
    DesbloquearHabilidad,
    ActivarObjeto,
    ReproducirSonido,
    AbrirPuerta
}

[System.Serializable]
public class DialogueAction
{
    public DialogueActionType tipo;

    [Header("Configuración")]
    public AbilityType habilidad;
    public GameObject objetoActivar;
    public bool activar = true;
    public AudioClip sonido;
    public string idPuerta;
}
