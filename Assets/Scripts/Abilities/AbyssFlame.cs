using UnityEngine;
using Unity.Cinemachine;

[RequireComponent(typeof(Rigidbody2D))]
public class AbyssFlame : MonoBehaviour
{
    [Header("Configuración")]
    public float velocidad = 5f;
    public float duracion = 5f;

    private Rigidbody2D rb;
    private Animator anim;
    private SpriteRenderer sprite;
    private Jugador jugador;
    private GridManager gridManager;
    private bool viva = true;
    private Vector2 movimiento;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        sprite = GetComponent<SpriteRenderer>();
    }

    public void Inicializar(Jugador owner)
    {
        jugador = owner;
        gridManager = owner.gridManager;

        // Ignorar colisión con el jugador
        var myCol = GetComponent<Collider2D>();
        var playerCol = owner.GetComponent<Collider2D>();
        if (myCol && playerCol)
            Physics2D.IgnoreCollision(myCol, playerCol, true);

        // Muerte automática tras X segundos
        Invoke(nameof(Extinguir), duracion);
    }

    void Update()
    {
        if (!viva) return;

        // 💀 Muerte manual con click derecho
        if (Input.GetMouseButtonDown(1))
        {
            Extinguir();
            return;
        }

        // Movimiento
        movimiento.x = Input.GetAxisRaw("Horizontal");
        movimiento.y = Input.GetAxisRaw("Vertical");
        movimiento.Normalize();

        ActualizarAnimacion();
    }

    void FixedUpdate()
    {
        if (!viva) return;
        rb.MovePosition(rb.position + movimiento * velocidad * Time.fixedDeltaTime);
    }

    private void ActualizarAnimacion()
    {
        if (anim == null) return;

        string animName = "AbyssIdle";

        if (movimiento.y > 0)
            animName = "MoveUp";
        else if (movimiento.y < 0)
            animName = "MoveDown";
        else if (movimiento.x != 0)
            animName = "AbyssFlameSide";

        anim.Play(animName);

        // Flip horizontal si se mueve lateralmente
        if (sprite != null && Mathf.Abs(movimiento.x) > 0.01f)
            sprite.flipX = movimiento.x < 0;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Luz roja destruye la llama
        var spot = other.GetComponent<SpotLightDetector>();
        if (spot && spot.tipoLuz == SpotLightDetector.TipoLuz.Roja)
        {
            Extinguir();
            return;
        }

        // Activar switch por contacto
        if (other.CompareTag("Switch"))
            other.SendMessage("ActivarSwitch", SendMessageOptions.DontRequireReceiver);
    }

    private void Extinguir()
    {
        if (!viva) return;
        viva = false;

        anim?.Play("AbyssFlameDie");

        // 🔥 Corromper SOLO el tile exacto bajo la llama
        if (gridManager != null && gridManager.sueloTilemap != null)
        {
            Vector3Int cell = gridManager.sueloTilemap.WorldToCell(transform.position);
            gridManager.CorromperCeldaUnica(cell);
        }

        // 🎥 Restaurar cámara al jugador
        var cineCam = Object.FindFirstObjectByType<CinemachineCamera>();
        if (cineCam != null)
            cineCam.Follow = jugador.transform;

        // ✅ Desbloquear controles del jugador
        jugador?.SetInputBloqueado(false);
        jugador?.SetControlActivo(true);

        Destroy(gameObject, 0.5f);
    }
}
