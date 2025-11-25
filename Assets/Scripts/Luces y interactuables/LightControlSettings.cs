using UnityEngine;

//
// ===============================================================
// ENUM DEL TIPO DE LUZ
// ===============================================================
public enum LightConfigType
{
    SpotLight,
    TopLight
}

//
// ===============================================================
// WRAPPER SERIALIZABLE (NECESARIO PARA UNITY)
// ===============================================================
[System.Serializable]
public class LightControlSettings
{
    [Header("Tipo de luz que controla este elemento")]
    public LightConfigType tipo;

    [Header("Configuración para SpotLight (si tipo = SpotLight)")]
    public SpotLightControlSettings spotSettings;

    [Header("Configuración para TopLight (si tipo = TopLight)")]
    public TopLightControlSettings topSettings;

    // ============================================================
    // APLICACIÓN UNIFICADA
    // ============================================================
    public void Aplicar(bool estadoON)
    {
        switch (tipo)
        {
            case LightConfigType.SpotLight:
                if (spotSettings != null)
                    spotSettings.AplicarEstado(estadoON);
                break;

            case LightConfigType.TopLight:
                if (topSettings != null)
                    topSettings.AplicarEstado(estadoON);
                break;
        }
    }

    // ============================================================
    // RESET UNIFICADO
    // ============================================================
    public void Reset()
    {
        switch (tipo)
        {
            case LightConfigType.SpotLight:
                if (spotSettings != null)
                {
                    spotSettings.ResetInterno();   // ⭐ FIX CRÍTICO ⭐

                    if (spotSettings.spot != null)
                        spotSettings.spot.ResetToInitialState();
                }
                break;

            case LightConfigType.TopLight:
                if (topSettings != null && topSettings.top != null)
                    topSettings.top.ResetToInitialState();
                break;
        }
    }
}

//
// ===============================================================
// BASE GENÉRICA
// ===============================================================
[System.Serializable]
public abstract class LightControlSettingsBase
{
    [Header("Encender / Apagar")]
    public bool modificarEncendido = false;

    public bool encendidoON = true;
    public bool encendidoOFF = true;

    [Header("Titileo")]
    public bool modificarTitileo = false;
    public bool titilarON = false;
    public bool titilarOFF = false;

    // Cada subclase implementa esto
    public abstract void AplicarEstado(bool estadoON);
}

//
// ===============================================================
// SPOTLIGHT
// ===============================================================
[System.Serializable]
public class SpotLightControlSettings : LightControlSettingsBase
{
    [Header("Referencia a SpotLight")]
    public SpotLightDetector spot;

    [Header("Tipo de luz")]
    public bool cambiarTipoLuz = false;

    // ✔ Evita repetir cambios infinitamente
    [System.NonSerialized]
    private bool cambioAplicado = false;

    // ========== Reset propio ==========
    public void ResetInterno()
    {
        cambioAplicado = false;   // ⭐ Se limpia al reiniciar
    }

    [Header("Rotación constante")]
    public bool modificarRotacion = false;
    public bool rotacionON = false;
    public bool rotacionOFF = false;

    [Header("Oscilación")]
    public bool modificarOscilacion = false;
    public bool oscilacionON = false;
    public bool oscilacionOFF = false;
    public float rangoOscilacion = 45f;

    [Header("Alcance del haz")]
    public bool modificarAlcance = false;
    public float alcanceON = 12f;
    public float alcanceOFF = 8f;

    public override void AplicarEstado(bool estadoON)
    {
        if (spot == null) return;

        // ENCENDIDO / APAGADO
        if (modificarEncendido)
        {
            bool encender = estadoON ? encendidoON : encendidoOFF;
            spot.SetLuzActiva(encender);
            if (!encender) return;
        }

        // =======================================================
        // 🔥 CAMBIO DE COLOR — SOLO UNA VEZ POR ACTIVACIÓN
        // =======================================================
        if (cambiarTipoLuz)
        {
            if (estadoON)
            {
                if (!cambioAplicado)
                {
                    var nuevoTipo =
                        (spot.initTipoLuz == SpotLightDetector.TipoLuz.Amarilla)
                        ? SpotLightDetector.TipoLuz.Roja
                        : SpotLightDetector.TipoLuz.Amarilla;

                    spot.SetTipoLuz(nuevoTipo);
                    cambioAplicado = true;
                }
            }
            else
            {
                // Volver al color original cuando se apaga
                spot.SetTipoLuz(spot.initTipoLuz);
                cambioAplicado = false;
            }
        }

        // TITILEO
        if (modificarTitileo)
            spot.titilar = estadoON ? titilarON : titilarOFF;

        // ROTACIÓN
        if (modificarRotacion)
            spot.rotacionConstante = estadoON ? rotacionON : rotacionOFF;

        // OSCILACIÓN
        if (modificarOscilacion)
        {
            spot.oscilacion = estadoON ? oscilacionON : oscilacionOFF;

            if (estadoON && oscilacionON)
                spot.rangoOscilacion = rangoOscilacion;
        }

        // ALCANCE
        if (modificarAlcance)
            spot.alcance = estadoON ? alcanceON : alcanceOFF;
    }
}

//
// ===============================================================
// TOPLIGHT
// ===============================================================
[System.Serializable]
public class TopLightControlSettings : LightControlSettingsBase
{
    [Header("Referencia a TopLight")]
    public TopLightDetector top;

    [Header("Movimiento entre puntos")]
    public bool modificarMovimiento = false;

    public enum MovimientoModo { ON, OFF, Toggle }
    public MovimientoModo movimientoON = MovimientoModo.ON;
    public MovimientoModo movimientoOFF = MovimientoModo.OFF;

    [Header("Radio del haz")]
    public bool modificarRadio = false;
    public float radioON = 4f;
    public float radioOFF = 4f;

    [Header("Luz 2D (intensidad)")]
    public bool modificarIntensidadLuz2D = false;
    public float intensidadON = 1f;
    public float intensidadOFF = 0f;

    public override void AplicarEstado(bool estadoON)
    {
        if (top == null) return;

        // ENCENDIDO
        if (modificarEncendido)
        {
            bool encender = estadoON ? encendidoON : encendidoOFF;
            top.SetLuzActiva(encender);
            if (!encender) return;
        }

        // TITILEO
        if (modificarTitileo)
            top.titilar = estadoON ? titilarON : titilarOFF;

        // MOVIMIENTO
        if (modificarMovimiento)
        {
            var modo = estadoON ? movimientoON : movimientoOFF;

            switch (modo)
            {
                case MovimientoModo.ON:
                    top.moverEntrePuntos = true;
                    break;

                case MovimientoModo.OFF:
                    top.moverEntrePuntos = false;
                    break;

                case MovimientoModo.Toggle:
                    top.moverEntrePuntos = !top.moverEntrePuntos;
                    break;
            }
        }

        // RADIO
        if (modificarRadio)
            top.radio = estadoON ? radioON : radioOFF;

        // INTENSIDAD LUZ 2D
        if (modificarIntensidadLuz2D)
            top.intensidadLuz2D = estadoON ? intensidadON : intensidadOFF;
    }
}
