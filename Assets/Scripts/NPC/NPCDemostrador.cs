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
    public List<DemostracionPaso> pasos = new();

    [Header("Prefabs de habilidades")]
    public GameObject prefabShadowBlock;
    public GameObject prefabAbyssFlame;
    public GameObject prefabMirrorBlock; 
    [Header("Sonidos")]
    public AudioClip fxShadowBlock;
    public AudioClip fxAbyssFlame;
    public AudioClip fxMirror;


    [Header("Recompensa final")]
    public bool otorgarHabilidadAlFinal = false;
    public AbilityType habilidadEntregada;

    // === Internos ===
    private Rigidbody2D rb;
    private bool ejecutando = false;
    private Vector2 ultimaDireccion = Vector2.down;
    private string animActual = "";

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        if (anim == null)
            anim = GetComponent<Animator>();
    }

    public void IniciarDemostracion()
    {
        if (!ejecutando)
            StartCoroutine(FlujoDemostracion());
    }

    private IEnumerator FlujoDemostracion()
    {
        ejecutando = true;
        Debug.Log($"🎬 NPC {name} inicia demostración.");

        do
        {
            foreach (var paso in pasos)
                yield return EjecutarPaso(paso);

        } while (repetir);

        if (otorgarHabilidadAlFinal && AbilityManager.Instance)
        {
            AbilityManager.Instance.Unlock(habilidadEntregada);
            Debug.Log($"✨ NPC otorgó la habilidad: {habilidadEntregada}");
        }

        ejecutando = false;
    }

    private IEnumerator EjecutarPaso(DemostracionPaso paso)
    {
        switch (paso.tipo)
        {
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
                EjecutarHabilidad(paso.habilidad, paso.objetivo ? paso.objetivo.position : transform.position);
                yield return new WaitForSeconds(1f);
                break;

            case PasoTipo.Hablar:
                if (paso.dialogo != null)
                {
                    DialogueSystemWorld.Instance?.IniciarDialogo(paso.dialogo, transform);
                    yield return new WaitUntil(() => !DialogueSystemWorld.Instance.EstaActivo);
                }
                break;
        }
    }

    // === Movimiento ===
    private IEnumerator MoverAlPunto(Vector3 destino)
    {
        Vector2 destino2D = destino;
        while (Vector2.Distance(transform.position, destino2D) > 0.05f)
        {
            Vector2 direccion = (destino2D - (Vector2)transform.position).normalized;
            ultimaDireccion = direccion;
            rb.MovePosition(rb.position + direccion * velocidad * Time.fixedDeltaTime);
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

    // === Animación ===
    private void ActualizarAnimacion(Vector2 dir)
    {
        if (anim == null) return;

        string nuevaAnim = "Idle";

        if (dir.magnitude > 0.1f)
        {
            if (dir.y > 0.1f)
                nuevaAnim = "WalkingUp";
            else if (dir.y < -0.1f)
                nuevaAnim = "WalkingDown";
            else
                nuevaAnim = "WalkingSide";
        }
        else
        {
            nuevaAnim = "Idle";
        }

        // Flip lateral
        if (dir.x > 0.1f)
            transform.localScale = new Vector3(1, 1, 1);
        else if (dir.x < -0.1f)
            transform.localScale = new Vector3(-1, 1, 1);

        if (animActual != nuevaAnim)
        {
            anim.Play(nuevaAnim);
            animActual = nuevaAnim;
        }
    }

    // === Habilidades (sin animaciones) ===
    private void EjecutarHabilidad(AbilityType tipo, Vector3 pos)
    {
        var grid = FindFirstObjectByType<GridManager>();
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
                    Debug.Log($"🧱 NPC creó bloque en {pos}");

                    var sr = bloque.GetComponent<SpriteRenderer>();
                    if (sr != null)
                    {
                        sr.sortingLayerName = "Entidades";
                        sr.sortingOrder = 5;
                    }

                    if (fxShadowBlock)
                        AudioManager.Instance?.ReproducirFX(fxShadowBlock);
                }
                break;

            case AbilityType.AbyssFlame:
                if (prefabAbyssFlame != null)
                {
                    var flame = Instantiate(prefabAbyssFlame, pos, Quaternion.identity);
                    flame.name = "AbyssFlame_NPC";
                    Debug.Log($"🔥 NPC lanzó llama en {pos}");

                    if (fxAbyssFlame)
                        AudioManager.Instance?.ReproducirFX(fxAbyssFlame);
                }
                break;

            case AbilityType.ReflectiveBlocks:
                CrearMirrorBlock(pos); // 💠 ahora sí lo llama
                break;
        }
    }

    private void CrearMirrorBlock(Vector3 pos)
    {
        if (prefabMirrorBlock == null) return;

        var mirror = Instantiate(prefabMirrorBlock, pos, Quaternion.identity);
        mirror.name = "MirrorBlock_NPC";

        var pasoActual = pasos.Find(p => p.tipo == PasoTipo.UsarHabilidad && p.habilidad == AbilityType.ReflectiveBlocks);
        if (pasoActual != null && pasoActual.direccionPersonalizada != Vector2.zero)
        {
            var comp = mirror.GetComponent<MirrorBlock>();
            if (comp != null)
                comp.SetDireccionInicial(pasoActual.direccionPersonalizada); // ✅ en vez de tocar direccionActual
        }

        if (fxMirror)
            AudioManager.Instance?.ReproducirFX(fxMirror);

        Debug.Log($"💠 NPC colocó bloque reflectivo en {pos}");
    }
    


}
