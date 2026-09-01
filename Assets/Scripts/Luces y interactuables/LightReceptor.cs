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
    //  Activación por luz
    // ============================================================
    public void RecibirLuz(TipoLuz tipo)
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

        // Luces
        foreach (var cfg in lucesControladas)
        {
            if (cfg == null) continue;
            cfg.Aplicar(true);
        }

        //  En vez de abrir/cerrar directamente, RE-EVALUAMOS la lógica de la puerta
        EvaluarPuertas();

        ActivarObjetos();

        Log.Info(this, $"Receptor {name} ACTIVADO");
    }

    private void Desactivar()
    {
        activado = false;
        estaActivo = false;

        spriteRenderer.sprite = spriteApagado;

        AplicarAccionesLuces(false);

        //  Igual que con el Switch: solo pedimos que la puerta se re-evalúe
        EvaluarPuertas();

        DesactivarObjetos();

        Log.Info(this, $"Receptor {name} DESACTIVADO");
    }

    // ============================================================
    //  SISTEMA DE CONTROL DE LUCES UNIFICADO
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
    //  CONTROL DE PUERTAS (CENTRALIZADO)
    // ============================================================
    private void EvaluarPuertas()
    {
        foreach (var p in puertas)
        {
            if (p == null) continue;
            p.Evaluar();
        }
    }

    public void AbrirPuertas()
    {
        //  Si seguís usando esto desde otro lado, dejalo;
        // pero para la lógica "2 receptores requieren abrir puerta", usá siempre EvaluarPuertas().
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
    //  CONTROL DE OBJETOS
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
    //  RESET COMPLETO
    // ============================================================
    public void ResetReceptor()
    {
        if (noReset) return;

        activado = false;
        estaActivo = false;
        lucesRecibiendo = 0;
        tiempoSinLuz = 0f;

        if (spriteRenderer != null)
            spriteRenderer.sprite = spriteApagado;

        foreach (var obj in objetosParaActivar)
            if (obj != null)
                obj.SetActive(false);

        foreach (var cfg in lucesControladas)
            if (cfg != null)
                cfg.Reset();

        //  Otra vez, nada de forzar puerta cerrada: que la puerta se evalúe
        EvaluarPuertas();

        Log.Info(this, $"Receptor {name} reseteado.");
    }
}
