using UnityEngine;

/// <summary>
/// Los valores de un tipo de bloque que se afinan jugando. Viven en un asset y
/// no en el prefab para poder tocarlos en play: todos los bloques de ese tipo,
/// incluidos los guardados en el pool, leen el mismo asset, asi que el cambio
/// se ve al instante y queda guardado al salir de play.
/// </summary>
[CreateAssetMenu(fileName = "AjustesDeBloque", menuName = "Shadows/Ajustes de bloque")]
public class AjustesDeBloque : ScriptableObject
{
    [Tooltip("Ancho de lo que tapa el bloque frente a la luz, en celdas. Con 1 tapa exactamente " +
             "su celda; con mas tapa tambien los rayos que pasan cerca de sus costados; con menos " +
             "deja pasar luz por sus bordes. Vale para focos, cenitales y el haz del espejo.")]
    [Min(0f)] public float anchoSombra = 1f;

    [Tooltip("Cuanta luz aguanta antes de romperse. La luz roja lo rompe de una igual. " +
             "Un cambio en play lo toman los bloques que se coloquen despues.")]
    [Min(0.01f)] public float vidaBajoLuz = 5f;
}
