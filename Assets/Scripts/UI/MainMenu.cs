using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour
{
    [Header("Partida")]
    [Tooltip("Solo se muestra si hay una partida guardada.")]
    public Selectable botonContinuar;
    public Selectable botonNuevaPartida;
    [Tooltip("Texto de Nueva partida: muestra 'CONFIRMAR' antes de borrar una partida.")]
    public TMP_Text textoNuevaPartida;

    [Header("Opciones")]
    [Tooltip("Los botones del menu: se desactivan mientras estan abiertas las opciones, sin esconder el arte.")]
    public CanvasGroup botones;
    [Tooltip("El boton que recibe el foco al volver de opciones.")]
    public Selectable primerBoton;

    private const float SegundosParaConfirmar = 3f;
    private bool esperandoConfirmacion;
    private string textoNuevaPartidaOriginal;

    void Start()
    {
        if (textoNuevaPartida != null) textoNuevaPartidaOriginal = textoNuevaPartida.text;
        ActualizarBotones();
    }

    // Continuar solo existe si hay partida, y es el que queda seleccionado.
    // Se reevalua al volver de opciones: ahi se puede reiniciar el progreso.
    private void ActualizarBotones()
    {
        bool hayPartida = SaveSystem.HayPartida;
        if (botonContinuar != null) botonContinuar.gameObject.SetActive(hayPartida);
        primerBoton = hayPartida && botonContinuar != null ? botonContinuar : botonNuevaPartida;

        if (primerBoton != null && EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(primerBoton.gameObject);
    }

    // BOTONES

    public void Continuar()
    {
        Bloquear();
        GameManager.Instance.ContinuarPartida();
    }

    /// <summary>
    /// Con una partida guardada, el primer clic pide confirmacion y el segundo,
    /// dentro de unos segundos, la borra. Sin partida empieza directo.
    /// </summary>
    public void NuevaPartida()
    {
        if (SaveSystem.HayPartida && !esperandoConfirmacion)
        {
            esperandoConfirmacion = true;
            // rainyhearts no tiene '¿': lo dibuja como un corazon.
            if (textoNuevaPartida != null) textoNuevaPartida.text = "CONFIRMAR";
            StartCoroutine(VencerConfirmacion());
            return;
        }

        CancelarConfirmacion();
        Bloquear();
        GameManager.Instance.NuevaPartida();
    }

    private IEnumerator VencerConfirmacion()
    {
        yield return new WaitForSecondsRealtime(SegundosParaConfirmar);
        CancelarConfirmacion();
    }

    private void CancelarConfirmacion()
    {
        StopAllCoroutines();
        esperandoConfirmacion = false;
        if (textoNuevaPartida != null && textoNuevaPartidaOriginal != null)
            textoNuevaPartida.text = textoNuevaPartidaOriginal;
    }

    // Un segundo clic durante la carga lanzaria otra carga encima.
    private void Bloquear()
    {
        if (botones != null) botones.interactable = false;
    }

    // Abrir panel de opciones: es el mismo que el de la pausa, del UIManager.
    // Al tocar 'Volver' el menu se vuelve a mostrar.
    public void Opciones()
    {
        if (UIManager.Instance == null) return;

        CancelarConfirmacion();
        HabilitarBotones(false);
        UIManager.Instance.AbrirOpcionesDesdeMenu(AlCerrarOpciones);
        Log.Info(this, "Abriendo menú de opciones...");
    }

    private void AlCerrarOpciones()
    {
        HabilitarBotones(true);
        ActualizarBotones();
    }

    // Con las opciones abiertas el menu se atenua: queda de fondo y no distrae.
    private const float AlfaDeFondo = 0.35f;

    private void HabilitarBotones(bool habilitar)
    {
        if (botones == null) return;
        botones.interactable = habilitar;
        botones.blocksRaycasts = habilitar;
        botones.alpha = habilitar ? 1f : AlfaDeFondo;
    }

    // Salir del juego
    public void Salir()
    {
        Log.Info(this, "Saliendo del juego...");
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false; // Solo en editor
#else
        Application.Quit(); // Build
#endif
    }
}
