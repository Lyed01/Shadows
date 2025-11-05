using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

[System.Serializable]
public class AbilityData
{
    public string nombre;
    public Sprite icono;
    public AbilityType tipo;
}

public class AbilitySelector : MonoBehaviour
{
    [Header("Base de datos de habilidades posibles (todas)")]
    public List<AbilityData> habilidadesDisponibles = new List<AbilityData>();

    [Header("UI del selector")]
    public Image iconoActual;
    public TextMeshProUGUI nombreActual;

    private List<AbilityData> habilidadesDesbloqueadas = new List<AbilityData>();
    private int indiceActual = 0;

    public static AbilitySelector Instance { get; private set; }

    void Awake()
    {
        Instance = this;
    }

    void OnEnable()
    {
        GameManager.OnPlayerDeath += ReiniciarSelector;
        GameManager.OnLevelRestart += ReiniciarSelector;

        if (AbilityManager.Instance != null)
        {
            AbilityManager.Instance.OnAbilityUnlocked.AddListener(OnAbilityUnlocked);
            AbilityManager.Instance.OnAbilityLocked.AddListener(OnAbilityLocked);

            // 🟢 SINCRONIZAR habilidades ya desbloqueadas
            SincronizarConAbilityManager();
        }
    }

    void OnDisable()
    {
        GameManager.OnPlayerDeath -= ReiniciarSelector;
        GameManager.OnLevelRestart -= ReiniciarSelector;

        if (AbilityManager.Instance != null)
        {
            AbilityManager.Instance.OnAbilityUnlocked.RemoveListener(OnAbilityUnlocked);
            AbilityManager.Instance.OnAbilityLocked.RemoveListener(OnAbilityLocked);
        }
    }

    void Start()
    {
        ActualizarUI();
    }

    void Update()
    {
        if (habilidadesDesbloqueadas.Count == 0) return;

        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll != 0f)
            CambiarHabilidad(scroll > 0 ? 1 : -1);
    }

    private void CambiarHabilidad(int direccion)
    {
        if (habilidadesDesbloqueadas.Count == 0) return;

        indiceActual = (indiceActual + direccion + habilidadesDesbloqueadas.Count) % habilidadesDesbloqueadas.Count;
        ActualizarUI();

        var hab = habilidadesDesbloqueadas[indiceActual];
        Debug.Log($"🔮 Habilidad seleccionada: {hab.nombre}");
    }

    private void ActualizarUI()
    {
        if (habilidadesDesbloqueadas.Count == 0)
        {
            if (iconoActual != null) iconoActual.enabled = false;
            if (nombreActual != null) nombreActual.text = "";
            return;
        }

        var habilidad = habilidadesDesbloqueadas[indiceActual];
        if (iconoActual != null)
        {
            iconoActual.enabled = true;
            iconoActual.sprite = habilidad.icono;
        }

        if (nombreActual != null)
            nombreActual.text = habilidad.nombre;
    }

    public AbilityData GetHabilidadActual()
    {
        if (habilidadesDesbloqueadas.Count == 0) return null;
        return habilidadesDesbloqueadas[indiceActual];
    }

    // === EVENTOS ===
    private void OnAbilityUnlocked(AbilityType tipo)
    {
        AbilityData data = habilidadesDisponibles.Find(h => h.tipo == tipo);
        if (data != null && !habilidadesDesbloqueadas.Contains(data))
        {
            habilidadesDesbloqueadas.Add(data);
            Debug.Log($"✨ Habilidad desbloqueada y agregada al selector: {data.nombre}");

            // Si es la primera habilidad desbloqueada → seleccionarla automáticamente
            if (habilidadesDesbloqueadas.Count == 1)
                indiceActual = 0;

            ActualizarUI();
        }
    }

    private void OnAbilityLocked(AbilityType tipo)
    {
        AbilityData data = habilidadesDisponibles.Find(h => h.tipo == tipo);
        if (data != null && habilidadesDesbloqueadas.Contains(data))
        {
            habilidadesDesbloqueadas.Remove(data);
            indiceActual = Mathf.Clamp(indiceActual, 0, habilidadesDesbloqueadas.Count - 1);
            Debug.Log($"🚫 Habilidad bloqueada y removida: {data.nombre}");
            ActualizarUI();
        }
    }

    // === NUEVO MÉTODO ===
    private void SincronizarConAbilityManager()
    {
        if (AbilityManager.Instance == null) return;

        habilidadesDesbloqueadas.Clear();

        foreach (var tipo in System.Enum.GetValues(typeof(AbilityType)))
        {
            if (AbilityManager.Instance.IsUnlocked((AbilityType)tipo))
            {
                var data = habilidadesDisponibles.Find(h => h.tipo == (AbilityType)tipo);
                if (data != null && !habilidadesDesbloqueadas.Contains(data))
                {
                    habilidadesDesbloqueadas.Add(data);
                    Debug.Log($"🔁 Sincronizado: {data.nombre}");
                }
            }
        }

        ActualizarUI();
    }

    private void ReiniciarSelector()
    {
        if (habilidadesDesbloqueadas.Count == 0) return;

        indiceActual = 0;
        ActualizarUI();

        Debug.Log("♻️ AbilitySelector reiniciado tras evento global del GameManager.");
    }
}
