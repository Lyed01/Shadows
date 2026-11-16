using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(Rigidbody2D))]
public class NPCDemostrador : MonoBehaviour
{
    [Header("Movimiento y animación")]
    public float velocidad = 2f;
    public Animator anim;
    public bool repetir = false;

    [Header("Secuencia")]
    [Tooltip("Los pasos configurados. En runtime se copian a una cola que se consume desde el frente.")]
    public DemostracionPaso[] pasos = new DemostracionPaso[0];

    [Header("Prefabs de habilidades")]
    public GameObject prefabShadowBlock;
    public GameObject prefabAbyssFlame;
    public GameObject prefabMirrorBlock;

    [Header("Sonidos")]
    public AudioClip fxShadowBlock;
    public AudioClip fxAbyssFlame;
    public AudioClip fxMirror;
    public AudioClip fxTeleport;
    public AudioClip morir;

    [Header("Pasos (igual que jugador)")]
    public float pasoIntervalo = 0.35f;
    private float pasoTimer = 0f;

    [Header("Recompensa final")]
    public bool otorgarHabilidadAlFinal = false;
    public AbilityType habilidadEntregada;


    [Header("Condición para iniciar la demostración (opcional)")]
    public bool requiereFlagParaIniciar = false;
    public string flagRequerida;


    // === Internos ===
    private Rigidbody2D rb;
    private bool ejecutando = false;
    private Vector2 ultimaDireccion = Vector2.down;
    private string animActual = "";
    private bool estaMuriendo = false;


    // === AbyssFlame del NPC ===
    private AbyssFlame flameNPC;
    private GridManager grid;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        if (anim == null)
            anim = GetComponent<Animator>();
    }

    public void IniciarDemostracion()
    {
        // Si ya está ejecutando, no hacer nada
        if (ejecutando)
            return;

        // Si hay flag requerida, la chequeamos con un "retry"
        if (requiereFlagParaIniciar)
        {
            StartCoroutine(IntentarIniciarDemostracion());
        }
        else
        {
            // No requiere flag  iniciar inmediatamente
            StartCoroutine(FlujoDemostracion());
        }
    }


    private IEnumerator IntentarIniciarDemostracion()
    {
        for (int i = 0; i < 5; i++) // hasta 5 frames
        {
            yield return null;
            if (SceneStateManager.Instance.HasFlag(flagRequerida))
            {
                StartCoroutine(FlujoDemostracion());
                yield break;
            }
        }

        Log.Info(this, $"{name}: Nunca encontró la flag '{flagRequerida}' después de varios intentos.");
    }

    private IEnumerator FlujoDemostracion()
    {
        ejecutando = true;
        Log.Info(this, $"NPC {name} inicia demostración.");

        do
        {
            // Los pasos se consumen como cola: se saca el primero, se ejecuta y
            // se descarta.
            var cola = new Queue<DemostracionPaso>();
            foreach (var paso in pasos)
                cola.Enqueue(paso);

            while (cola.Count > 0)
                yield return EjecutarPaso(cola.Dequeue());

        } while (repetir);

        if (otorgarHabilidadAlFinal && AbilityManager.Instance)
        {
            AbilityManager.Instance.Unlock(habilidadEntregada);
            Log.Info(this, $"NPC otorgó la habilidad: {habilidadEntregada}");
        }

        ejecutando = false;
    }

    private IEnumerator EjecutarPaso(DemostracionPaso paso)
    {
        switch (paso.tipo)
        {
            case PasoTipo.Morir:
                yield return MorirNPC();
                break;

            case PasoTipo.MoverA:
                if (paso.objetivo)
                    yield return MoverAlPunto(paso.objetivo.position);
                break;

            case PasoTipo.Esperar:
                yield return new WaitForSeconds(paso.duracion);
                break;

            case PasoTipo.MirarA:
                if (paso.objetivo)
                    MirarHacia(paso.objetivo.position);
                break;

            case PasoTipo.UsarHabilidad:
                if (paso.habilidad == AbilityType.ShadowTp)
                {
                    Vector3 destinoTp = paso.objetivo ? paso.objetivo.position : transform.position;
                    yield return TeleportNPC(destinoTp);
                }
                else
                {
                    EjecutarHabilidad(paso.habilidad, paso.objetivo ? paso.objetivo.position : transform.position,
                                      paso.direccionPersonalizada);
                    yield return new WaitForSeconds(1f);
                }
                break;

            case PasoTipo.MoverAbyssFlame:
                if (paso.rutaPuntos != null && paso.rutaPuntos.Length > 0)
                    yield return MoverLlamaPorRuta(paso.rutaPuntos, paso.velocidadLlama);
                break;

            case PasoTipo.Hablar:
                if (paso.dialogo != null)
                {
                    DialogueSystemWorld.Instance?.IniciarDialogo(paso.dialogo, transform);
                    yield return new WaitUntil(() => !DialogueSystemWorld.Instance.EstaActivo);
                }
                break;
        }

        // FLAG DESPUÉS DE COMPLETAR EL PASO
        if (!string.IsNullOrEmpty(paso.flagAlCompletar))
        {
            SceneStateManager.Instance.SetFlag(paso.flagAlCompletar);
            Log.Info(this, "Paso completado Flag activada: " + paso.flagAlCompletar);
        }
    }


    // MOVIMIENTO NPC
    private IEnumerator MoverAlPunto(Vector3 destino)
    {
        Vector2 destino2D = destino;
        while (Vector2.Distance(transform.position, destino2D) > 0.05f)
        {
            Vector2 direccion = (destino2D - (Vector2)transform.position).normalized;
            ultimaDireccion = direccion;
            rb.MovePosition(rb.position + direccion * velocidad * Time.fixedDeltaTime);

            if (direccion.magnitude > 0.1f)
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

            ActualizarAnimacion(direccion);
            yield return new WaitForFixedUpdate();
        }

        ActualizarAnimacion(Vector2.zero);
    }

    private void MirarHacia(Vector3 objetivo)
    {
        Vector3 dir = (objetivo - transform.position).normalized;
        if (dir.x != 0)
            transform.localScale = new Vector3(Mathf.Sign(dir.x), 1f, 1f);
    }

    private void ActualizarAnimacion(Vector2 dir)
    {
        if (anim == null) return;

        // PRIORIDAD: si está muriendo, solo reproducir Dying
        if (estaMuriendo)
        {
            if (animActual != "Dying")
            {
                anim.Play("Dying");
                animActual = "Dying";
            }
            return; // no permitir más cambios
        }

        string nuevaAnim = "Idle";

        if (dir.magnitude > 0.1f)
        {
            if (dir.y > 0.1f)
                nuevaAnim = "WalkUp";
            else if (dir.y < -0.1f)
                nuevaAnim = "WalkDown";
            else
                nuevaAnim = "WalkSide";
        }

        // Flip horizontal
        if (dir.x > 0.1f)
            transform.localScale = Vector3.one;
        else if (dir.x < -0.1f)
            transform.localScale = new Vector3(-1, 1, 1);

        if (animActual != nuevaAnim)
        {
            anim.Play(nuevaAnim);
            animActual = nuevaAnim;
        }
    }


    // HABILIDADES NPC
    private void EjecutarHabilidad(AbilityType tipo, Vector3 pos, Vector2 direccionEspejo)
    {
        if (grid == null) grid = FindFirstObjectByType<GridManager>();
        if (grid != null && grid.sueloTilemap != null)
        {
            Vector3Int celda = grid.sueloTilemap.WorldToCell(pos);
            pos = grid.sueloTilemap.GetCellCenterWorld(celda);
        }
        pos.z = 0;

        switch (tipo)
        {
            case AbilityType.ShadowBlocks:
                if (prefabShadowBlock != null)
                {
                    var bloque = Instantiate(prefabShadowBlock, pos, Quaternion.identity);
                    bloque.name = "ShadowBlock_NPC";

                    if (fxShadowBlock)
                        AudioManager.Instance?.ReproducirFX(fxShadowBlock);
                }
                break;

            case AbilityType.AbyssFlame:
                CrearAbyssFlameNPC(pos);
                break;

            case AbilityType.ReflectiveBlocks:
                CrearMirrorBlock(pos, direccionEspejo);
                break;
        }
    }

    private void CrearAbyssFlameNPC(Vector3 pos)
    {
        if (prefabAbyssFlame == null) return;

        var flame = Instantiate(prefabAbyssFlame, pos, Quaternion.identity);
        flame.name = "AbyssFlame_NPC";

        flameNPC = flame.GetComponent<AbyssFlame>();

        if (flameNPC != null)
        {
            flameNPC.controlNPC = true;
            flameNPC.Inicializar(null); // clave
        }

        if (fxAbyssFlame)
            AudioManager.Instance?.ReproducirFX(fxAbyssFlame);
    }


    // La direccion viene del paso que se esta ejecutando. Antes se buscaba el
    // primer paso de espejo, y con dos espejos el segundo salia con la del primero.
    private void CrearMirrorBlock(Vector3 pos, Vector2 direccion)
    {
        if (prefabMirrorBlock == null) return;

        var mirror = Instantiate(prefabMirrorBlock, pos, Quaternion.identity);
        mirror.name = "MirrorBlock_NPC";

        // SetDireccionInicial ignora Vector2.zero: el espejo queda con la del prefab.
        if (mirror.TryGetComponent(out MirrorBlock comp))
            comp.SetDireccionInicial(direccion);

        if (fxMirror)
            AudioManager.Instance?.ReproducirFX(fxMirror);
    }

    // RUTA PARA ABYSS FLAME
    private IEnumerator MoverLlamaPorRuta(Transform[] ruta, float vel)
    {
        if (flameNPC == null)
        {
            Log.Aviso(this, "NPC no tiene AbyssFlame instanciada.");
            yield break;
        }

        for (int i = 0; i < ruta.Length; i++)
        {
            Transform objetivo = ruta[i];
            Vector2 destino = objetivo.position;

            while (Vector2.Distance(flameNPC.transform.position, destino) > 0.05f)
            {
                Vector2 dir = (destino - (Vector2)flameNPC.transform.position).normalized;
                flameNPC.SetDireccionNPC(dir);

                yield return new WaitForFixedUpdate();
            }

            flameNPC.SetDireccionNPC(Vector2.zero);

            // pequeña pausa opcional
            yield return new WaitForSeconds(0.1f);
        }
    }

    // TELETRANSPORTE NPC
    private IEnumerator TeleportNPC(Vector3 destino)
    {
        if (grid == null) grid = FindFirstObjectByType<GridManager>();
        if (grid != null && grid.sueloTilemap != null)
        {
            Vector3Int celda = grid.sueloTilemap.WorldToCell(destino);
            destino = grid.sueloTilemap.GetCellCenterWorld(celda);
        }
        destino.z = 0;

        if (anim != null)
        {
            anim.Play("Teleport_Disappear");
            yield return new WaitForSeconds(0.25f);
        }

        transform.position = destino;

        if (anim != null)
        {
            anim.Play("Teleport_Appear");
            yield return new WaitForSeconds(0.25f);
        }

        if (fxTeleport)
            AudioManager.Instance?.ReproducirFX(fxTeleport);

        Log.Info(this, $"NPC Teleport a {destino}");
    }

    private void ReproducirPaso()
    {
        if (AudioManager.Instance == null) return;
        // Se busca una vez: antes era un FindFirstObjectByType por cada paso.
        if (grid == null) grid = FindFirstObjectByType<GridManager>();
        if (grid == null) return;

        AudioManager.Instance.ReproducirPaso(grid.EsSueloCorrupto(transform.position));
    }

    private IEnumerator MorirNPC()
    {
        Log.Info(this, $"NPC {name} muere.");

        estaMuriendo = true; // Prioridad a la animación de muerte
        rb.simulated = false;

        // Animación de muerte
        if (anim != null)
            anim.Play("Dying");

        // Esperar duración de la animación
        yield return new WaitForSeconds(0.8f);

        // Sonido de muerte
        if (morir)
            AudioManager.Instance?.ReproducirFX(morir, AudioManager.PrioridadAlta);

        // DAR TIEMPO A QUE SUENE
        yield return new WaitForSeconds(0.3f);

        // ENTREGAR HABILIDAD JUSTO ANTES DE DESAPARECER
        if (otorgarHabilidadAlFinal && AbilityManager.Instance)
        {
            AbilityManager.Instance.Unlock(habilidadEntregada);
            Log.Info(this, $"NPC otorgó la habilidad (después de morir): {habilidadEntregada}");
        }

        // El NPC ahora puede desaparecer
        Destroy(gameObject);
    }
 


}
