using UnityEngine;

[RequireComponent(typeof(Collider2D))]


[System.Serializable]
public class DialogoPorFlag
{
    public DialogueData dialogo;

    [Header("Flag primaria")]
    public string flag1;
    public bool negarFlag1;

    [Header("Flag secundaria (opcional)")]
    public string flag2;
    public bool negarFlag2;

    [Header("Control")]
    public bool unaSolaVez = false;
    [HideInInspector] public bool usado = false;
}


public class NPCInteractivo : MonoBehaviour
{
    public KeyCode teclaInteraccion = KeyCode.E;
    public float rangoInteraccion = 2f;
    public AudioClip sonidoDialogo;
    public DialogoPorFlag[] dialogos;


    private Transform jugador;

  

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
            // Si el diálogo es one-shot y ya se usó → saltar
            if (d.unaSolaVez && d.usado)
                continue;

            // ----------------------
            // FLAG 1
            // ----------------------
            if (!string.IsNullOrEmpty(d.flag1))
            {
                bool tiene = SceneStateManager.Instance.HasFlag(d.flag1);

                if (!d.negarFlag1 && !tiene) continue;
                if (d.negarFlag1 && tiene) continue;
            }

            // ----------------------
            // FLAG 2
            // ----------------------
            if (!string.IsNullOrEmpty(d.flag2))
            {
                bool tiene = SceneStateManager.Instance.HasFlag(d.flag2);

                if (!d.negarFlag2 && !tiene) continue;
                if (d.negarFlag2 && tiene) continue;
            }

            // ----------------------
            // AHORA sí el diálogo es válido
            // ----------------------
            if (d.unaSolaVez)
                d.usado = true;

            return d.dialogo;
        }

        return null;
    }


    void OnEnable()
    {
        GameManager.OnPlayerSpawned += RegistrarJugador;
    }

    void OnDisable()
    {
        GameManager.OnPlayerSpawned -= RegistrarJugador;
    }

    private void RegistrarJugador(Jugador j)
    {
        jugador = j.transform;
    }




}
