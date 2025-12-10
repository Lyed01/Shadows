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

    [Header("Requisitos de progreso opcionales")]
    public bool requiereFragmentos = false;
    public int fragmentosNecesarios = 0;
    public string claveFragmentos = "FragmentosTotales";

    [HideInInspector] public bool noReset = false;

    void Start()
    {
        
        initialIsOpen = IsOpen;
        Evaluar();

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

        // === 3) Revisar fragmentos requeridos ===
        if (requiereFragmentos)
        {
            int fragmentosActuales = PlayerPrefs.GetInt(claveFragmentos, 0);

            if (fragmentosActuales < fragmentosNecesarios)
            {
                Close();
                return;
            }
        }

        // === SI TODO ESTÁ CUMPLIDO → ABRIR ===
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
