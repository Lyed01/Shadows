using UnityEngine;

public static class MirrorBlockAbility
{
    public static void RotarReflectivo(Jugador jugador)
    {
        if (jugador == null)
        {
            Debug.LogWarning("❌ MirrorBlockAbility: jugador nulo.");
            return;
        }

        if (!Jugador.ModoHabilidadActivo)
        {
            Debug.Log("⚠ No estás en modo habilidad (Space).");
            return;
        }

        var habilidad = AbilitySelector.Instance?.GetHabilidadActual();
        if (habilidad == null || habilidad.tipo != AbilityType.ReflectiveBlocks)
        {
            Debug.Log("⚠ Habilidad actual no es ReflectiveBlocks.");
            return;
        }

        // Posición del mouse
        Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector2 mousePos2D = new Vector2(mouseWorld.x, mouseWorld.y);

        // Buscar colisión alrededor del cursor
        Collider2D[] hits = Physics2D.OverlapCircleAll(mousePos2D, 0.25f);
        if (hits.Length == 0)
        {
            Debug.Log("⚠ No se detectó ningún objeto cerca del cursor.");
            return;
        }

        MirrorBlock mirror = null;
        foreach (var h in hits)
        {
            mirror = h.GetComponent<MirrorBlock>();
            if (mirror != null) break;
        }

        if (mirror == null)
        {
            Debug.Log("⚠ No se detectó un MirrorBlock bajo el cursor.");
            return;
        }

        float distancia = Vector2.Distance(jugador.transform.position, mirror.transform.position);
        if (distancia > jugador.rangoHabilidad)
        {
            Debug.Log("❌ Bloque fuera del rango de habilidad.");
            return;
        }

        // Rotar el haz
        mirror.RotarHaz();
        AbilityManager.OnUsarHabilidad?.Invoke(); // ✅ Notifica uso de habilidad
        Debug.Log($"🔁 MirrorBlock rotado: {mirror.name}");
    }
}
