using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using UnityEngine.UI;
public class CursorManager : MonoBehaviour
{
    [Header("Texturas del Cursor")]
    // 0 = Normal, 1 = Hover, 2 = Click, 3 = Habilidad, 4 = Habilidad usada
    public Texture2D[] cursors = new Texture2D[5];

    [Header("Configuración")]
    public Vector2 hotspot = Vector2.zero;
    public CursorMode cursorMode = CursorMode.Auto;

    private static CursorManager instance;
    private int currentState = -1;

    // Referencias a estado de juego
    private bool modoHabilidad = false;
    private bool habilidadUsada = false;

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);

        SetCursor(0); // Normal inicial

        // Suscribirse a eventos del sistema de habilidad
        Jugador.OnActivarHabilidad += ActivarHabilidad;
        Jugador.OnDesactivarHabilidad += DesactivarHabilidad;
        AbilityManager.OnUsarHabilidad += HabilidadUsada;
    }

    void OnDestroy()
    {
        Jugador.OnActivarHabilidad -= ActivarHabilidad;
        Jugador.OnDesactivarHabilidad -= DesactivarHabilidad;
        AbilityManager.OnUsarHabilidad -= HabilidadUsada;
    }

    void Update()
    {
        int newState = GetCursorState();

        if (newState != currentState)
        {
            SetCursor(newState);
            currentState = newState;
        }

        // Reset visual de “habilidad usada” después de un corto tiempo
        if (habilidadUsada)
        {
            StartCoroutine(ResetHabilidadUsada());
        }
    }

    private int GetCursorState()
    {
        if (habilidadUsada)
            return 4; // flash de habilidad usada
        if (modoHabilidad)
            return 3; // modo habilidad activo

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

    // ==== Eventos del jugador ====

    private void ActivarHabilidad()
    {
        modoHabilidad = true;
    }

    private void DesactivarHabilidad()
    {
        modoHabilidad = false;
    }

    private void HabilidadUsada()
    {
        habilidadUsada = true;
    }

    private System.Collections.IEnumerator ResetHabilidadUsada()
    {
        yield return new WaitForSeconds(0.15f);
        habilidadUsada = false;
    }
}
