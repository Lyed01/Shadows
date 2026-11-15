using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Selector de habilidad del HUD: muestra la elegida y cambia con la rueda.
/// Los nombres e iconos salen de los assets HabilidadSO; aca solo se guarda
/// cuales estan desbloqueadas y cual esta elegida.
/// </summary>
public class AbilitySelector : SceneSingleton<AbilitySelector>
{
    [Header("UI del selector")]
    public Image iconoActual;
    public TextMeshProUGUI nombreActual;
    [Tooltip("Segundos que se ve el nombre al cambiar de habilidad, antes de desvanecerse.")]
    public float nombreVisible = 1f;
    public float nombreFundido = 0.5f;

    private List<HabilidadSO> habilidadesDesbloqueadas = new List<HabilidadSO>();
    private int indiceActual = 0;
    private Coroutine nombreCoroutine;

    void OnEnable()
    {
        

        if (AbilityManager.Instance != null)
        {
            AbilityManager.Instance.OnAbilityUnlocked.AddListener(OnAbilityUnlocked);
            AbilityManager.Instance.OnAbilityLocked.AddListener(OnAbilityLocked);

            // SINCRONIZAR habilidades ya desbloqueadas
            SincronizarConAbilityManager();
        }
    }

    void OnDisable()
    {
      

        if (AbilityManager.Instance != null)
        {
            AbilityManager.Instance.OnAbilityUnlocked.RemoveListener(OnAbilityUnlocked);
            AbilityManager.Instance.OnAbilityLocked.RemoveListener(OnAbilityLocked);
        }

        // Si se apaga a mitad del fundido, la corrutina muere y el nombre quedaria a medias.
        if (nombreActual != null) nombreActual.alpha = 0f;
        nombreCoroutine = null;
    }

    void Start()
    {
        // El nombre solo aparece al cambiar de habilidad.
        if (nombreActual != null) nombreActual.alpha = 0f;
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
        MostrarNombre();

        var hab = habilidadesDesbloqueadas[indiceActual];
        Log.Info(this, $"Habilidad seleccionada: {hab.nombre}");
    }

    private void MostrarNombre()
    {
        if (nombreActual == null) return;
        if (nombreCoroutine != null) StopCoroutine(nombreCoroutine);
        nombreCoroutine = StartCoroutine(DesvanecerNombre());
    }

    // Tiempo real, como el aviso de cargas: con el juego en pausa igual se va.
    private IEnumerator DesvanecerNombre()
    {
        nombreActual.alpha = 1f;
        yield return new WaitForSecondsRealtime(nombreVisible);

        for (float t = 0f; t < nombreFundido; t += Time.unscaledDeltaTime)
        {
            nombreActual.alpha = 1f - t / nombreFundido;
            yield return null;
        }

        nombreActual.alpha = 0f;
        nombreCoroutine = null;
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
            iconoActual.sprite = habilidad.iconoSelector;
        }

        if (nombreActual != null)
            nombreActual.text = habilidad.nombre;
    }

    public HabilidadSO GetHabilidadActual()
    {
        if (habilidadesDesbloqueadas.Count == 0) return null;
        return habilidadesDesbloqueadas[indiceActual];
    }

    // === EVENTOS ===
    private void OnAbilityUnlocked(AbilityType tipo)
    {
        HabilidadSO data = DelSelector(tipo);
        if (data != null && !habilidadesDesbloqueadas.Contains(data))
        {
            habilidadesDesbloqueadas.Add(data);
            Log.Info(this, $"Habilidad desbloqueada y agregada al selector: {data.nombre}");

            // Si es la primera habilidad desbloqueada  seleccionarla automáticamente
            if (habilidadesDesbloqueadas.Count == 1)
                indiceActual = 0;

            ActualizarUI();
        }
    }

    private void OnAbilityLocked(AbilityType tipo)
    {
        HabilidadSO data = DelSelector(tipo);
        if (data != null && habilidadesDesbloqueadas.Contains(data))
        {
            habilidadesDesbloqueadas.Remove(data);
            indiceActual = Mathf.Clamp(indiceActual, 0, habilidadesDesbloqueadas.Count - 1);
            Log.Info(this, $"Habilidad bloqueada y removida: {data.nombre}");
            ActualizarUI();
        }
    }

    // Un asset sin nombre de selector no se elige con la rueda (AbilityMode).
    private static HabilidadSO DelSelector(AbilityType tipo)
    {
        HabilidadSO habilidad = HabilidadSO.De(tipo);
        return habilidad != null && !string.IsNullOrEmpty(habilidad.nombre) ? habilidad : null;
    }

    private void SincronizarConAbilityManager()
    {
        if (AbilityManager.Instance == null) return;

        habilidadesDesbloqueadas.Clear();

        foreach (var tipo in System.Enum.GetValues(typeof(AbilityType)))
        {
            if (AbilityManager.Instance.IsUnlocked((AbilityType)tipo))
            {
                var data = DelSelector((AbilityType)tipo);
                if (data != null && !habilidadesDesbloqueadas.Contains(data))
                {
                    habilidadesDesbloqueadas.Add(data);
                    Log.Info(this, $"Sincronizado: {data.nombre}");
                }
            }
        }

        ActualizarUI();
    }

    
  
}
