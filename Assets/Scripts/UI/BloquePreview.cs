using UnityEngine;
using UnityEngine.Tilemaps;

public class BloquePreview : MonoBehaviour
{
    [Header("Referencias")]
    public Tilemap sueloTilemap;          // Tilemap del suelo
    public SpriteRenderer spriteRenderer; // Sprite del contorno (animado o estático)
    public float zOffset = -0.1f;         // Para que no quede detrás del tilemap

    private Vector3Int ultimaCelda = new Vector3Int(int.MaxValue, int.MaxValue, 0);

    void Awake()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        // Arranca oculto
        spriteRenderer.enabled = false;
    }

    void Update()
    {
        // Si no está en modo habilidad, ocultar
        if (!Jugador.ModoHabilidadActivo)
        {
            spriteRenderer.enabled = false;
            return;
        }

        // Obtener posición del mouse en coordenadas de celda
        Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mousePos.z = 0;
        Vector3Int cellPos = sueloTilemap.WorldToCell(mousePos);

        // Solo recalcular si se movió a otra celda
        if (cellPos != ultimaCelda)
        {
            ultimaCelda = cellPos;

            // ¿Hay un tile de suelo en esa celda?
            bool haySuelo = sueloTilemap.HasTile(cellPos);

            if (haySuelo)
            {
                Vector3 cellCenter = sueloTilemap.GetCellCenterWorld(cellPos);
                transform.position = cellCenter + new Vector3(0, 0, zOffset);
                spriteRenderer.enabled = true;
            }
            else
            {
                spriteRenderer.enabled = false;
            }
        }
    }
}
