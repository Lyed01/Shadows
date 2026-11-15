using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Escala el Canvas con el mismo zoom entero que la Pixel Perfect Camera, asi
/// un pixel de la UI es un pixel del juego. La UI se diseña en pixeles del
/// juego y tiene que entrar en 512x288, la pantalla mas chica posible.
///
/// Antes los Canvas escalaban desde 1920x1080 con decimales: los sprites de la
/// UI quedaban con pixeles mas finos que los del juego y de tamaños desparejos.
/// </summary>
[RequireComponent(typeof(CanvasScaler))]
public class EscalaPixelUI : MonoBehaviour
{
    /// <summary>Resolucion a la que se dibuja el juego.</summary>
    public const int AnchoJuego = 512;
    public const int AltoJuego = 288;
    /// <summary>PPU de los sprites del juego: con este valor un pixel del sprite mide un pixel de la UI.</summary>
    public const int PixelesPorUnidad = 16;

    private CanvasScaler escalador;
    private int ultimoAncho, ultimoAlto;

    /// <summary>El mismo calculo que la Pixel Perfect Camera: el mayor entero que entra.</summary>
    public static int Zoom(int anchoPantalla, int altoPantalla) =>
        Mathf.Max(1, Mathf.Min(anchoPantalla / AnchoJuego, altoPantalla / AltoJuego));

    void Awake()
    {
        escalador = GetComponent<CanvasScaler>();
        escalador.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
        // Define cuanto miden los bordes 9-slice; con 100 un borde de 2 px se veia de 0,3.
        escalador.referencePixelsPerUnit = PixelesPorUnidad;
        Aplicar();
    }

    // La ventana puede cambiar de tamaño en cualquier momento (opciones, modo ventana).
    void Update()
    {
        if (Screen.width != ultimoAncho || Screen.height != ultimoAlto)
            Aplicar();
    }

    private void Aplicar()
    {
        ultimoAncho = Screen.width;
        ultimoAlto = Screen.height;
        escalador.scaleFactor = Zoom(ultimoAncho, ultimoAlto);
    }
}
