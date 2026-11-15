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
    private float bloqueoHabilidadTimer = 0f;   // tiempo en el que NO se puede mover ni cancelar

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

        // Solo deshabilitar habilidades si NO estamos en el Hub
        if (SceneManager.GetActiveScene().name != Escenas.Hub)
            DeshabilitarHabilidad();

        if (AbilityManager.Instance != null)
        {
            AbilityManager.Instance.OnAbilityUnlocked.AddListener(OnHabilidadDesbloqueada);

            // Con una sola habilidad desbloqueada alcanza: RecibirHabilidad
            // solo enciende y reinicia el HUD, y repetirla no suma nada.
            foreach (AbilityType tipo in System.Enum.GetValues(typeof(AbilityType)))
            {
                if (tipo == AbilityType.AbilityMode || !AbilityManager.Instance.IsUnlocked(tipo)) continue;
                RecibirHabilidad();
                break;
            }
        }

        vivo = true;
        controlActivo = true;
        inputBloqueado = false;
    }

    // El AbilityManager sobrevive a las escenas: sin esto cada jugador muerto
    // quedaba suscrito y un desbloqueo reiniciaba el HUD una vez por muerte.
    void OnDestroy()
    {
        if (AbilityManager.Instance != null)
            AbilityManager.Instance.OnAbilityUnlocked.RemoveListener(OnHabilidadDesbloqueada);
    }

    void Update()
    {
        if (!vivo || inputBloqueado) return;

        // Leer input una sola vez
        float inputX = Input.GetAxisRaw("Horizontal");
        float inputY = Input.GetAxisRaw("Vertical");
        bool presionoSpace = Input.GetKeyDown(KeyCode.Space);

        if (bloqueoHabilidadTimer > 0f)
            bloqueoHabilidadTimer -= Time.deltaTime;

        if (enModoHabilidad)
        {
            // Pasado el bloqueo, Espacio o moverse sacan del modo y este mismo
            // frame ya se camina. Mientras tanto, quieto.
            bool sale = bloqueoHabilidadTimer <= 0f && (presionoSpace || inputX != 0 || inputY != 0);
            if (!sale)
            {
                movimiento = Vector2.zero;
                ActualizarAnimacion();
                return;
            }
            DesactivarModoHabilidad();
        }
        // El mismo Espacio que saca del modo no vuelve a entrar.
        else if (presionoSpace && EntrarModoHabilidad())
            return;

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
        AudioManager.Instance.ReproducirPaso(gridManager.EsSueloCorrupto(transform.position));
    }

    /// <summary>
    /// Hacia donde mira Shawy, en una de las cuatro direcciones que muestra su
    /// animacion: en diagonal gana arriba o abajo, como al caminar.
    /// </summary>
    public Vector2 DireccionMirada =>
        ultimaDireccion.y > 0 ? Vector2.up :
        ultimaDireccion.y < 0 ? Vector2.down :
        transform.localScale.x < 0 ? Vector2.left : Vector2.right;

    // === MODO HABILIDAD ===

    /// <summary>
    /// Frena al jugador y muestra el rango. Lo usan Espacio y el clic con una
    /// habilidad elegida. Devuelve si quedo en el modo.
    /// </summary>
    public bool EntrarModoHabilidad()
    {
        if (enModoHabilidad) return true;
        if (!vivo || !controlActivo || SinControl) return false;

        ActivarModoHabilidad();
        movimiento = Vector2.zero;
        ActualizarAnimacion();
        return true;
    }

    void ActivarModoHabilidad()
    {
        enModoHabilidad = true;
        ModoHabilidadActivo = true;

        // Quieto un instante aunque se siga apretando WASD: el clic tambien
        // entra al modo, y asi colocar caminando frena a Shawy en vez de ignorarlo.
        bloqueoHabilidadTimer = 0.2f;

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
        SoltarModoHabilidad();
        OnDesactivarHabilidad?.Invoke();

        // Desvanecer el pulso cuando se sale del modo habilidad
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

    // Lo que tienen en comun todas las salidas del modo: la posicion se
    // congela al entrar y hay que soltarla siempre.
    private void SoltarModoHabilidad()
    {
        enModoHabilidad = false;
        ModoHabilidadActivo = false;
        rb.constraints = constraintsOriginal;
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

        // Notifica al GridManager
        gridManager?.CorromperCeldas(transform.position);
        gridManager?.NotificarMuerteJugador();
        Log.Info(this, "Murio, notificando a GameManager");
    }

    // === HABILIDAD ===
    public void RecibirHabilidad()
    {
        if (hudHabilidad != null)
        {
            hudHabilidad.gameObject.SetActive(true);
            hudHabilidad.Reiniciar();
        }

        Log.Info(this, "Habilidad obtenida por el jugador");
    }

    private void OnHabilidadDesbloqueada(AbilityType tipo)
    {
        // Todas las habilidades reales habilitan el modo habilidad. La unica
        // excepcion es AbilityMode, que no se desbloquea: solo muestra su
        // cartel. El switch que habia aca listaba las otras cuatro una por una
        // para hacer lo mismo en todas.
        if (tipo != AbilityType.AbilityMode)
            RecibirHabilidad();

        Log.Info(this, $"Jugador sincronizó habilidad desbloqueada: {tipo}");
    }

    public void DeshabilitarHabilidad()
    {
        SoltarModoHabilidad();
        hudHabilidad?.gameObject.SetActive(false);
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
            Destroy(rangoVisual); // Limpieza de rango visual residual
            rangoVisual = null;
        }
    }

    public bool ControlActivo => controlActivo;

    public void SetInputBloqueado(bool estado)
    {
        inputBloqueado = estado;

        if (estado)
        {
            movimiento = Vector2.zero;
            SoltarModoHabilidad();
            OnDesactivarHabilidad?.Invoke();
        }
    }

 


    // Control o input quitados: lo hace la AbyssFlame mientras se maneja.
    private bool SinControl => !controlActivo || inputBloqueado;

    public bool InputBloqueado => inputBloqueado;
}
