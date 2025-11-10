using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections.Generic;

public class CursorManager : PersistentSingleton<CursorManager>
{
    [Header("Texturas del Cursor")]
    // 0 = Normal, 1 = Hover, 2 = Click, 3 = Habilidad, 4 = Habilidad usada
    public Texture2D[] cursors = new Texture2D[5];

    [Header("Configuración")]
    public Vector2 hotspot = Vector2.zero;
    public CursorMode cursorMode = CursorMode.Auto;

    private int currentState = -1;

    // Estados internos
    private bool modoHabilidad = false;
    private bool habilidadUsada = false;

    protected override void OnBoot()
    {
        base.OnBoot();
        Debug.Log("🟢 CursorManager persistente inicializado.");
        SetCursor(0); // Estado normal inicial

        // Suscribirse a eventos del sistema de habilidad
        Jugador.OnActivarHabilidad += ActivarHabilidad;
        Jugador.OnDesactivarHabilidad += DesactivarHabilidad;
        AbilityManager.OnUsarHabilidad += HabilidadUsada;

        // Reconectar EventSystem en cada escena
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        Jugador.OnActivarHabilidad -= ActivarHabilidad;
        Jugador.OnDesactivarHabilidad -= DesactivarHabilidad;
        AbilityManager.OnUsarHabilidad -= HabilidadUsada;
        UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(UnityEngine.SceneManagement.Scene escena, UnityEngine.SceneManagement.LoadSceneMode modo)
    {
        // Reforzar conexión con el EventSystem al cambiar escena
        if (EventSystem.current != null)
            Debug.Log($"🧩 CursorManager: reconectado al EventSystem de {escena.name}");
        else
            Debug.LogWarning($"⚠️ CursorManager: no se encontró EventSystem en {escena.name}");
    }

    void Update()
    {
        int newState = GetCursorState();
        if (newState != currentState)
        {
            SetCursor(newState);
            currentState = newState;
        }

        if (habilidadUsada)
            StartCoroutine(ResetHabilidadUsada());
    }

    // === Estados del cursor ===
    private int GetCursorState()
    {
        if (habilidadUsada)
            return 4;
        if (modoHabilidad)
            return 3;

        bool hovering = IsPointerOverClickableUI();
        bool clicking = Input.GetMouseButton(0);

        if (clicking) return 2;
        if (hovering) return 1;
        return 0;
    }

    private void SetCursor(int index)
    {
        if (index < 0 || index >= cursors.Length || cursors[index] == null) return;
        Cursor.SetCursor(cursors[index], hotspot, cursorMode);
    }

    private bool IsPointerOverClickableUI()
    {
        if (EventSystem.current == null)
            return false;

        PointerEventData pointerData = new PointerEventData(EventSystem.current)
        {
            position = Input.mousePosition
        };

        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(pointerData, results);

        foreach (var result in results)
        {
            if (result.gameObject.GetComponent<Button>() != null ||
                result.gameObject.GetComponent<Toggle>() != null ||
                result.gameObject.GetComponent<Slider>() != null)
                return true;
        }

        return false;
    }

    // === Eventos del jugador / habilidad ===
    private void ActivarHabilidad() => modoHabilidad = true;
    private void DesactivarHabilidad() => modoHabilidad = false;
    private void HabilidadUsada() => habilidadUsada = true;

    private System.Collections.IEnumerator ResetHabilidadUsada()
    {
        yield return new WaitForSeconds(0.15f);
        habilidadUsada = false;
    }
}
