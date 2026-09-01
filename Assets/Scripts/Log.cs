using System.Diagnostics;
using UnityEngine;

/// <summary>
/// Registro de mensajes del juego.
///
/// El atributo Conditional hace que el compilador ni siquiera incluya estas
/// llamadas fuera del editor: en el build no cuestan nada, ni siquiera el
/// armado del texto.
///
/// Los errores de configuracion siguen usando Debug.LogError directamente,
/// porque esos si tienen que verse cuando el juego corre fuera del editor.
/// </summary>
public static class Log
{
    /// <summary>
    /// Mensaje desde un componente. Pasar 'this' permite ademas hacer clic en
    /// la consola para seleccionar el objeto que lo emitio.
    /// </summary>
    [Conditional("UNITY_EDITOR")]
    public static void Info(Object origen, string mensaje) =>
        Debug.Log($"[{origen.GetType().Name}] {mensaje}", origen);

    /// <summary>Mensaje desde una clase estatica: Log.Info(typeof(Clase), ...).</summary>
    [Conditional("UNITY_EDITOR")]
    public static void Info(System.Type origen, string mensaje) =>
        Debug.Log($"[{origen.Name}] {mensaje}");

    [Conditional("UNITY_EDITOR")]
    public static void Aviso(Object origen, string mensaje) =>
        Debug.LogWarning($"[{origen.GetType().Name}] {mensaje}", origen);

    [Conditional("UNITY_EDITOR")]
    public static void Aviso(System.Type origen, string mensaje) =>
        Debug.LogWarning($"[{origen.Name}] {mensaje}");
}
