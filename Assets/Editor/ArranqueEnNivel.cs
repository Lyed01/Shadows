using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Play desde un nivel arranca en ese nivel, sin pasar por el menu. Funciona con
/// el nivel abierto solo o junto con CoreManagers: el Play arranca siempre por
/// CoreManagers, como el build, y el BootstrapLoader salta al nivel en vez de
/// ir al menu.
///
/// Es solo del editor: no toca la escena de inicio ni el build. El interruptor
/// 'Shadows/Arrancar desde el menu' vuelve al recorrido completo y se guarda
/// por usuario, no en el proyecto.
/// </summary>
[InitializeOnLoad]
public static class ArranqueEnNivel
{
    private const string MenuDesdeElMenu = "Shadows/Arrancar desde el menú";
    private const string ClaveDesdeElMenu = "Shadows.ArrancarDesdeElMenu";
    private const string RutaCoreManagers = "Assets/Scenes/CoreManagers.unity";

    // Escenas que ya son parte del recorrido normal: con ellas el Play no cambia.
    private static readonly string[] EscenasDelRecorrido =
        { Escenas.CoreManagers, Escenas.SplashScreen, Escenas.MainMenu };

    static ArranqueEnNivel()
    {
        EditorApplication.playModeStateChanged += AlCambiarModo;
    }

    private static void AlCambiarModo(PlayModeStateChange cambio)
    {
        if (cambio != PlayModeStateChange.ExitingEditMode && cambio != PlayModeStateChange.EnteredEditMode)
            return;

        EditorSceneManager.playModeStartScene = null;
        SessionState.EraseString(BootstrapLoader.ClaveNivelDeArranque);

        if (cambio == PlayModeStateChange.EnteredEditMode) return;

        var core = AssetDatabase.LoadAssetAtPath<SceneAsset>(RutaCoreManagers);
        if (core == null)
        {
            Debug.LogWarning($"[ArranqueEnNivel] No se encontró {RutaCoreManagers}: el Play arranca sin managers.");
            return;
        }

        // Solo dura este Play: al volver al editor se limpia arriba. Sin esto,
        // Play con el menu abierto arrancaba sin managers ni UI.
        EditorSceneManager.playModeStartScene = core;

        if (EditorPrefs.GetBool(ClaveDesdeElMenu, false)) return;

        Scene nivel = NivelAbierto();
        if (!nivel.IsValid()) return;

        if (SceneUtility.GetBuildIndexByScenePath(nivel.path) < 0)
        {
            Debug.LogWarning($"[ArranqueEnNivel] '{nivel.name}' no está en Build Settings: se arranca por el menú.");
            return;
        }

        SessionState.SetString(BootstrapLoader.ClaveNivelDeArranque, nivel.name);
    }

    /// <summary>
    /// La escena abierta que no es parte del recorrido normal: la activa si lo
    /// es, si no la primera cargada que lo sea.
    /// </summary>
    private static Scene NivelAbierto()
    {
        Scene activa = SceneManager.GetActiveScene();
        if (EsNivel(activa)) return activa;

        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene escena = SceneManager.GetSceneAt(i);
            if (EsNivel(escena)) return escena;
        }

        return default;
    }

    private static bool EsNivel(Scene escena) =>
        escena.IsValid() && escena.isLoaded && !string.IsNullOrEmpty(escena.path) &&
        System.Array.IndexOf(EscenasDelRecorrido, escena.name) < 0;

    [MenuItem(MenuDesdeElMenu)]
    private static void CambiarInterruptor() =>
        EditorPrefs.SetBool(ClaveDesdeElMenu, !EditorPrefs.GetBool(ClaveDesdeElMenu, false));

    [MenuItem(MenuDesdeElMenu, true)]
    private static bool MarcarInterruptor()
    {
        Menu.SetChecked(MenuDesdeElMenu, EditorPrefs.GetBool(ClaveDesdeElMenu, false));
        return true;
    }
}
