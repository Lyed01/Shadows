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
    public bool controlNPC = false;
    private Vector2 direccionNPC = Vector2.zero;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        sprite = GetComponent<SpriteRenderer>();
    }

    public void Inicializar(Jugador owner)
    {
        jugador = owner; // puede ser null si es NPC
        gridManager = owner != null ? owner.gridManager : FindFirstObjectByType<GridManager>();

        // Ignorar colisión con el jugador si lo hay
        if (owner != null)
        {
            var myCol = GetComponent<Collider2D>();
            var playerCol = owner.GetComponent<Collider2D>();
            if (myCol && playerCol)
                Physics2D.IgnoreCollision(myCol, playerCol, true);
        }

        // MUERTE AUTOMÁTICA universal
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
        if (!controlNPC)
        {
            // Control normal del jugador
            movimiento.x = Input.GetAxisRaw("Horizontal");
            movimiento.y = Input.GetAxisRaw("Vertical");
        }
        else
        {
            // Controlado por NPC
            movimiento = direccionNPC;
        }

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

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.collider.CompareTag("Objetos"))
        {
            Physics2D.IgnoreCollision(
                collision.collider,
                GetComponent<Collider2D>()
            );
        }
    }


    public void Extinguir()
    {
        if (!viva) return;
        viva = false;

        anim?.Play("AbyssFlameDie");

        // 🔥 Corromper celda
        if (gridManager != null && gridManager.sueloTilemap != null)
        {
            Vector3Int cell = gridManager.sueloTilemap.WorldToCell(transform.position);
            gridManager.CorromperCeldaUnica(cell);
        }

        // 🔥 SOLO si es del jugador, restaurar cámara y controles
        if (jugador != null)
        {
            var cineCam = Object.FindFirstObjectByType<CinemachineCamera>();
            if (cineCam != null)
                cineCam.Follow = jugador.transform;

            jugador.SetInputBloqueado(false);
            jugador.SetControlActivo(true);
        }

        // 🧽 Finalmente destruir
        Destroy(gameObject, 0.5f);
    }


    public void SetDireccionNPC(Vector2 dir)
    {
        controlNPC = true;
        direccionNPC = dir.normalized;
    }
}
