using UnityEngine;

/// <summary>
/// Las capas de ordenamiento del juego, de atras hacia adelante. Los nombres
/// tienen que coincidir con los de Project Settings > Tags and Layers; la
/// herramienta Shadows > Ordenar capas de render las crea en ese orden.
///
/// - piso, paredes, objetos: los tilemaps del nivel, fijos.
/// - Default: lo que no esta clasificado queda aca, debajo de las luces y las
///   entidades, para que un olvido no tape a Shawy.
/// - luces: haces, halos, lamparas y marcas en el suelo.
/// - entidades: Shawy, NPC, bloques, puertas, switches y receptores. Se tapan
///   entre si por la altura de su base (ver OrdenPorAltura).
/// - efectos: lo que va sobre las entidades, como la vista previa de un bloque.
/// - ui: la interfaz.
/// </summary>
public static class CapasDeRender
{
    public const string Piso = "piso";
    public const string Paredes = "paredes";
    public const string Objetos = "objetos";
    public const string Luces = "luces";
    public const string Entidades = "entidades";
    public const string Efectos = "efectos";
    public const string Ui = "ui";

    /// <summary>De atras hacia adelante, tal como se guardan en el TagManager.</summary>
    public static readonly string[] Orden =
        { Piso, Paredes, Objetos, "Default", Luces, Entidades, Efectos, Ui };

    public static void Aplicar(Renderer renderer, string capa, int orden)
    {
        renderer.sortingLayerName = capa;
        renderer.sortingOrder = orden;
    }
}
