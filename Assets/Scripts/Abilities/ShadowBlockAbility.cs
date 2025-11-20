using UnityEngine;

public static class ShadowBlockAbility
{
    public static void ColocarBloque(bool reflectante, Jugador jugador)
    {
        if (jugador == null || jugador.gridManager == null || jugador.hudHabilidad == null)
        {
            Debug.LogWarning("⚠️ ShadowBlockAbility: faltan referencias");
            //Chequear cada uno individualmente para ver cual falta
            if (jugador == null)
                Debug.LogWarning("⚠️ ShadowBlockAbility: falta referencia a Jugador");

            if (jugador.gridManager == null)
                Debug.LogWarning("⚠️ ShadowBlockAbility: falta referencia a GridManager");

            if  (jugador.hudHabilidad == null)
                Debug.LogWarning("⚠️ ShadowBlockAbility: falta referencia a HUDHabilidad");

            return;
        }

        var grid = jugador.gridManager;
        var hud = jugador.hudHabilidad;
        var rango = jugador.rangoHabilidad;

        if (!hud.TieneCargas(reflectante ? 2 : 1))
        {
            hud.MostrarAviso("Energía insuficiente.");
            return;
        }
        
        Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mousePos.z = 0;
        
        var resultado = grid.IntentarColocarBloque(mousePos, reflectante, jugador.transform.position, rango);

        switch (resultado)
        {
            case ResultadoColocacion.Exito:
                AudioManager.Instance?.ReproducirBloque();
                hud.UsarCargas(reflectante ? 2 : 1);
                AbilityManager.OnUsarHabilidad?.Invoke(); // ✅ Notifica al sistema de puntuación
                break;

            case ResultadoColocacion.NoExisteCelda:
                hud.MostrarAviso("No hay suelo en ese lugar.");
                break;

            case ResultadoColocacion.CeldaBloqueada:
                hud.MostrarAviso("Esa zona aún no está corrupta.");
                break;

            case ResultadoColocacion.CeldaOcupada:
                hud.MostrarAviso("Ya hay un bloque ahí.");
                break;

            case ResultadoColocacion.FueraDeRango:
                hud.MostrarAviso("Estás demasiado lejos.");
                break;
        }
    }
}
