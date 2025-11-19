using UnityEngine;

public class Door : MonoBehaviour
{
    public Animator anim;
    public bool IsOpen = false;



    //si la puetra está cerrada, la abre y viceversa

    void Awake()
    {
        if (IsOpen)         {
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
    public void ToggleDoor()
    {
        if (IsOpen)
            Close();
        else
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
}
