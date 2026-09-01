using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public class PopupFragmentosUI : SceneSingleton<PopupFragmentosUI>
{
    [Header("UI")]
    public CanvasGroup canvasGroup;
    public RectTransform panel;
    public TextMeshProUGUI texto;

    [Header("Animación")]
    public float duracionFade = 0.2f;
    public Vector3 escalaOculto = new Vector3(0.85f, 0.85f, 1f);
    public Vector3 escalaVisible = Vector3.one;

    [Header("Detección")]
    public float distanciaMostrar = 2.2f;
    public LayerMask layerPuertas; // o ignoralo si no usás layers

    private Camera cam;
    private Door[] puertasConFragmentos = new Door[0];
    private Transform jugador;
    private Door puertaActual;
    private Coroutine anim;
    private bool visible = false;
    private Vector3 worldPosObjetivo; // <-- NUEVO (posición que debe seguir)

    protected override void OnAwake()
    {
        cam = Camera.main;

        canvasGroup.alpha = 0f;
        panel.localScale = escalaOculto;
    }

    void Start()
    {
        RefrescarPuertas();

        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) jugador = p.transform;
    }

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        GameManager.OnPlayerSpawned += RegistrarJugador;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        GameManager.OnPlayerSpawned -= RegistrarJugador;
    }

    private void OnSceneLoaded(Scene escena, LoadSceneMode modo) => RefrescarPuertas();

    private void RegistrarJugador(Jugador j)
    {
        if (j != null) jugador = j.transform;
    }

    /// <summary>
    /// Cachea las puertas que exigen fragmentos. Se rearma al cargar una escena,
    /// porque en runtime no se crean puertas nuevas.
    /// </summary>
    private void RefrescarPuertas()
    {
        List<Door> conFragmentos = new();

        foreach (var d in Object.FindObjectsByType<Door>(FindObjectsSortMode.None))
            if (d != null && d.requiereFragmentos)
                conFragmentos.Add(d);

        puertasConFragmentos = conFragmentos.ToArray();
    }

    void Update()
    {
        if (jugador == null) return;

        Door puertaMasCercana = BuscarPuertaCercana();

        if (puertaMasCercana == null)
        {
            Ocultar();
            return;
        }

        puertaActual = puertaMasCercana;

        int actuales = SaveSystem.GetFragmentos(puertaActual.claveFragmentos);
        int necesarios = puertaActual.fragmentosNecesarios;

        Vector3 pos = ObtenerPosicionSobrePuerta(puertaActual);

        Mostrar(pos, actuales, necesarios, puertaMasCercana);

    }

    void LateUpdate()
    {
        if (visible && puertaActual != null)
        {
            // Se reposiciona constantemente encima de la puerta
            worldPosObjetivo = ObtenerPosicionSobrePuerta(puertaActual);
            ActualizarPosicion(worldPosObjetivo);
        }
    }


    // ============================================================
    // BUSCAR PUERTA CERCANA AL JUGADOR
    // ============================================================
    Door BuscarPuertaCercana()
    {
        float mejorDist = Mathf.Infinity;
        Door seleccionada = null;

        foreach (var d in puertasConFragmentos)
        {
            if (d == null) continue;

            float dist = Vector2.Distance(jugador.position, d.transform.position);

            if (dist < distanciaMostrar && dist < mejorDist)
            {
                mejorDist = dist;
                seleccionada = d;
            }
        }

        return seleccionada;
    }

    // ============================================================
    // POSICIÓN
    // ============================================================
    private Vector3 ObtenerPosicionSobrePuerta(Door d)
    {
        Collider2D col = d.GetComponent<Collider2D>();

        if (col != null)
        {
            return new Vector3(
                col.bounds.center.x,
                col.bounds.max.y + 0.45f,
                0f
            );
        }

        return d.transform.position + new Vector3(0f, 1f, 0f);
    }

    private void ActualizarPosicion(Vector3 worldPos)
    {
        if (cam == null)
            cam = Camera.main;

        if (panel == null)
            return;

        Vector3 screenPos = cam.WorldToScreenPoint(worldPos);

        // Forzamos la Z para evitar invisibilidad si está detrás
        if (screenPos.z < 0)
            screenPos.z = 0.1f;

        panel.position = screenPos;
    }


    // ============================================================
    // MOSTRAR / OCULTAR
    // ============================================================
    public void Mostrar(Vector3 pos, int actuales, int necesarios, Door puerta)

    {
        puertaActual = puerta;           // GUARDAMOS LA PUERTA
        worldPosObjetivo = pos;          // GUARDAMOS LA POSICIÓN A SEGUIR

        texto.text = $"{actuales} / {necesarios} fragmentos";

        ActualizarPosicion(worldPosObjetivo);

        if (!visible)
        {
            if (anim != null) StopCoroutine(anim);
            anim = StartCoroutine(Fade(true));
            visible = true;
        }
    }


    public void Ocultar()
    {
        if (!visible) return;

        if (anim != null) StopCoroutine(anim);
        anim = StartCoroutine(Fade(false));
        visible = false;
    }

    private IEnumerator Fade(bool mostrar)
    {
        float t = 0f;
        float ini = canvasGroup.alpha;
        float fin = mostrar ? 1f : 0f;

        Vector3 sIni = panel.localScale;
        Vector3 sFin = mostrar ? escalaVisible : escalaOculto;

        while (t < duracionFade)
        {
            t += Time.unscaledDeltaTime;
            float k = t / duracionFade;

            canvasGroup.alpha = Mathf.Lerp(ini, fin, k);
            panel.localScale = Vector3.Lerp(sIni, sFin, k);

            yield return null;
        }

        canvasGroup.alpha = fin;
        panel.localScale = sFin;
    }
}
