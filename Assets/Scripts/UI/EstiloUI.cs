using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Guia de estilo de la interfaz. Todo lo que la UI comparte entre pantallas
/// sale de aca, para que un boton nuevo se vea igual que los demas.
///
/// Medidas en pixeles del juego (512x288); EscalaPixelUI las agranda con zoom entero.
///
/// Texto: solo tres tamaños, porque una fuente pixel fuera de su grilla
/// deforma los pixeles.
/// - TextoTitulo (32) y TextoNormal (16): "rainyhearts Pixel".
/// - TextoChico (8): "PixelOperator8 Pixel", para valores, ayudas y descripciones.
///
/// Paneles: sprites 9-slice MarcoPanel (fondo violeta) y MarcoPanelOscuro,
/// recortados del arte de Panel1 y SideBar.
///
/// Controles: texto sin fondo. El seleccionado se pinta de amarillo y lleva un
/// '>' a la izquierda (MarcaDeSeleccion); no depende solo del color.
/// </summary>
public static class EstiloUI
{
    public const int TextoChico = 8;
    public const int TextoNormal = 16;
    public const int TextoTitulo = 32;

    /// <summary>Separacion estandar entre controles de una lista.</summary>
    public const int Espacio = 4;

    public static readonly Color Texto = new Color32(0xE8, 0xE0, 0xF0, 0xFF);
    public static readonly Color TextoSecundario = new Color32(0xA8, 0x98, 0xBC, 0xFF);
    public static readonly Color Seleccion = new Color32(0xFF, 0xD9, 0x66, 0xFF);
    public static readonly Color Apretado = new Color32(0xC9, 0xA6, 0x40, 0xFF);
    public static readonly Color Deshabilitado = new Color32(0x6B, 0x61, 0x78, 0xFF);
    public static readonly Color Peligro = new Color32(0xFF, 0x73, 0x73, 0xFF);

    // Colores del arte de los paneles.
    public static readonly Color BordeOscuro = new Color32(0x3A, 0x31, 0x44, 0xFF);
    public static readonly Color Borde = new Color32(0x80, 0x6B, 0x97, 0xFF);
    public static readonly Color Fondo = new Color32(0x36, 0x27, 0x42, 0xFF);
    public static readonly Color FondoOscuro = new Color32(0x17, 0x12, 0x1C, 0xFF);

    /// <summary>
    /// Estados de un control de texto. El tinte multiplica el color del
    /// grafico, asi que el texto va en blanco y el color sale de aca.
    /// </summary>
    public static ColorBlock Colores(Color normal) => new ColorBlock
    {
        normalColor = normal,
        highlightedColor = Seleccion,
        selectedColor = Seleccion,
        pressedColor = Apretado,
        disabledColor = Deshabilitado,
        colorMultiplier = 1f,
        fadeDuration = 0f,
    };
}
