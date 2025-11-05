using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class LevelExitTrigger : MonoBehaviour
{
    [Header("Configuración del nivel")]
    [Tooltip("Nombre de este nivel (debe coincidir con el del LevelScoreManager).")]
    public string nombreNivel = "Nivel1";

    [Tooltip("Duración mínima antes de volver al hub (para evitar activaciones accidentales).")]
    public float delayAntesDeSalir = 1f;

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
        // 🔹 Reproducir sonido de fin de nivel
        AudioManager.Instance?.ReproducirUIClick();

        // 🔹 Bloquear input y detener tiempo parcialmente
        jugador.SetInputBloqueado(true);
        jugador.SetControlActivo(false);
        GameManager.Instance.CambiarEstado(GameManager.GameState.Transicion);


        yield return new WaitForSeconds(delayAntesDeSalir);

        // 🔹 Recopilar estadísticas desde LevelScoreManager
        if (LevelScoreManager.Instance != null)
        {
            float tiempo = LevelScoreManager.Instance.GetTiempoNivel();
            int muertes = LevelScoreManager.Instance.GetMuertes();
            int habilidades = LevelScoreManager.Instance.GetHabilidadesUsadas();

            // Calcular estrellas finales
            int estrellas = LevelScoreManager.Instance.CalcularEstrellasFinales();

            LevelScoreManager.Instance.GuardarResultados(nombreNivel, estrellas, tiempo, muertes, habilidades);
            Debug.Log($"🏁 Nivel completado: {nombreNivel} → {estrellas}⭐ ({tiempo:F1}s, {muertes} muertes, {habilidades} habilidades)");
        }

        // 🔹 Fade out visual
        if (GameManager.Instance.fader != null)
            GameManager.Instance.fader.FadeIn(GameManager.Instance.duracionFade);

        yield return new WaitForSeconds(GameManager.Instance.duracionFade);

        // 🔹 Cargar Hub
        GameManager.Instance.VolverAlHub();
    }
}
