using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
using Unity.Cinemachine;

/// <summary>
/// Frena la camara en los bordes de la sala, asi no muestra el vacio de afuera.
/// Arma el limite del confiner con el tilemap de paredes de la escena al
/// empezar: todos los niveles lo tienen y nadie tiene que dibujar un limite a mano.
/// Si la sala es mas chica que la vista, el confiner la deja centrada.
/// </summary>
[RequireComponent(typeof(CinemachineCamera), typeof(CinemachineConfiner2D))]
public class LimiteDeCamara : MonoBehaviour
{
    [Tooltip("Tilemap que marca el borde de la sala.")]
    public string nombreParedes = "Tilemap Paredes";

    private CinemachineCamera vcam;
    private CinemachineConfiner2D confiner;
    private float tamanoPedido;
    private int ultimoAncho, ultimoAlto;

    void Awake()
    {
        vcam = GetComponent<CinemachineCamera>();
        confiner = GetComponent<CinemachineConfiner2D>();
        tamanoPedido = vcam.Lens.OrthographicSize;
    }

    // El confiner calcula con el tamaño del lente, pero la Pixel Perfect Camera
    // lo redondea despues a un zoom entero (6 a 7,5 segun la pantalla): sin
    // esto el limite no coincide con lo que se ve. Se rehace si cambia la ventana.
    void LateUpdate()
    {
        if (Screen.width == ultimoAncho && Screen.height == ultimoAlto) return;
        Camera cam = Camera.main;
        var ppc = cam != null ? cam.GetComponent<PixelPerfectCamera>() : null;
        if (ppc == null) return;

        ultimoAncho = Screen.width;
        ultimoAlto = Screen.height;
        var lente = vcam.Lens;
        lente.OrthographicSize = ppc.CorrectCinemachineOrthoSize(tamanoPedido);
        vcam.Lens = lente;
        confiner.InvalidateLensCache();
    }

    void Start()
    {
        Tilemap paredes = BuscarParedes();
        if (paredes == null)
        {
            Log.Aviso(this, $"No hay '{nombreParedes}' en la escena: la cámara no tiene límite.");
            return;
        }

        // Los tilemaps guardan limites viejos mas grandes que lo dibujado.
        paredes.CompressBounds();
        Bounds sala = paredes.localBounds;

        var limite = new GameObject("LimiteDeCamara");
        SceneManager.MoveGameObjectToScene(limite, gameObject.scene);
        limite.transform.position = paredes.transform.TransformPoint(sala.center);
        var caja = limite.AddComponent<BoxCollider2D>();
        caja.size = Vector2.Scale(sala.size, paredes.transform.lossyScale);
        // El confiner solo lee la forma; desactivado no choca ni dispara triggers.
        caja.enabled = false;

        confiner.BoundingShape2D = caja;
        confiner.InvalidateBoundingShapeCache();
    }

    private Tilemap BuscarParedes()
    {
        foreach (var tm in FindObjectsByType<Tilemap>(FindObjectsSortMode.None))
            if (tm.name == nombreParedes && tm.gameObject.scene == gameObject.scene)
                return tm;
        return null;
    }
}
