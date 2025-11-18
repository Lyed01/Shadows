using UnityEngine;

[System.Serializable]
public class LightControlSettings
{
    [Header("Referencia")]
    public SpotLightDetector luz;

    // ------------ Encender / Apagar ------------
    [Header("Encender / Apagar Spotlight")]
    public bool modificarEncendido = false;
    public bool encendidoON = true;   // Estado de la luz cuando el switch/receptor está ON
    public bool encendidoOFF = true;  // Estado de la luz cuando está OFF (puede ser false para apagarla)

    // ------------ Tipo de luz ------------
    [Header("Cambiar tipo de luz (Amarilla/Roja)")]
    public bool cambiarTipoLuz = false;

    // ------------ Titileo ------------
    [Header("Titileo")]
    public bool modificarTitileo = false;
    public bool titilarON = false;
    public bool titilarOFF = false;

    // ------------ Rotación constante ------------
    [Header("Rotación constante")]
    public bool modificarRotacion = false;
    public bool rotacionON = false;
    public bool rotacionOFF = false;

    // ------------ Oscilación ------------
    [Header("Oscilación")]
    public bool modificarOscilacion = false;
    public bool oscilacionON = false;
    public bool oscilacionOFF = false;
    public float rangoOscilacion = 45f;

    // ------------ Alcance ------------
    [Header("Alcance del haz")]
    public bool modificarAlcance = false;
    public float alcanceON = 12f;
    public float alcanceOFF = 8f;
}
