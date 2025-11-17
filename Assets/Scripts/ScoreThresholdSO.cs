using UnityEngine;

[CreateAssetMenu(
    fileName = "NewLevelScoreConfig",
    menuName = "Shadows/Level Score Config",
    order = 1)]
public class ScoreThresholdSO : ScriptableObject
{
    [Header("ID del nivel (debe coincidir con el nombre de la escena)")]
    public string idNivel = "Nivel1";

    [Header("Muertes (límites para 3-2-1-0)")]
    public int muertes3 = 1;
    public int muertes2 = 2;
    public int muertes1 = 3;

    [Header("Tiempo (segundos límites para 3-2-1-0)")]
    public float tiempo3 = 60f;
    public float tiempo2 = 120f;
    public float tiempo1 = 300f;

    [Header("Uso de habilidades (límites para 3-2-1-0)")]
    public int habilidades3 = 2;
    public int habilidades2 = 4;
    public int habilidades1 = 6;
}
