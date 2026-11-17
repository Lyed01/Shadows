using UnityEngine;

/// <summary>
/// Salida de un nivel de demostracion: al tocarla el jugador, el tiempo entra al
/// ranking y se vuelve al indice. Reemplaza al LevelExitTrigger del nivel original,
/// que cargaria el Hub.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class SalidaDeDemo : MonoBehaviour
{
    private bool usada;

    void Awake() => GetComponent<Collider2D>().isTrigger = true;

    void OnTriggerEnter2D(Collider2D otro)
    {
        if (usada || otro.GetComponent<Jugador>() == null) return;

        usada = true;
        Demostracion.CompletarNivel();
    }
}
