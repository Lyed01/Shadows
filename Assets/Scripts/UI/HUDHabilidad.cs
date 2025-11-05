using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class HUDHabilidad : MonoBehaviour
{
    public static HUDHabilidad Instance;

    [Header("UI de Cargas")]
    public Image[] iconos;
    public Sprite spriteDisponible;
    public Sprite spriteUsado;

    [Header("Aviso")]
    public TextMeshProUGUI avisoTexto;
    public float duracionAviso = 1f;
    private Coroutine avisoCoroutine;

    [Header("Cargas")]
    public int maxCargas = 4;
    private int cargasDisponibles;

    void Awake()
    {
        Debug.Log($"🧩 HUDHabilidad activo en {gameObject.scene.name}");

        // 🔹 Evita duplicados y persiste entre escenas
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(transform.root.gameObject);

        Reiniciar();

        if (avisoTexto != null)
            avisoTexto.gameObject.SetActive(false);
    }

    void OnEnable()
    {
        // 🔹 Escuchar eventos globales del GameManager
        GameManager.OnPlayerDeath += Reiniciar;
        GameManager.OnLevelRestart += Reiniciar;
    }

    void OnDisable()
    {
        GameManager.OnPlayerDeath -= Reiniciar;
        GameManager.OnLevelRestart -= Reiniciar;
    }

    // === CONSULTAS ===
    public bool TieneCargas(int costo = 1)
    {
        return cargasDisponibles >= costo;
    }

    // === USO / RECUPERACIÓN ===
    public void UsarCargas(int cantidad = 1)
    {
        cargasDisponibles = Mathf.Max(0, cargasDisponibles - cantidad);
        ActualizarHUD();
    }

    public void RecuperarCargas(int cantidad = 1)
    {
        cargasDisponibles = Mathf.Min(maxCargas, cargasDisponibles + cantidad);
        ActualizarHUD();
    }

    public void Reiniciar()
    {
        cargasDisponibles = maxCargas;
        ActualizarHUD();
        Debug.Log("♻️ HUDHabilidad reiniciado o persistente.");
    }

    private void ActualizarHUD()
    {
        if (iconos == null || iconos.Length == 0)
        {
            Debug.LogWarning("⚠️ HUDHabilidad: no hay iconos asignados.");
            return;
        }

        for (int i = 0; i < iconos.Length; i++)
        {
            if (iconos[i] == null) continue; // Protección
            iconos[i].sprite = i < cargasDisponibles ? spriteDisponible : spriteUsado;
        }
    }


    // === AVISOS ===
    public void MostrarAviso(string mensaje)
    {
        if (avisoTexto == null) return;

        if (avisoCoroutine != null)
            StopCoroutine(avisoCoroutine);

        AudioManager.Instance?.ReproducirAdvertencia();
        avisoCoroutine = StartCoroutine(AvisoTemporal(mensaje));
    }

    private IEnumerator AvisoTemporal(string mensaje)
    {
        avisoTexto.text = mensaje;
        avisoTexto.gameObject.SetActive(true);
        yield return new WaitForSeconds(duracionAviso);
        avisoTexto.gameObject.SetActive(false);
    }

    public void OcultarAviso()
    {
        if (avisoCoroutine != null)
            StopCoroutine(avisoCoroutine);

        avisoTexto?.gameObject.SetActive(false);
    }
}
