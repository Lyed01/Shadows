using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(PlayerAbilityController))]
public class Jugador : MonoBehaviour
{
    [Header("Referencias")]
    public GridManager gridManager;
    public HUDHabilidad hudHabilidad;

    [Header("Prefabs")]
    public GameObject rangoVisualPrefab;

    [Header("Stats")]
    public float velocidad = 5f;
    public float rangoHabilidad = 3f;

    [Header("Audio de pasos")]
    public float pasoIntervalo = 0.35f;
    private float pasoTimer = 0f;

    // === Estado general ===
    private bool tieneHabilidad = false;
    private bool enModoHabilidad = false;
    private bool vivo = true;
    private bool controlActivo = true;
    private bool inputBloqueado = false;

    // === Estado global accesible ===
    public static bool ModoHabilidadActivo { get; private set; } = false;
    public static System.Action OnActivarHabilidad;
    public static System.Action OnDesactivarHabilidad;

    // === Componentes internos ===
    private Animator anim;
    private Rigidbody2D rb;
    private PlayerAbilityController abilityController;
    private GameObject rangoVisual;
    private Vector2 movimiento;
    private Vector2 ultimaDireccion = Vector2.down;
    private string animActual = "";
    private float bloqueoHabilidadTimer = 0f;   // 🔒 tiempo en el que NO se puede mover ni cancelar

    // Guardamos los constraints originales para restaurarlos
    private RigidbodyConstraints2D constraintsOriginal;

    void Awake()
    {
        anim = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();
        abilityController = GetComponent<PlayerAbilityController>();

        constraintsOriginal = rb.constraints;
    }

    // === Inicialización dinámica ===
    public void Inicializar(GridManager gm, HUDHabilidad hud)
    {
        gridManager = gm;
        hudHabilidad = hud;

        // ❗ Solo deshabilitar habilidades si NO estamos en el Hub
        if (SceneManager.GetActiveScene().name != "Hub")
            DeshabilitarHabilidad();

        if (AbilityManager.Instance != null)
        {
            AbilityManager.Instance.OnAbilityUnlocked.AddListener(OnHabilidadDesbloqueada);

            // 🔹 Sincroniza TODAS las habilidades desbloqueadas actuales
            foreach (AbilityType tipo in System.Enum.GetValues(typeof(AbilityType)))
            {
                if (AbilityManager.Instance.IsUnlocked(tipo))
                    OnHabilidadDesbloqueada(tipo);
            }
        }

        vivo = true;
        controlActivo = true;
        inputBloqueado = false;
    }

    void Update()
    {
        if (!vivo || inputBloqueado) return;

        // Leer input una sola vez
        float inputX = Input.GetAxisRaw("Horizontal");
        float inputY = Input.GetAxisRaw("Vertical");
        bool presionoSpace = Input.GetKeyDown(KeyCode.Space);

        // 🔄 Actualizar timer de bloqueo inicial
        if (bloqueoHabilidadTimer > 0f)
            bloqueoHabilidadTimer -= Time.deltaTime;
        // === PRIORIDAD: ya estoy en modo habilidad ===
        // === PRIORIDAD: ya estoy en modo habilidad ===
        if (enModoHabilidad)
        {
            // 1) Mientras dure el bloqueo, ignoramos todo:
            if (bloqueoHabilidadTimer > 0f)
            {
                movimiento = Vector2.zero;
                ActualizarAnimacion();
                return;  // ⚠ NO salimos del modo habilidad, NO dejamos mover
            }

            // 2) Pasado el 1s, ahora sí: Space o WASD sacan del modo habilidad

            // Space de nuevo → salir
            if (presionoSpace)
            {
                DesactivarModoHabilidad();
                // seguís este frame con movimiento normal
            }
            else
            {
                // WASD → salir del modo habilidad
                if (inputX != 0 || inputY != 0)
                {
                    DesactivarModoHabilidad();
                }
                else
                {
                    // Quieto en habilidad
                    movimiento = Vector2.zero;
                    ActualizarAnimacion();
                    return;
                }
            }
        }

        // === Si llegamos acá, NO estamos en modo habilidad ===
        // Toggle para ENTRAR al modo habilidad
        if (presionoSpace && controlActivo)
        {
            if (!EstaUsandoAbyssFlame())
            {
                ActivarModoHabilidad();
                movimiento = Vector2.zero;
                ActualizarAnimacion();
                return; // no procesar movimiento normal en el mismo frame que entrás
            }
        }

        // === Movimiento normal ===
        movimiento = new Vector2(inputX, inputY).normalized;

        if (movimiento != Vector2.zero)
            ultimaDireccion = movimiento;

        ActualizarAnimacion();

        // Flip lateral
        if (ultimaDireccion.x > 0) transform.localScale = new Vector3(1, 1, 1);
        else if (ultimaDireccion.x < 0) transform.localScale = new Vector3(-1, 1, 1);
    }




    void FixedUpdate()
    {

        if (!vivo || inputBloqueado) return;

        // En modo habilidad NO se mueve
        if (enModoHabilidad)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
            return;
        }

        rb.MovePosition(rb.position + movimiento * velocidad * Time.fixedDeltaTime);

        // Sonido de pasos
        if (movimiento.magnitude > 0.1f)
        {
            pasoTimer -= Time.fixedDeltaTime;
            if (pasoTimer <= 0f)
            {
                ReproducirPaso();
                pasoTimer = pasoIntervalo;
            }
        }
        else
        {
            pasoTimer = 0.05f;
        }
    }


    private void ReproducirPaso()
    {
        if (gridManager == null || AudioManager.Instance == null) return;

        Vector3Int cell = gridManager.sueloTilemap.WorldToCell(transform.position);

        bool esCorrupto = false;
        if (gridManager.tileDesbloqueado != null && gridManager.sueloTilemap.GetTile(cell) == gridManager.tileDesbloqueado)
            esCorrupto = true;

        AudioManager.Instance.ReproducirPaso(esCorrupto);
    }

    // === MODO HABILIDAD ===
    void ActivarModoHabilidad()
    {
        enModoHabilidad = true;
        ModoHabilidadActivo = true;

        bloqueoHabilidadTimer = 0.2f;           // ⏱ 1 segundo quieto SÍ O SÍ

        movimiento = Vector2.zero;
        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;
        rb.constraints = RigidbodyConstraints2D.FreezePosition | RigidbodyConstraints2D.FreezeRotation;

        OnActivarHabilidad?.Invoke();
        AudioManager.Instance?.ReproducirHabilidad();

        if (rangoVisualPrefab != null)
        {
            rangoVisual = Instantiate(rangoVisualPrefab, transform.position, Quaternion.identity);
            var pulse = rangoVisual.GetComponent<PulseEffect>();
            if (pulse != null)
                pulse.Configurar(rangoHabilidad, new Color(0.6f, 0.5f, 0.9f, 0.25f));
        }

        gridManager?.MostrarCeldasDisponibles(transform.position, rangoHabilidad);
    }



    // === Desactivar modo habilidad ===
    void DesactivarModoHabilidad()
    {
        enModoHabilidad = false;
        ModoHabilidadActivo = false;

        // 🔓 Restaurar constraints originales
        rb.constraints = constraintsOriginal;

        OnDesactivarHabilidad?.Invoke();

        // 🟣 Desvanecer el pulso cuando se sale del modo habilidad
        if (rangoVisual != null)
        {
            var pulse = rangoVisual.GetComponent<PulseEffect>();
            if (pulse != null)
                pulse.DestruirSuavemente();
            else
                Destroy(rangoVisual);
        }

        gridManager?.OcultarCeldasDisponibles();
        hudHabilidad?.OcultarAviso();
    }

    // === VIDA / MUERTE ===
    public void Matar()
    {
        if (!vivo) return; // evita dobles llamadas

        DesactivarModoHabilidad();
        vivo = false;
        anim?.Play("Dying");
        AudioManager.Instance?.ReproducirMuerte();

        // Reinicia el HUD para el nuevo intento
        hudHabilidad?.Reiniciar();

        // 🔹 Notifica al GridManager
        gridManager?.CorromperCeldas(transform.position);
        gridManager?.NotificarMuerteJugador();
        Switch.ResetearTodos();
        Debug.Log("☠️ Jugador murió, notificando a GameManager...");
    }

    // === HABILIDAD ===
    public void RecibirHabilidad()
    {
        tieneHabilidad = true;

        if (hudHabilidad != null)
        {
            hudHabilidad.gameObject.SetActive(true);
            hudHabilidad.Reiniciar();
        }

        Debug.Log("🔮 Habilidad obtenida por el jugador");
    }

    private void OnHabilidadDesbloqueada(AbilityType tipo)
    {
        switch (tipo)
        {
            case AbilityType.ShadowBlocks:
            case AbilityType.ReflectiveBlocks:
            case AbilityType.AbyssFlame:
            case AbilityType.ShadowTp:
                RecibirHabilidad();
                break;
        }

        Debug.Log($"🧩 Jugador sincronizó habilidad desbloqueada: {tipo}");
    }

    public void DeshabilitarHabilidad()
    {
        tieneHabilidad = false;
        enModoHabilidad = false;
        ModoHabilidadActivo = false;
        hudHabilidad?.gameObject.SetActive(false);

        // Por las dudas, restaurar constraints
        rb.constraints = constraintsOriginal;
    }

    // === ANIMACIONES ===
    void ActualizarAnimacion()
    {
        if (anim == null) return;

        string nuevaAnim = "Idle";

        if (!vivo)
            nuevaAnim = "Dying";
        else if (enModoHabilidad)
        {
            if (ultimaDireccion.y > 0)
                nuevaAnim = "UseAbilityUp";
            else if (ultimaDireccion.y < 0)
                nuevaAnim = "UseAbilityDown";
            else
                nuevaAnim = "UseAbilitySide";
        }
        else if (movimiento.y > 0)
            nuevaAnim = "WalkUp";
        else if (movimiento.y < 0)
            nuevaAnim = "WalkDown";
        else if (movimiento.x != 0)
            nuevaAnim = "WalkSide";
        else
            nuevaAnim = "Idle";

        if (animActual != nuevaAnim)
        {
            anim.Play(nuevaAnim);
            animActual = nuevaAnim;
        }
    }

    // === CONTROL GLOBAL ===
    public void SetControlActivo(bool estado)
    {
        controlActivo = estado;

        if (!estado && rangoVisual != null)
        {
            Destroy(rangoVisual); // 💥 Limpieza de rango visual residual
            rangoVisual = null;
        }
    }

    public bool GetControlActivo() => controlActivo;

    public void SetInputBloqueado(bool estado)
    {
        inputBloqueado = estado;

        if (estado)
        {
            movimiento = Vector2.zero;
            enModoHabilidad = false;
            ModoHabilidadActivo = false;
            rb.constraints = constraintsOriginal;
            OnDesactivarHabilidad?.Invoke();
        }
    }

 


    private bool EstaUsandoAbyssFlame()
    {
        // Si el control está desactivado o el input bloqueado, se asume que la llama está activa
        return !controlActivo || inputBloqueado;
    }

    public bool GetInputBloqueado() => inputBloqueado;
}
