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
    public string claveFragmentos = SaveSystem.ClaveFragmentos;

    [HideInInspector] public bool noReset = false;

    // Al cargar la escena la puerta toma su estado en silencio: si no, en el
    // Hub sonaban todas las puertas juntas.
    private bool silenciosa;

    void Start()
    {
        initialIsOpen = IsOpen;
        silenciosa = true;
        Evaluar();
        silenciosa = false;
    }

    void Awake()
    {
        if (anim != null)
            anim.SetBool("IsOpen", IsOpen);
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
            int fragmentosActuales = SaveSystem.GetFragmentos(claveFragmentos);

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
            if (!silenciosa) AudioManager.Instance?.ReproducirPuertaAbrir();
            IsOpen = true;
            if (anim != null)
                anim.SetBool("IsOpen", true);
        }
    }

    public void Close()
    {
        if (IsOpen)
        {
            if (!silenciosa) AudioManager.Instance?.ReproducirPuertaCerrar();
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
