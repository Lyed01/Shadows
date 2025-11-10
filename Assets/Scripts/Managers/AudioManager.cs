using UnityEngine;
using System.Collections.Generic;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

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

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Crear canales si no existen
        musicaSource ??= gameObject.AddComponent<AudioSource>();
        musicaSource.loop = true;

        fxSource ??= gameObject.AddComponent<AudioSource>();
        uiSource ??= gameObject.AddComponent<AudioSource>();

        // Cargar configuraciones previas
        volumenMusica = PlayerPrefs.GetFloat("VolMusica", 0.7f);
        volumenFX = PlayerPrefs.GetFloat("VolEfectos", 1f);
        volumenUI = PlayerPrefs.GetFloat("VolGeneral", 0.8f);
        multiplicadorGlobal = PlayerPrefs.GetFloat("VolMaster", 1f);

        bool mute = PlayerPrefs.GetInt("Mute", 0) == 1;

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

    public void ReproducirFX(AudioClip clip)
    {
        if (clip == null) return;
        fxSource.pitch = Random.Range(0.96f, 1.04f);
        fxSource.PlayOneShot(clip, volumenFX * multiplicadorGlobal);
    }

    public void ReproducirFX(List<AudioClip> clips)
    {
        if (clips == null || clips.Count == 0) return;
        AudioClip clip = clips[Random.Range(0, clips.Count)];
        ReproducirFX(clip);
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
            ReproducirFX(pasosCorruptos);
        else
            ReproducirFX(pasosNormales);
    }

    public void ReproducirMuerte() => ReproducirFX(sonidosMuerte);
    public void ReproducirBloque() => ReproducirFX(sonidosBloqueColocado);
    public void ReproducirTeleport() => ReproducirFX(sonidosTeleport);
    public void ReproducirHabilidad() => ReproducirFX(sonidosHabilidadUsada);
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
        PlayerPrefs.SetFloat("VolMusica", volumenMusica);
        PlayerPrefs.SetFloat("VolEfectos", volumenFX);
        PlayerPrefs.SetFloat("VolGeneral", volumenUI);
        PlayerPrefs.SetFloat("VolMaster", multiplicadorGlobal);
        PlayerPrefs.Save();
    }

    public void PlaySfx(AudioClip clip)
    {
        if (clip == null)
        {
            Debug.LogWarning("⚠️ AudioManager: intento de reproducir un SFX nulo.");
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
