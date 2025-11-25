using UnityEngine;

public class Door : MonoBehaviour
{
    public Animator anim;
    public bool IsOpen = false;
    public bool initialIsOpen;

    [Header("Switches requeridos para abrir")]
    public Switch[] switchesRequeridos;

    [Header("Receptores de luz requeridos")]
    public LightReceptor[] receptoresRequeridos;

    [HideInInspector] public bool noReset = false;

    void Start()
    {
        initialIsOpen = IsOpen;
    }

    void Awake()
    {
        if (IsOpen)
        {
            if (anim != null)
            {
                anim.SetBool("IsOpen", true);
                AudioManager.Instance?.ReproducirPuertaAbrir();
            }
        }
        else
        {
            if (anim != null)
            {
                anim.SetBool("IsOpen", false);
                AudioManager.Instance?.ReproducirPuertaCerrar();
            }
        }
    }

    // Se llama cada vez que cambia un switch o un receptor
    public void Evaluar()
    {
        // === 1) Revisar switches ===
        if (switchesRequeridos != null && switchesRequeridos.Length > 0)
        {
            foreach (var s in switchesRequeridos)
            {
                if (s == null || !s.activado)
                {
                    Close();
                    return;
                }
            }
        }

        // === 2) Revisar receptores de luz ===
        if (receptoresRequeridos != null && receptoresRequeridos.Length > 0)
        {
            foreach (var r in receptoresRequeridos)
            {
                if (r == null || !r.estaActivo)
                {
                    Close();
                    return;
                }
            }
        }

        // Si TODO lo requerido está activo → abrir
        Open();
    }

    public void Open()
    {
        if (!IsOpen)
        {
            AudioManager.Instance?.ReproducirPuertaAbrir();
            IsOpen = true;
            if (anim != null)
                anim.SetBool("IsOpen", true);
        }
    }

    public void Close()
    {
        if (IsOpen)
        {
            AudioManager.Instance?.ReproducirPuertaCerrar();
            IsOpen = false;
            if (anim != null)
                anim.SetBool("IsOpen", false);
        }
    }

    public void ResetToInitialState()

    {
        if (noReset) return;
        if (initialIsOpen) Open();
        else Close();
    }
}
