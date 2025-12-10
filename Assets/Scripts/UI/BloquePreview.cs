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
        if (!Jugador.ModoHabilidadActivo)
        {
            spriteRenderer.enabled = false;
            return;
        }

        // 1. Tomar mouse → World (sin pixel snapping)
        Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mouseWorld.z = 0;

        // 2. Convertir a celda EXACTA del tilemap
        Vector3Int cellPos = sueloTilemap.WorldToCell(mouseWorld);

        // 3. Solo actualizar si cambió de celda
        if (cellPos != ultimaCelda)
        {
            ultimaCelda = cellPos;

            if (sueloTilemap.HasTile(cellPos))
            {
                // 4. Usar SIEMPRE el CenterWorld del tilemap (no pixel snap)
                Vector3 pos = sueloTilemap.GetCellCenterWorld(cellPos);

                // 5. Asignar posición exacta del grid
                transform.position = pos + new Vector3(0, 0, zOffset);

                spriteRenderer.enabled = true;
            }
            else
            {
                spriteRenderer.enabled = false;
            }
        }
    }

}
