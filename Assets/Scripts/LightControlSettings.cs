using UnityEngine;

[System.Serializable]
public class LightControlSettings
{
    public SpotLightDetector luz;

    [Header("Cambiar tipo de luz")]
    public bool cambiarTipoLuz = false;

    [Header("Titileo")]
    public bool modificarTitileo = false;
    public bool titilarON = false;

    [Header("Rotación constante")]
    public bool modificarRotacionConstante = false;
    public bool rotacionConstanteON = false;

    [Header("Oscilación")]
    public bool modificarOscilacion = false;
    public bool oscilacionON = false;
    public float nuevoRangoOscilacion;

    [Header("Daño base")]
    public bool modificarDaño = false;
    public float dañoON;
    public float dañoOFF;

    [Header("Alcance")]
    public bool modificarAlcance = false;
    public float alcanceON;
    public float alcanceOFF;

    [Header("Intensidad de luz 2D")]
    public bool modificarIntensidad = false;
    public float intensidadON;
    public float intensidadOFF;
}

