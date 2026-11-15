using UnityEngine;

/// <summary>
/// Deja el nivel como estaba al empezar, con una sola llamada.
///
/// Reiniciar un nivel toca siete sistemas que no se conocen entre si: los
/// bloques del grid, las celdas, los switches, los receptores de luz, los
/// focos, las cenitales y las puertas. Antes el GameManager los recorria uno
/// por uno, y cualquier sistema nuevo con estado inicial obligaba a tocarlo.
/// Esta fachada junta esos pasos: quien reinicia no necesita saber que hay
/// detras, y un sistema nuevo se suma aca.
///
/// Es estatica porque no guarda estado: cada vez busca lo que hay en la escena.
/// </summary>
public static class ReinicioDeNivel
{
    public static void Reiniciar(GridManager grid)
    {
        if (grid != null)
        {
            grid.EliminarShadowBlocks();
            grid.ResetearCeldas();
        }

        foreach (var sw in Buscar<Switch>())
            sw.ResetSwitch();

        foreach (var receptor in Buscar<LightReceptor>())
            receptor.ResetReceptor();

        foreach (var foco in Buscar<SpotLightDetector>())
            foco.ResetToInitialState();

        foreach (var cenital in Buscar<TopLightDetector>())
            cenital.ResetToInitialState();

        foreach (var puerta in Buscar<Door>())
            puerta.ResetToInitialState();

        // Todo vuelve a su lugar bajo la sombra: las puertas que cambiaron no suenan.
        AudioManager.Instance?.DescartarFXPendientes();
    }

    // Incluye los inactivos: un objeto apagado durante el nivel tambien tiene
    // que volver a su estado inicial.
    private static T[] Buscar<T>() where T : Object =>
        Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None);
}
