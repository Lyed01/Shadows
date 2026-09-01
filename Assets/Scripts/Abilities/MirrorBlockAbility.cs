using UnityEngine;

public static class MirrorBlockAbility
{
    public static void RotarReflectivo(Jugador jugador)
    {
        if (jugador == null)
        {
            Log.Aviso(typeof(MirrorBlockAbility), "jugador nulo.");
            return;
        }

        if (!Jugador.ModoHabilidadActivo)
        {
            Log.Info(typeof(MirrorBlockAbility), "No estás en modo habilidad (Space).");
            return;
        }

        var habilidad = AbilitySelector.Instance?.GetHabilidadActual();
        if (habilidad == null || habilidad.tipo != AbilityType.ReflectiveBlocks)
        {
            Log.Info(typeof(MirrorBlockAbility), "Habilidad actual no es ReflectiveBlocks.");
            return;
        }

        // Posición del mouse
        Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector2 mousePos2D = new Vector2(mouseWorld.x, mouseWorld.y);

        // Buscar colisión alrededor del cursor
        Collider2D[] hits = Physics2D.OverlapCircleAll(mousePos2D, 0.25f);
        if (hits.Length == 0)
        {
            Log.Info(typeof(MirrorBlockAbility), "No se detectó ningún objeto cerca del cursor.");
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
            Log.Info(typeof(MirrorBlockAbility), "No se detectó un MirrorBlock bajo el cursor.");
            return;
        }

        float distancia = Vector2.Distance(jugador.transform.position, mirror.transform.position);
        if (distancia > jugador.rangoHabilidad)
        {
            Log.Info(typeof(MirrorBlockAbility), "Bloque fuera del rango de habilidad.");
            return;
        }

        // Rotar el haz
        mirror.RotarHaz();
        AbilityManager.OnUsarHabilidad?.Invoke(); //  Notifica uso de habilidad
        Log.Info(typeof(MirrorBlockAbility), $"MirrorBlock rotado: {mirror.name}");
    }
}
