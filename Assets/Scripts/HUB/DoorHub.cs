using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class DoorHub : MonoBehaviour
{
    /// <summary>
    /// Todas las puertas vivas del Hub. PopupNivelUI la recorre por indice en
    /// cada frame para elegir la mas cercana al jugador, y por eso es una
    /// SimpleArrayList: se llena una vez al cargar la escena y despues solo se
    /// lee por posicion, que es donde la implementacion estatica rinde.
    /// </summary>
    public static readonly SimpleArrayList<DoorHub> Registradas = new SimpleArrayList<DoorHub>();

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
    private int estrellas = 0;

    private void OnEnable()
    {
        if (!Registradas.Contains(this))
            Registradas.Add(this);
    }

    private void OnDisable()
    {
        Registradas.Remove(this);
    }

    void Start()
    {
        estrellas = SaveSystem.GetEstrellas(idNivel);
        ActualizarLuz();
    }

    /// <summary>Donde se ancla el popup de esta puerta.</summary>
    public Vector3 PosicionPopup => puntoPopup != null ? puntoPopup.position : transform.position;

    public float DistanciaActivacion => distanciaActivacion;
    public string TituloNivel => tituloNivel;
    public string DescripcionNivel => descripcionNivel;

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
        SaveSystem.SetEstrellas(idNivel, estrellas);
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
