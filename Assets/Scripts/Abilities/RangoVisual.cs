using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class RangoVisual : MonoBehaviour
{
    private SpriteRenderer sr;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
    }

    /// <summary>
    /// Ajusta el radio visible para que coincida con el rango real del juego (en unidades del mundo)
    /// </summary>
    public void SetRango(float rango)
    {
        if (sr == null || sr.sprite == null) return;

        // Radio actual del sprite en unidades del mundo
        float radioSprite = sr.sprite.bounds.extents.x; // asumimos sprite circular centrado

        // Escala necesaria para que el sprite coincida con el rango real
        float factor = rango / radioSprite;
        transform.localScale = new Vector3(factor, factor, 1f);
    }

    public void SetColor(Color color)
    {
        if (sr != null) sr.color = color;
    }
}
