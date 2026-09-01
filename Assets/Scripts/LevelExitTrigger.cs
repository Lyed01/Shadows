using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class LevelExitTrigger : MonoBehaviour
{
    [Header("Configuración del nivel")]
    [Tooltip("Nombre de este nivel (debe coincidir con el del LevelScoreManager).")]
    public string nombreNivel = "Nivel1";

    [Header("Final del juego (opcional)")]
    public bool irAEscenaFinal = false;

    [Tooltip("Nombre de la escena final a cargar")]
    public string nombreEscenaFinal = Escenas.CinematicaFinal;

    private bool activo = true;

    private void Awake()
    {
        GetComponent<Collider2D>().isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!activo) return;

        Jugador jugador = other.GetComponent<Jugador>();
        if (jugador == null) return;

        activo = false;
        StartCoroutine(FlujoSalidaNivel(jugador));
    }

    private System.Collections.IEnumerator FlujoSalidaNivel(Jugador jugador)
    {
        AudioManager.Instance?.ReproducirUIClick();

        //  Bloquear input y detener tiempo parcialmente
        jugador.SetInputBloqueado(true);
        jugador.SetControlActivo(false);
        GameManager.Instance.CambiarEstado(GameManager.GameState.Transicion);

        //  Recopilar estadísticas desde LevelScoreManager
        if (LevelScoreManager.Instance != null)
        {
            float tiempo = LevelScoreManager.Instance.GetTiempoNivel();
            int muertes = LevelScoreManager.Instance.GetMuertes();
            int habilidades = LevelScoreManager.Instance.GetHabilidadesUsadas();

            int estrellas = LevelScoreManager.Instance.CalcularEstrellasFinales();

            LevelScoreManager.Instance.GuardarResultados(nombreNivel, estrellas, tiempo, muertes, habilidades);
            Log.Info(this, $"Nivel completado: {nombreNivel}  {estrellas} ({tiempo:F1}s, {muertes} muertes, {habilidades} habilidades)");
        }

        //  Fade out visual
        if (GameManager.Instance.fader != null)
            GameManager.Instance.fader.FadeIn(GameManager.Instance.duracionFade);

        yield return new WaitForSeconds(GameManager.Instance.duracionFade);

        if (irAEscenaFinal)
        {
            Log.Info(this, $"LevelExitTrigger: Cargando escena final '{nombreEscenaFinal}'");
            UnityEngine.SceneManagement.SceneManager.LoadScene(nombreEscenaFinal);
        }
        else
        {
            GameManager.Instance.VolverAlHub();
        }
    }
}
