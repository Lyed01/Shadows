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
    [Tooltip("Los casilleros de la barra, de izquierda a derecha. Se ven solo los de las habilidades desbloqueadas.")]
    public SlotDeHabilidad[] slots;
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

        // Update corre tambien en pausa y en las transiciones, y ahi la
        // seleccion no tiene que cambiar.
        if (GameManager.Instance != null && !GameManager.Instance.EstaJugando) return;

        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll != 0f)
            Elegir((indiceActual + (scroll > 0 ? 1 : -1) + habilidadesDesbloqueadas.Count) % habilidadesDesbloqueadas.Count);
        else
            ElegirPorAtajo();
    }

    // Cada habilidad declara su tecla: una que no esta desbloqueada no figura
    // en la lista, asi que su tecla no hace nada.
    private void ElegirPorAtajo()
    {
        for (int i = 0; i < habilidadesDesbloqueadas.Count; i++)
        {
            if (!Input.GetKeyDown(habilidadesDesbloqueadas[i].atajo)) continue;

            Elegir(i);
            return;
        }
    }

    private void Elegir(int indice)
    {
        indiceActual = indice;
        ActualizarUI();
        MostrarNombre();

        Log.Info(this, $"Habilidad seleccionada: {habilidadesDesbloqueadas[indiceActual].nombre}");
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
        for (int i = 0; i < slots.Length; i++)
        {
            if (i < habilidadesDesbloqueadas.Count)
                slots[i].Mostrar(habilidadesDesbloqueadas[i], i == indiceActual);
            else
                slots[i].Ocultar();
        }

        if (nombreActual != null)
            nombreActual.text = habilidadesDesbloqueadas.Count == 0 ? "" : habilidadesDesbloqueadas[indiceActual].nombre;
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
            Agregar(data);
            Log.Info(this, $"Habilidad desbloqueada y agregada al selector: {data.nombre}");
            ActualizarUI();
        }
    }

    // La barra va en el orden de las teclas (1, 2, 3, 4) y no en el de
    // desbloqueo; la elegida sigue siendo la misma aunque cambie de lugar.
    private void Agregar(HabilidadSO nueva)
    {
        HabilidadSO elegida = GetHabilidadActual();

        habilidadesDesbloqueadas.Add(nueva);
        habilidadesDesbloqueadas.Sort((a, b) => ((int)a.atajo).CompareTo((int)b.atajo));

        indiceActual = elegida == null ? 0 : habilidadesDesbloqueadas.IndexOf(elegida);
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
                    Agregar(data);
                    Log.Info(this, $"Sincronizado: {data.nombre}");
                }
            }
        }

        ActualizarUI();
    }

    
  
}
