using UnityEngine;

/// <summary>
/// Lo que define a un tipo de habilidad: cómo se llama, cómo se ve y cómo se
/// explica. Hay un asset por habilidad en Resources/Habilidades.
///
/// Antes estos datos vivían en dos lugares: un switch en AbilityManager para el
/// popup de desbloqueo y una lista en el Inspector del selector del HUD. Sumar
/// una habilidad obligaba a tocar los dos, y la lista ya se había desfasado de
/// los tipos. Ahora cada tipo de habilidad es un objeto con sus datos, y una
/// habilidad nueva se suma creando el asset.
///
/// Los assets se comparten: el popup, el selector y el controlador reciben la
/// misma instancia, nunca una copia. Lo que cambia en cada partida (si está
/// desbloqueada, cuál está elegida, las cargas) no vive acá sino en los
/// managers, así cinco objetos alcanzan para todo el juego.
///
/// Lo que la habilidad hace no está en el asset: eso es de su estrategia,
/// IHabilidad, que se busca por el mismo tipo.
/// </summary>
[CreateAssetMenu(fileName = "NuevaHabilidad", menuName = "Shadows/Habilidad")]
public class HabilidadSO : ScriptableObject
{
    public AbilityType tipo;

    [Header("Selector del HUD")]
    public string nombre;
    public Sprite iconoSelector;

    [Header("Popup de desbloqueo")]
    public string titulo;
    public Sprite iconoPopup;
    [TextArea(2, 4)] public string descripcion;

    private const string CarpetaEnResources = "Habilidades";

    // Diccionario estatico: cinco entradas que se cargan una vez y no cambian.
    private static ISimpleDictionary<AbilityType, HabilidadSO> catalogo;

    /// <summary>
    /// El asset de un tipo de habilidad, o null si no hay ninguno.
    /// </summary>
    public static HabilidadSO De(AbilityType tipo)
    {
        if (catalogo == null)
            CargarCatalogo();

        return catalogo.TryGetValue(tipo, out HabilidadSO habilidad) ? habilidad : null;
    }

    private static void CargarCatalogo()
    {
        catalogo = new SimpleArrayDictionary<AbilityType, HabilidadSO>();

        HabilidadSO[] assets = Resources.LoadAll<HabilidadSO>(CarpetaEnResources);
        if (assets.Length == 0)
            Debug.LogError($"[HabilidadSO] No hay assets de habilidad en Resources/{CarpetaEnResources}.");

        foreach (HabilidadSO habilidad in assets)
        {
            if (!catalogo.TryAdd(habilidad.tipo, habilidad))
                Debug.LogError($"[HabilidadSO] Hay dos assets para {habilidad.tipo}: queda el primero, se ignora '{habilidad.name}'.");
        }
    }
}
