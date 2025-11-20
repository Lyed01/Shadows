using UnityEngine;

[RequireComponent(typeof(Collider2D), typeof(SpriteRenderer))]
public class LightReceptor : MonoBehaviour
{
    [Header("Acciones al activarse/desactivarse")]
    public Door[] puertas;
    public GameObject[] objetosParaActivar;

    [Header("Control individual de luces (mismo sistema que Switch)")]
    public LightControlSettings[] lucesControladas;

    [Header("Sprites visuales")]
    public Sprite spriteApagado;
    public Sprite spriteEncendido;

    [Header("Configuración")]
    public float tiempoDesactivacion = 0.5f;

    private SpriteRenderer spriteRenderer;
    private bool activado = false;
    private float tiempoSinLuz = 0f;
    private int lucesRecibiendo = 0;

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        spriteRenderer.sprite = spriteApagado;

        GetComponent<Collider2D>().isTrigger = true;
    }

    void Update()
    {
        if (activado && lucesRecibiendo == 0)
        {
            tiempoSinLuz += Time.deltaTime;
            if (tiempoSinLuz >= tiempoDesactivacion)
                Desactivar();
        }
        else if (lucesRecibiendo > 0)
        {
            tiempoSinLuz = 0f;
        }

        lucesRecibiendo = 0;
    }

    // ============================================================
    // 🔸 Activación por luz
    // ============================================================
    public void RecibirLuz(SpotLightDetector.TipoLuz tipo)
    {
        lucesRecibiendo++;

        if (!activado)
            Activar();
    }

    private void Activar()
    {

     
        activado = true;
        spriteRenderer.sprite = spriteEncendido;
        
        AplicarAccionesEnLuces(true);
        AbrirPuertas();
        ActivarObjetos();

        Debug.Log($"🔆 Receptor {name} activado.");
    }

    private void Desactivar()
    {
        activado = false;
        spriteRenderer.sprite = spriteApagado;

        AplicarAccionesEnLuces(false);
        CerrarPuertas();
        DesactivarObjetos();

        Debug.Log($"💤 Receptor {name} desactivado.");
    }

    // ============================================================
    // 🔥 CONTROL DE SPOTLIGHT (igual que Switch)
    // ============================================================
    private void AplicarAccionesEnLuces(bool estadoON)
    {
        foreach (var cfg in lucesControladas)
        {
            if (cfg == null || cfg.luz == null) continue;

            var luz = cfg.luz;

            // ---------- Encender / Apagar ----------
            if (cfg.modificarEncendido)
            {
                bool encender = estadoON ? cfg.encendidoON : cfg.encendidoOFF;
                luz.SetLuzActiva(encender);

                if (!encender)
                    continue;
            }

            // ---------- Cambiar tipo de luz ----------
            if (cfg.cambiarTipoLuz && estadoON)
                luz.AlternarTipoLuz();

            // ---------- Titileo ----------
            if (cfg.modificarTitileo)
                luz.titilar = estadoON ? cfg.titilarON : cfg.titilarOFF;

            // ---------- Rotación ----------
            if (cfg.modificarRotacion)
                luz.rotacionConstante = estadoON ? cfg.rotacionON : cfg.rotacionOFF;

            // ---------- Oscilación ----------
            if (cfg.modificarOscilacion)
            {
                luz.oscilacion = estadoON ? cfg.oscilacionON : cfg.oscilacionOFF;

                if (estadoON && cfg.oscilacionON)
                    luz.rangoOscilacion = cfg.rangoOscilacion;
            }

            // ---------- Alcance ----------
            if (cfg.modificarAlcance)
                luz.alcance = estadoON ? cfg.alcanceON : cfg.alcanceOFF;
        }
    }

    // ============================================================
    // 🔓 FUNCIONES NUEVAS PARA CONTROLAR PUERTAS
    // ============================================================
    public void AbrirPuertas()
    {
        foreach (var p in puertas)
        {
            // si la puerta ya està abierta cerrarla 
            if (p.IsOpen)
            {
                if (p != null) p.Close();
            }
            else if (p != null) p.Open();
        }
    }

    public void CerrarPuertas()
    {
        foreach (var p in puertas)
            if (p != null) p.Close();
    }

    // ============================================================
    // 🔹 Control de objetos
    // ============================================================
    private void ActivarObjetos()
    {
        foreach (var obj in objetosParaActivar)
            if (obj != null) obj.SetActive(true);
    }

    private void DesactivarObjetos()
    {
        foreach (var obj in objetosParaActivar)
            if (obj != null) obj.SetActive(false);
    }
}
