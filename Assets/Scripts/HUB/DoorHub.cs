using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class DoorHub : MonoBehaviour
{
    [Header("Datos del nivel")]
    public string idNivel = "Nivel1";               // para PlayerPrefs
    public string nombreEscenaNivel = "Nivel1";     // nombre exacto de la escena
    public string tituloNivel = "NIVEL 1 - ZONA 1";
    [TextArea] public string descripcionNivel = "Primer desafío.";

    [Header("Detección")]
    public float distanciaActivacion = 2.5f;

    [Header("Indicador visual")]
    public SpriteRenderer luzPuerta;
    public Sprite spriteApagado;
    public Sprite spriteEncendido;

    [Header("Spawn de retorno")]
    [Tooltip("Punto donde el jugador aparecerá al volver desde este nivel.")]
    public Transform puntoSpawnRetorno;
    [Header("Posición del popup (opcional)")]
    public Transform puntoPopup; // si lo dejas vacío, usa la posición de la puerta

    // runtime
    private Transform jugador;
    private bool popupVisible = false;
    private int estrellas = 0;

    void Start()
    {
        jugador = FindFirstObjectByType<Jugador>()?.transform;

        // Cargar progreso
        estrellas = PlayerPrefs.GetInt($"Nivel_{idNivel}_Estrellas", 0);
        ActualizarLuz();
    }

    void Update()
    {
        if (jugador == null || PopupNivelUI.Instance == null) return;

        var posReferencia = (puntoPopup != null) ? puntoPopup.position : transform.position;
        float dist = Vector2.Distance(jugador.position, transform.position);

        if (dist <= distanciaActivacion)
        {
            if (!popupVisible)
            {
                popupVisible = true;
                PopupNivelUI.Instance.Mostrar(
                    this,
                    posReferencia,
                    tituloNivel,
                    descripcionNivel,
                    estrellas
                );
            }
            else
            {
                // opcional: re-posicionar si el jugador se mueve
                PopupNivelUI.Instance.ActualizarPosicion(posReferencia);
            }
        }
        else if (popupVisible)
        {
            popupVisible = false;
            PopupNivelUI.Instance.Ocultar();
        }
    }

    public int ObtenerEstrellas() => estrellas;

    public void Entrar()
    {
        // Sonido UI y puerta (si quieres desde aquí)
        AudioManager.Instance?.ReproducirUIClick();
        AudioManager.Instance?.ReproducirPuertaAbrir();

        GameManager.Instance?.CargarNivelDesdePuerta(this);
    }

    public void ActualizarProgreso(int nuevasEstrellas)
    {
        estrellas = Mathf.Max(estrellas, nuevasEstrellas);
        PlayerPrefs.SetInt($"Nivel_{idNivel}_Estrellas", estrellas);
        PlayerPrefs.Save();
        ActualizarLuz();
    }

    private void ActualizarLuz()
    {
        if (luzPuerta == null) return;
        if (estrellas > 0 && spriteEncendido != null) luzPuerta.sprite = spriteEncendido;
        else if (spriteApagado != null) luzPuerta.sprite = spriteApagado;
    }
}
