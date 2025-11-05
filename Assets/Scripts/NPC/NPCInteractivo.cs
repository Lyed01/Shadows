using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class NPCInteractivo : MonoBehaviour
{
    public KeyCode teclaInteraccion = KeyCode.E;
    public float rangoInteraccion = 2f;
    public AudioClip sonidoDialogo;
    public NPCDialogueCondition[] dialogos;

    private Transform jugador;

    void Start()
    {
        jugador = FindFirstObjectByType<Jugador>()?.transform;
    }

    void Update()
    {
        if (!jugador) return;

        if (Vector2.Distance(transform.position, jugador.position) <= rangoInteraccion &&
            Input.GetKeyDown(teclaInteraccion))
        {
            if (DialogueSystemWorld.Instance?.EstaActivo ?? false)
                return;

            DialogueData dialogo = SeleccionarDialogo();
            if (dialogo != null)
                DialogueSystemWorld.Instance.IniciarDialogo(dialogo, transform);
        }
    }

    private DialogueData SeleccionarDialogo()
    {
        foreach (var d in dialogos)
        {
            if (d.unaSolaVez && d.yaUsado) continue;
            if (CumpleCondicion(d.idCondicion))
            {
                d.yaUsado = true;
                return d.dialogo;
            }
        }
        return null;
    }

    private bool CumpleCondicion(string id)
    {
        switch (id)
        {
            case "Inicio": return true;
            case "TieneHabilidad": return AbilityManager.Instance?.IsUnlocked(AbilityType.ShadowBlocks) == true;
            default: return false;
        }
    }
}
