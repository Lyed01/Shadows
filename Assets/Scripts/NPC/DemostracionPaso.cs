using UnityEngine;

public enum PasoTipo
{
    MoverA,        // El NPC se mueve a un punto
    MirarA,        // Gira hacia un objetivo
    Esperar,       // Espera un tiempo determinado
    UsarHabilidad, // Usa una habilidad en un punto
    Hablar         // Inicia un diálogo

}

[System.Serializable]
public class DemostracionPaso
{
    [Header("Tipo de paso")]
    public PasoTipo tipo;

    [Header("Objetivo (posición o NPC)")]
    public Transform objetivo;

    [Header("Duración (para Esperar)")]
    public float duracion = 1f;

    [Header("Diálogo (para Hablar)")]
    public DialogueData dialogo;

    [Header("Habilidad (para UsarHabilidad)")]
    public AbilityType habilidad;
    [Header("Dirección personalizada (solo ReflectiveBlock)")]
    public Vector2 direccionPersonalizada = Vector2.zero;
}
