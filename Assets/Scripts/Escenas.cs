/// <summary>
/// Nombres de las escenas que el codigo necesita identificar por nombre.
/// Centralizarlos evita que un renombre de archivo deje comparaciones sueltas
/// apuntando a una escena que ya no existe, y que un error de tipeo pase
/// desapercibido hasta que el jugador llega a esa pantalla.
///
/// Los niveles no estan aca: se identifican por su ScoreThresholdSO, no por
/// comparacion de nombre.
/// </summary>
public static class Escenas
{
    /// <summary>Escena de arranque con los managers persistentes. Nunca instancia jugador.</summary>
    public const string CoreManagers = "CoreManagers";

    public const string SplashScreen = "SplashScreen";
    public const string MainMenu = "MainMenu";
    public const string Tutorial = "Tutorial";

    /// <summary>Pasillo central con una puerta por nivel.</summary>
    public const string Hub = "Hub";

    /// <summary>Cinematica de cierre, al terminar el ultimo nivel.</summary>
    public const string CinematicaFinal = "FinalCutscene";
}
