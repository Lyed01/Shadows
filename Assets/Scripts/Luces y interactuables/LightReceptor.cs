using UnityEngine;

[RequireComponent(typeof(Collider2D), typeof(SpriteRenderer))]
public class LightReceptor : MonoBehaviour
{
    [Header("Acciones al activarse/desactivarse")]
    public Door[] puertas;
    public GameObject[] objetosParaActivar;

    [Header("Control de luces (SpotLight + TopLight unificados)")]
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
    [HideInInspector] public bool estaActivo = false;
    [HideInInspector] public bool yaCambioTipoLuz = false;
    [HideInInspector] public bool noReset = false;


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
        estaActivo = true;
        spriteRenderer.sprite = spriteEncendido;

        foreach (var cfg in lucesControladas)
        {
            if (cfg == null) continue;

            // Si este control cambia el tipo de luz →
            // prevenir múltiples alternancias
            if (cfg.tipo == LightConfigType.SpotLight &&
                cfg.spotSettings != null &&
                cfg.spotSettings.cambiarTipoLuz)
            {
                if (!yaCambioTipoLuz)
                {
                    cfg.Aplicar(true);
                    yaCambioTipoLuz = true;
                }
            }
            else
            {
                // Config normal (no alterna tipo de luz)
                cfg.Aplicar(true);
            }
        }

        foreach (var p in puertas)
            if (p != null)
            {
                if (p.IsOpen)
                    p.Close();
                else
                    p.Open();
            }

        ActivarObjetos();
    }



    private void Desactivar()
    {
        activado = false;
        estaActivo = false;

        spriteRenderer.sprite = spriteApagado;


        AplicarAccionesLuces(false);
        foreach (var p in puertas)
            if (p != null)
                p.ResetToInitialState();

        DesactivarObjetos();

        yaCambioTipoLuz = false;

        Debug.Log($"💤 Receptor {name} desactivado.");
    }


    // ============================================================
    // 🔥 SISTEMA DE CONTROL DE LUCES UNIFICADO
    // ============================================================
    private void AplicarAccionesLuces(bool estadoON)
    {
        foreach (var cfg in lucesControladas)
        {
            if (cfg == null) continue;
            cfg.Aplicar(estadoON);
        }
    }

    // ============================================================
    // 🔓 CONTROL DE PUERTAS
    // ============================================================
    public void AbrirPuertas()
    {
        foreach (var p in puertas)
        {
            if (p == null) continue;

            if (p.IsOpen)
                p.Close();
            else
                p.Open();
        }
    }

    public void CerrarPuertas()
    {
        foreach (var p in puertas)
            if (p != null) p.Close();
    }

    // ============================================================
    // 🔹 CONTROL DE OBJETOS
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

    // ============================================================
    // 🔄 RESET COMPLETOwwwwwwwww
    // ============================================================
    public void ResetReceptor()
    {
        if (noReset) return;

        activado = false;
        estaActivo = false;
        lucesRecibiendo = 0;
        tiempoSinLuz = 0f;

        yaCambioTipoLuz = false; // <-- RESET CRÍTICO

        if (spriteRenderer != null)
            spriteRenderer.sprite = spriteApagado;

        foreach (var obj in objetosParaActivar)
            if (obj != null)
                obj.SetActive(false);

        foreach (var p in puertas)
            if (p != null)
                p.Close();

        foreach (var cfg in lucesControladas)
            if (cfg != null)
                cfg.Reset();
    }

}
