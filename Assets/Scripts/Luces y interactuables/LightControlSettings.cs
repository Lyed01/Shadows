using UnityEngine;

public enum LightConfigType
{
    SpotLight,
    TopLight
}

/// <summary>
/// Lo que un switch o un receptor le hace a una luz cuando se activa o se
/// desactiva. Es un wrapper porque Unity no serializa polimorfismo con campos
/// planos: guarda las dos configuraciones y usa la que indica 'tipo'.
///
/// Los nombres de campo (tipo, spotSettings, topSettings y los de cada una)
/// estan guardados en los niveles: renombrarlos o moverlos de lugar hace que
/// las escenas pierdan lo configurado.
/// </summary>
[System.Serializable]
public class LightControlSettings
{
    [Header("Tipo de luz que controla este elemento")]
    public LightConfigType tipo;

    [Header("Configuración para SpotLight (si tipo = SpotLight)")]
    public SpotLightControlSettings spotSettings;

    [Header("Configuración para TopLight (si tipo = TopLight)")]
    public TopLightControlSettings topSettings;

    private LightControlSettingsBase Actual =>
        tipo == LightConfigType.SpotLight ? spotSettings : topSettings;

    public void Aplicar(bool estadoON) => Actual?.AplicarEstado(estadoON);

    public void Reset()
    {
        var actual = Actual;
        if (actual == null) return;

        actual.ResetInterno();
        if (actual.Luz != null)
            actual.Luz.ResetToInitialState();
    }
}

/// <summary>
/// Lo comun a las dos luces: encender o apagar, cambiar el tipo y el titileo.
/// Cada hija agrega lo suyo en AplicarEspecifico.
/// </summary>
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

    [Header("Cambio de tipo de luz")]
    public bool cambiarTipoLuz = false;

    // El cambio se hace una sola vez por activacion, aunque el switch se
    // vuelva a evaluar: si no, alternaria el tipo en cada llamada.
    [System.NonSerialized] private bool cambioAplicado = false;

    /// <summary>La luz que controla. Null si todavia no se asigno.</summary>
    public abstract LightDetectorBase Luz { get; }

    protected abstract TipoLuz TipoAlActivar { get; }
    protected abstract TipoLuz TipoAlDesactivar { get; }

    public void AplicarEstado(bool estadoON)
    {
        LightDetectorBase luz = Luz;
        if (luz == null) return;

        if (modificarEncendido)
        {
            bool encender = estadoON ? encendidoON : encendidoOFF;
            luz.SetLuzActiva(encender);
            if (!encender) return;
        }

        AplicarTipo(luz, estadoON);

        if (modificarTitileo)
            luz.titilar = estadoON ? titilarON : titilarOFF;

        AplicarEspecifico(estadoON);
    }

    private void AplicarTipo(LightDetectorBase luz, bool estadoON)
    {
        if (!cambiarTipoLuz) return;

        if (estadoON)
        {
            if (cambioAplicado) return;
            luz.SetTipoLuz(TipoAlActivar);
            cambioAplicado = true;
        }
        else
        {
            luz.SetTipoLuz(TipoAlDesactivar);
            cambioAplicado = false;
        }
    }

    /// <summary>Lo que solo existe en este tipo de luz: giro, alcance, movimiento...</summary>
    protected abstract void AplicarEspecifico(bool estadoON);

    public virtual void ResetInterno() => cambioAplicado = false;
}

[System.Serializable]
public class SpotLightControlSettings : LightControlSettingsBase
{
    [Header("Referencia a SpotLight")]
    public SpotLightDetector spot;

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

    public override LightDetectorBase Luz => spot;

    // El foco no declara un tipo: al activar invierte el que traia la escena.
    protected override TipoLuz TipoAlActivar =>
        spot.TipoLuzInicial == TipoLuz.Amarilla ? TipoLuz.Roja : TipoLuz.Amarilla;

    protected override TipoLuz TipoAlDesactivar => spot.TipoLuzInicial;

    protected override void AplicarEspecifico(bool estadoON)
    {
        if (modificarRotacion)
            spot.rotacionConstante = estadoON ? rotacionON : rotacionOFF;

        if (modificarOscilacion)
        {
            spot.oscilacion = estadoON ? oscilacionON : oscilacionOFF;

            if (estadoON && oscilacionON)
                spot.rangoOscilacion = rangoOscilacion;
        }

        if (modificarAlcance)
            spot.alcance = estadoON ? alcanceON : alcanceOFF;
    }
}

[System.Serializable]
public class TopLightControlSettings : LightControlSettingsBase
{
    [Header("Referencia a TopLight")]
    public TopLightDetector top;

    [Header("Tipo de luz al activar y al desactivar")]
    public TipoLuz tipoLuzON;
    public TipoLuz tipoLuzOFF;

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

    public override LightDetectorBase Luz => top;

    protected override TipoLuz TipoAlActivar => tipoLuzON;
    protected override TipoLuz TipoAlDesactivar => tipoLuzOFF;

    public override void ResetInterno()
    {
        base.ResetInterno();

        if (top != null)
            top.SetTipoLuz(top.tipoLuz);
    }

    protected override void AplicarEspecifico(bool estadoON)
    {
        if (modificarMovimiento)
        {
            MovimientoModo modo = estadoON ? movimientoON : movimientoOFF;
            top.moverEntrePuntos = modo switch
            {
                MovimientoModo.ON => true,
                MovimientoModo.OFF => false,
                _ => !top.moverEntrePuntos,
            };
        }

        if (modificarRadio)
            top.radio = estadoON ? radioON : radioOFF;

        if (modificarIntensidadLuz2D)
            top.intensidadLuz2D = estadoON ? intensidadON : intensidadOFF;
    }
}
