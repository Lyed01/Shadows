using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Cola de eventos de sonido. Quien pide un FX no lo reproduce: deja el pedido
/// en la cola y sigue. En Update el AudioManager atiende los pedidos del frame
/// por prioridad, hasta maxFXPorFrame.
///
/// Separar el pedido de la reproduccion permite dos cosas. Si diez sistemas
/// piden sonidos en el mismo frame, se decide en un solo lugar cuales suenan.
/// Y quien pide no depende de cuantas fuentes de audio hay ni de su estado.
/// </summary>
public class AudioManager : PersistentSingleton<AudioManager>
{
    // === PRIORIDAD DE SONIDOS ===
    // Cuando varios NPCs y sistemas piden un FX en el mismo frame (pasos,
    // habilidades, muertes, avisos...) no se reproducen todos: se encolan con
    // una prioridad y cada frame solo suenan los "maxFXPorFrame" mas
    // importantes. El resto se descarta para no saturar de audio al jugador.
    // La cola atiende primero al numero mas chico, como los puestos de una
    // carrera.
    public const int PrioridadAlta = 1;   // muerte, habilidades especiales
    public const int PrioridadMedia = 2;  // bloques, teletransporte, dialogo
    public const int PrioridadBaja = 3;   // pasos

    private readonly struct SolicitudSonido
    {
        public readonly AudioClip Clip;
        public readonly float Pitch;
        public readonly float Volumen;

        public SolicitudSonido(AudioClip clip, float pitch, float volumen)
        {
            Clip = clip;
            Pitch = pitch;
            Volumen = volumen;
        }
    }

    // Clips que ya sonaron en este frame: diez puertas o pasos con el mismo
    // clip a la vez suenan como uno solo, y no apilados.
    private readonly List<AudioClip> sonaronEsteFrame = new();

    [Header("Límite de sonidos simultáneos")]
    [Tooltip("Cuantos FX de la cola se reproducen como máximo en cada frame. El resto de ese frame se descarta.")]
    public int maxFXPorFrame = 3;

    // Se pide siempre encolar (Enqueue) y muy pocas veces por frame se saca el
    // de mayor prioridad (Dequeue): por eso conviene la implementación
    // estática, que resuelve el alta en O(1) amortizado. Ver PruebaColasDePrioridad.
    private readonly ISimplePriorityQueue<SolicitudSonido> colaFX = new SimpleArrayPriorityQueue<SolicitudSonido>();

    [Header("Multiplicador global de volumen")]
    [Range(0.1f, 3f)] public float multiplicadorGlobal = 1f;

    [Header("Música inicial (opcional)")]
    public AudioClip musicaInicial;

    // === FX PUERTA ===
    [Header("FX Puerta")]
    public AudioClip sonidoPuertaAbrir;
    public AudioClip sonidoPuertaCerrar;

    // === SONIDOS DE INTERFAZ ===
    [Header("FX UI")]
    public AudioClip sonidoUIClick;
    public AudioClip sonidoUIHover;
    public AudioClip sonidoUIToggle;
    public AudioClip sonidoUISlider;
    public AudioClip sonidoUINavegacion;

    [Header("Canales de audio")]
    public AudioSource musicaSource;
    public AudioSource fxSource;
    public AudioSource uiSource;

    [Header("FX Jugador")]
    public List<AudioClip> pasosNormales = new();
    public List<AudioClip> pasosCorruptos = new();

    [Header("Volúmenes base")]
    [Range(0f, 1f)] public float volumenMusica = 0.7f;
    [Range(0f, 1f)] public float volumenFX = 1f;
    [Range(0f, 1f)] public float volumenUI = 0.8f;

    [Header("Clips globales (asignar desde el inspector)")]
    public List<AudioClip> sonidosMuerte = new();
    public List<AudioClip> sonidosBloqueColocado = new();
    public List<AudioClip> sonidosTeleport = new();
    public List<AudioClip> sonidosHabilidadUsada = new();
    public List<AudioClip> sonidosHUDAdvertencia = new();
    public List<AudioClip> sonidosCorromperSuelo = new();

    protected override void OnBoot()
    {
        base.OnBoot();

        musicaSource ??= gameObject.AddComponent<AudioSource>();
        musicaSource.loop = true;

        fxSource ??= gameObject.AddComponent<AudioSource>();
        uiSource ??= gameObject.AddComponent<AudioSource>();

        volumenMusica = SaveSystem.VolumenMusica;
        volumenFX = SaveSystem.VolumenEfectos;
        volumenUI = SaveSystem.VolumenInterfaz;
        multiplicadorGlobal = SaveSystem.VolumenMaestro;

        bool mute = SaveSystem.Silenciado;

        if (mute)
        {
            musicaSource.volume = 0;
            fxSource.volume = 0;
            uiSource.volume = 0;
        }
        else
        {
            ActualizarVolumenes();
        }
    }
    private void Start()
    {
        if (musicaInicial != null)
            ReproducirMusica(musicaInicial);
    }

    private void ActualizarVolumenes()
    {
        musicaSource.volume = volumenMusica * multiplicadorGlobal;
        fxSource.volume = volumenFX * multiplicadorGlobal;
        uiSource.volume = volumenUI * multiplicadorGlobal;
    }

    // === MÉTODOS PRINCIPALES ===

    /// <summary>
    /// No reproduce el clip directamente: lo encola con su prioridad. La cola
    /// se procesa una vez por frame en Update, que decide cuales suenan.
    /// </summary>
    public void ReproducirFX(AudioClip clip, int prioridad = PrioridadMedia, float volumen = 1f)
    {
        if (clip == null) return;
        colaFX.Enqueue(new SolicitudSonido(clip, Random.Range(0.96f, 1.04f), volumen), prioridad);
    }

    /// <summary>Descarta los FX pedidos que todavia no sonaron.</summary>
    public void DescartarFXPendientes() => colaFX.Clear();

    public void ReproducirFX(List<AudioClip> clips, int prioridad = PrioridadMedia)
    {
        if (clips == null || clips.Count == 0) return;
        AudioClip clip = clips[Random.Range(0, clips.Count)];
        ReproducirFX(clip, prioridad);
    }

    private void Update()
    {
        ProcesarColaDeFX();
    }

    /// <summary>
    /// Saca de la cola los "maxFXPorFrame" pedidos de mayor prioridad y los
    /// reproduce. Los que sobran ese frame se descartan: si en un mismo
    /// instante suenan diez pasos y una muerte, la muerte gana el lugar.
    /// </summary>
    private void ProcesarColaDeFX()
    {
        int reproducidos = 0;
        sonaronEsteFrame.Clear();

        while (reproducidos < maxFXPorFrame && colaFX.Count > 0)
        {
            SolicitudSonido solicitud = colaFX.Dequeue();
            if (sonaronEsteFrame.Contains(solicitud.Clip)) continue;
            sonaronEsteFrame.Add(solicitud.Clip);

            fxSource.pitch = solicitud.Pitch;
            fxSource.PlayOneShot(solicitud.Clip, volumenFX * multiplicadorGlobal * solicitud.Volumen);
            reproducidos++;
        }

        colaFX.Clear();
    }

    public void ReproducirUI(AudioClip clip)
    {
        if (clip == null) return;
        uiSource.pitch = Random.Range(0.98f, 1.02f);
        uiSource.PlayOneShot(clip, volumenUI * multiplicadorGlobal);
    }

    public void ReproducirMusica(AudioClip clip)
    {
        if (clip == null) return;
        if (musicaSource.clip == clip && musicaSource.isPlaying) return;
        musicaSource.clip = clip;
        musicaSource.volume = volumenMusica * multiplicadorGlobal;
        musicaSource.Play();
    }

    // === EFECTOS ESPECÍFICOS ===

    public void ReproducirPaso(bool esCorrupto)
    {
        if (esCorrupto)
            ReproducirFX(pasosCorruptos, PrioridadBaja);
        else
            ReproducirFX(pasosNormales, PrioridadBaja);
    }

    public void ReproducirMuerte() => ReproducirFX(sonidosMuerte, PrioridadAlta);
    public void ReproducirBloque() => ReproducirFX(sonidosBloqueColocado);
    public void ReproducirTeleport() => ReproducirFX(sonidosTeleport);
    public void ReproducirHabilidad() => ReproducirFX(sonidosHabilidadUsada, PrioridadAlta);
    public void ReproducirAdvertencia() =>
        ReproducirUI(sonidosHUDAdvertencia.Count > 0 ? sonidosHUDAdvertencia[Random.Range(0, sonidosHUDAdvertencia.Count)] : null);
    public void ReproducirCorromperSuelo() => ReproducirFX(sonidosCorromperSuelo);

    // === PUERTAS (volumen reducido al 10 %) ===
    // Pasan por la cola como el resto: antes sonaban directo y varias puertas
    // a la vez se apilaban.
    private const float VolumenPuertas = 0.1f;
    public void ReproducirPuertaAbrir() => ReproducirFX(sonidoPuertaAbrir, PrioridadMedia, VolumenPuertas);
    public void ReproducirPuertaCerrar() => ReproducirFX(sonidoPuertaCerrar, PrioridadMedia, VolumenPuertas);

    // === UI ===
    public void ReproducirUIClick() => ReproducirUI(sonidoUIClick);
    public void ReproducirUIHover() => ReproducirUI(sonidoUIHover);
    public void ReproducirUIToggle() => ReproducirUI(sonidoUIToggle);
    public void ReproducirUISlider() => ReproducirUI(sonidoUISlider);
    public void ReproducirUINavegacion() => ReproducirUI(sonidoUINavegacion);

    public void DetenerMusica() => musicaSource.Stop();

    // === Ajustes de volumen ===
    public void AjustarVolumenMusica(float v)
    {
        volumenMusica = v;
        musicaSource.volume = v * multiplicadorGlobal;
        GuardarPreferencias();
    }

    public void AjustarVolumenFX(float v)
    {
        volumenFX = v;
        fxSource.volume = v * multiplicadorGlobal;
        GuardarPreferencias();
    }

    public void AjustarVolumenUI(float v)
    {
        volumenUI = v;
        uiSource.volume = v * multiplicadorGlobal;
        GuardarPreferencias();
    }

    public void AjustarVolumenGlobal(float v)
    {
        multiplicadorGlobal = v;
        ActualizarVolumenes();
        GuardarPreferencias();
    }

    private void GuardarPreferencias()
    {
        SaveSystem.VolumenMusica = volumenMusica;
        SaveSystem.VolumenEfectos = volumenFX;
        SaveSystem.VolumenInterfaz = volumenUI;
        SaveSystem.VolumenMaestro = multiplicadorGlobal;
        SaveSystem.Guardar();
    }
}
