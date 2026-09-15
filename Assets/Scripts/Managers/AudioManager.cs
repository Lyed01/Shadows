using UnityEngine;
using System.Collections.Generic;

public class AudioManager : PersistentSingleton<AudioManager>
{
    // === PRIORIDAD DE SONIDOS ===
    // Cuando varios NPCs y sistemas piden un FX en el mismo frame (pasos,
    // habilidades, muertes, avisos...) no se reproducen todos: se encolan con
    // una prioridad y cada frame solo suenan los "maxFXPorFrame" mas
    // importantes. El resto se descarta para no saturar de audio al jugador.
    public const float PrioridadBaja = 1f;   // pasos
    public const float PrioridadMedia = 2f;  // bloques, teletransporte, dialogo
    public const float PrioridadAlta = 3f;   // muerte, habilidades especiales

    private readonly struct SolicitudSonido
    {
        public readonly AudioClip Clip;
        public readonly float Pitch;

        public SolicitudSonido(AudioClip clip, float pitch)
        {
            Clip = clip;
            Pitch = pitch;
        }
    }

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
    public void ReproducirFX(AudioClip clip, float prioridad = PrioridadMedia)
    {
        if (clip == null) return;
        colaFX.Enqueue(new SolicitudSonido(clip, Random.Range(0.96f, 1.04f)), prioridad);
    }

    public void ReproducirFX(List<AudioClip> clips, float prioridad = PrioridadMedia)
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

        while (reproducidos < maxFXPorFrame && colaFX.Count > 0)
        {
            SolicitudSonido solicitud = colaFX.Dequeue();
            fxSource.pitch = solicitud.Pitch;
            fxSource.PlayOneShot(solicitud.Clip, volumenFX * multiplicadorGlobal);
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

    // === PUERTAS (volumen reducido al 50 %) ===
    public void ReproducirPuertaAbrir()
    {
        if (sonidoPuertaAbrir == null) return;
        fxSource.pitch = Random.Range(0.96f, 1.04f);
        fxSource.PlayOneShot(sonidoPuertaAbrir, (volumenFX * multiplicadorGlobal) * 0.1f);
    }

    public void ReproducirPuertaCerrar()
    {
        if (sonidoPuertaCerrar == null) return;
        fxSource.pitch = Random.Range(0.96f, 1.04f);
        fxSource.PlayOneShot(sonidoPuertaCerrar, (volumenFX * multiplicadorGlobal) * 0.1f);
    }

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

    public void PlaySfx(AudioClip clip)
    {
        if (clip == null)
        {
            Log.Aviso(this, "AudioManager: intento de reproducir un SFX nulo.");
            return;
        }

        AudioSource sfxSource = GetComponent<AudioSource>();
        if (sfxSource == null)
        {
            sfxSource = gameObject.AddComponent<AudioSource>();
            sfxSource.playOnAwake = false;
        }

        sfxSource.PlayOneShot(clip);
    }
}
