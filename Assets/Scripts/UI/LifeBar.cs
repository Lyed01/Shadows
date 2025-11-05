using UnityEngine;
using UnityEngine.UI;

public class LifeBar : MonoBehaviour
{
    public Image barraVida; // asigná la imagen verde en el inspector

    public void SetVida(float actual, float max)
    {
        if (barraVida != null)
            barraVida.fillAmount = actual / max;
    }
}
