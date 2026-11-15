using UnityEngine;

/// <summary>Lo primero que toca un rayo de luz. Mismos nombres que RaycastHit2D.</summary>
public struct ImpactoDeLuz
{
    public Collider2D collider;
    public Vector2 point;
    public float distance;
}

/// <summary>
/// Tira los rayos de las luces. Paredes, puertas y jugador cortan la luz con su
/// colisionador, como siempre. Los ShadowBlock no: cortan con un rectangulo que
/// mide anchoSombra a lo ancho, cruzado al rayo, y lo mismo que el bloque a lo
/// largo. Asi se ajusta desde el Inspector cuanto tapa un bloque a sus costados
/// sin que la luz se corte antes de llegar a su cara. Con anchoSombra = 1 el
/// rectangulo es el colisionador y no cambia nada.
///
/// Lo usan los focos, las cenitales y el haz del espejo, asi un bloque tapa lo
/// mismo frente a cualquier luz.
/// </summary>
public static class SombraDeBloques
{
    // shortcut: un rayo cruza a lo sumo unos pocos colisionadores; si alguna vez
    // pasa de 32, los mas lejanos se ignoran y habria que agrandar el buffer.
    private static readonly RaycastHit2D[] buffer = new RaycastHit2D[32];
    private static ContactFilter2D filtro;

    /// <summary>
    /// Como Physics2D.Raycast, pero con la sombra de los bloques. Si no toca
    /// nada, collider queda null y distance es el largo pedido.
    /// </summary>
    public static ImpactoDeLuz Lanzar(Vector2 origen, Vector2 dir, float largo, LayerMask mascara,
        ShadowBlock ignorar = null)
    {
        filtro.useTriggers = Physics2D.queriesHitTriggers;
        filtro.SetLayerMask(mascara);

        var mejor = new ImpactoDeLuz { point = origen + dir * largo, distance = largo };

        int cantidad = Physics2D.Raycast(origen, dir, filtro, buffer, largo);
        for (int i = 0; i < cantidad; i++)
        {
            RaycastHit2D h = buffer[i];
            if (h.collider == null || EsBloque(h.collider)) continue;

            if (h.distance < mejor.distance)
                mejor = new ImpactoDeLuz { collider = h.collider, point = h.point, distance = h.distance };
        }

        if (PrimerBloque(origen, dir, mejor.distance, mascara, ignorar, out ShadowBlock bloque, out float t))
            mejor = new ImpactoDeLuz { collider = bloque.Colisionador, point = origen + dir * t, distance = t };

        return mejor;
    }

    /// <summary>Si el colisionador es de un bloque: su sombra se calcula aparte.</summary>
    public static bool EsBloque(Collider2D colisionador) =>
        colisionador.TryGetComponent(out ShadowBlock _);

    /// <summary>
    /// El bloque cuya sombra corta primero al rayo, antes de largoMax.
    /// </summary>
    public static bool PrimerBloque(Vector2 origen, Vector2 dir, float largoMax, LayerMask mascara,
        ShadowBlock ignorar, out ShadowBlock bloque, out float distancia)
    {
        bloque = null;
        distancia = largoMax;

        var activos = ShadowBlock.Activos;
        for (int i = 0; i < activos.Count; i++)
        {
            ShadowBlock b = activos[i];
            if (b == ignorar || b.Colisionador == null || !b.Colisionador.enabled) continue;
            if ((mascara.value & (1 << b.gameObject.layer)) == 0) continue;

            Vector2 mitad = MitadDeSombra(dir, b.Colisionador.bounds.extents, b.AnchoSombra);
            if (CruceConRectangulo(origen, dir, b.Colisionador.bounds.center, mitad, distancia, out float t))
            {
                bloque = b;
                distancia = t;
            }
        }

        return bloque != null;
    }

    /// <summary>
    /// Medio ancho y medio alto de la sombra de un bloque frente a un rayo. El
    /// ancho de sombra va en el eje que cruza al rayo; en el otro queda el tamaño
    /// real, para que la luz corte en la cara del bloque y no antes.
    /// </summary>
    public static Vector2 MitadDeSombra(Vector2 dir, Vector2 mitadReal, float anchoSombra)
    {
        if (Mathf.Abs(dir.y) > Mathf.Abs(dir.x)) mitadReal.x = anchoSombra * 0.5f;
        else mitadReal.y = anchoSombra * 0.5f;
        return mitadReal;
    }

    /// <summary>
    /// Donde entra un rayo a un rectangulo alineado a los ejes (mitad = medio
    /// ancho y medio alto), si entra antes de largoMax. Si el origen ya esta
    /// adentro, la distancia es 0.
    /// </summary>
    public static bool CruceConRectangulo(Vector2 origen, Vector2 dir, Vector2 centro, Vector2 mitad,
        float largoMax, out float distancia)
    {
        distancia = 0f;
        if (mitad.x <= 0f || mitad.y <= 0f) return false;

        float entrada = 0f;
        float salida = largoMax;

        for (int eje = 0; eje < 2; eje++)
        {
            float desde = origen[eje] - centro[eje];
            float paso = dir[eje];

            float m = mitad[eje];

            if (Mathf.Abs(paso) < 1e-6f)
            {
                // Paralelo a este eje: o va por dentro de la franja o no la toca nunca.
                if (desde < -m || desde > m) return false;
                continue;
            }

            float t1 = (-m - desde) / paso;
            float t2 = (m - desde) / paso;
            if (t1 > t2) (t1, t2) = (t2, t1);

            entrada = Mathf.Max(entrada, t1);
            salida = Mathf.Min(salida, t2);
            if (entrada > salida) return false;
        }

        distancia = entrada;
        return true;
    }
}
