using UnityEngine;
using UnityEngine.UI;

public class OpcionesSonidoUI : MonoBehaviour
{
    [Header("Referencias UI")]
    public Slider sliderGeneral;   // Volumen maestro (puede ir > 1.0)
    public Slider sliderMusica;
    public Slider sliderEfectos;
    public Toggle toggleMute;
    public Button botonAplicar;

    // Valores temporales
    private float volGeneralTemp;
    private float volMusicaTemp;
    private float volEfectosTemp;
    private bool muteTemp;

    void Start()
    {
        // Cargar valores guardados
        volGeneralTemp = PlayerPrefs.GetFloat("VolMaster", 1f);
        volMusicaTemp = PlayerPrefs.GetFloat("VolMusica", 0.7f);
        volEfectosTemp = PlayerPrefs.GetFloat("VolEfectos", 1f);
        muteTemp = PlayerPrefs.GetInt("Mute", 0) == 1;

        // ✅ Permitir valores superiores a 1.0
        if (sliderGeneral)
        {
            sliderGeneral.minValue = 0.1f;
            sliderGeneral.maxValue = 2.5f;
            sliderGeneral.value = volGeneralTemp;
        }

        if (sliderMusica)
        {
            sliderMusica.minValue = 0f;
            sliderMusica.maxValue = 1.5f;
            sliderMusica.value = volMusicaTemp;
        }

        if (sliderEfectos)
        {
            sliderEfectos.minValue = 0f;
            sliderEfectos.maxValue = 1.5f;
            sliderEfectos.value = volEfectosTemp;
        }

        if (toggleMute) toggleMute.isOn = muteTemp;

        // Listeners
        if (sliderGeneral) sliderGeneral.onValueChanged.AddListener(v => { volGeneralTemp = v; ActualizarVolumenEnTiempoReal(); });
        if (sliderMusica) sliderMusica.onValueChanged.AddListener(v => { volMusicaTemp = v; ActualizarVolumenEnTiempoReal(); });
        if (sliderEfectos) sliderEfectos.onValueChanged.AddListener(v => { volEfectosTemp = v; ActualizarVolumenEnTiempoReal(); });
        if (toggleMute) toggleMute.onValueChanged.AddListener(v => { muteTemp = v; ActualizarVolumenEnTiempoReal(); });

        if (botonAplicar)
            botonAplicar.onClick.AddListener(AplicarCambios);

        ActualizarVolumenEnTiempoReal(); // iniciar sincronizado
    }

    public void AplicarCambios()
    {
        if (AudioManager.Instance == null)
        {
            Debug.LogWarning("⚠️ No hay AudioManager activo en la escena.");
            return;
        }

        // Guardar preferencias
        PlayerPrefs.SetFloat("VolMaster", volGeneralTemp);
        PlayerPrefs.SetFloat("VolMusica", volMusicaTemp);
        PlayerPrefs.SetFloat("VolEfectos", volEfectosTemp);
        PlayerPrefs.SetInt("Mute", muteTemp ? 1 : 0);
        PlayerPrefs.Save();

        Debug.Log($"💾 Volúmenes aplicados → Master={volGeneralTemp:F2} | Música={volMusicaTemp:F2} | FX={volEfectosTemp:F2} | Mute={muteTemp}");
    }

    private void ActualizarVolumenEnTiempoReal()
    {
        if (AudioManager.Instance == null) return;

        if (muteTemp)
        {
            AudioManager.Instance.musicaSource.volume = 0f;
            AudioManager.Instance.fxSource.volume = 0f;
            AudioManager.Instance.uiSource.volume = 0f;
            return;
        }

        // 🎚️ Aplica el volumen global (puede ser > 1)
        AudioManager.Instance.AjustarVolumenGlobal(volGeneralTemp);
        AudioManager.Instance.AjustarVolumenMusica(volMusicaTemp);
        AudioManager.Instance.AjustarVolumenFX(volEfectosTemp);
        AudioManager.Instance.AjustarVolumenUI(volGeneralTemp * 0.8f);
    }
}
