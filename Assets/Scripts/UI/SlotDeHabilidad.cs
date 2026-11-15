using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Un casillero de la barra de habilidades del HUD: marco, icono y la tecla
/// que la elige. El AbilitySelector decide cuales se ven y cual esta elegida.
/// </summary>
public class SlotDeHabilidad : MonoBehaviour
{
    public Image marco;
    [Tooltip("Borde amarillo que solo se ve en el casillero elegido.")]
    public Image seleccion;
    public Image icono;
    public TMP_Text tecla;

    // El elegido se distingue por color y por opacidad, no solo por uno de los dos.
    private static readonly Color IconoApagado = new Color(1f, 1f, 1f, 0.55f);
    private static readonly Color MarcoApagado = new Color(0.6f, 0.6f, 0.7f, 1f);

    public void Mostrar(HabilidadSO habilidad, bool elegida)
    {
        gameObject.SetActive(true);

        icono.sprite = habilidad.iconoSelector;
        icono.color = elegida ? Color.white : IconoApagado;
        marco.color = elegida ? Color.white : MarcoApagado;
        seleccion.enabled = elegida;

        tecla.text = NombreDeTecla(habilidad.atajo);
        tecla.color = elegida ? EstiloUI.Seleccion : EstiloUI.TextoSecundario;
    }

    public void Ocultar() => gameObject.SetActive(false);

    // Alpha1 a "1": el numero solo, como lo muestra el teclado.
    public static string NombreDeTecla(KeyCode atajo)
    {
        if (atajo >= KeyCode.Alpha0 && atajo <= KeyCode.Alpha9) return ((int)atajo - (int)KeyCode.Alpha0).ToString();
        return atajo == KeyCode.None ? "" : atajo.ToString();
    }
}
