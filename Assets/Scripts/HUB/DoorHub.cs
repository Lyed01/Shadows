using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class DoorHub : MonoBehaviour
{
    [Header("Datos del nivel")]
    public string idNivel = "Nivel1";
    public string nombreEscenaNivel = "Nivel1";
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
    public Transform puntoPopup;

    // runtime
    private Transform jugador;
    private bool popupVisible = false;
    private int estrellas = 0;

    private void OnEnable()
    {
        GameManager.OnPlayerSpawned += OnPlayerSpawned;
    }

    private void OnDisable()
    {
        GameManager.OnPlayerSpawned -= OnPlayerSpawned;
    }

    void Start()
    {
        jugador = FindFirstObjectByType<Jugador>()?.transform;

        // Cargar progreso y luz
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
                PopupNivelUI.Instance.Mostrar(this, posReferencia, tituloNivel, descripcionNivel, estrellas);
            }
            else
            {
                PopupNivelUI.Instance.ActualizarPosicion(posReferencia);
            }
        }
        else if (popupVisible)
        {
            popupVisible = false;
            PopupNivelUI.Instance.Ocultar();
        }
    }

    // 🔹 llamado automáticamente por GameManager cuando se crea un nuevo jugador
    private void OnPlayerSpawned(Jugador nuevoJugador)
    {
        if (nuevoJugador == null) return;

        jugador = nuevoJugador.transform;
        Debug.Log($"🔁 DoorHub [{idNivel}] reenganchó referencia al nuevo jugador.");

        // 🟡 Chequeo inmediato por si el jugador renace dentro del área de activación
        if (Vector2.Distance(jugador.position, transform.position) <= distanciaActivacion)
        {
            var posReferencia = (puntoPopup != null) ? puntoPopup.position : transform.position;
            popupVisible = true;
            PopupNivelUI.Instance?.Mostrar(this, posReferencia, tituloNivel, descripcionNivel, estrellas);
        }
    }

    public int ObtenerEstrellas() => estrellas;

    public void Entrar()
    {
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

        if (estrellas > 0 && spriteEncendido != null)
            luzPuerta.sprite = spriteEncendido;
        else if (spriteApagado != null)
            luzPuerta.sprite = spriteApagado;
    }
}
