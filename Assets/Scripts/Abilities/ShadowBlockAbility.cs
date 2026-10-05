using UnityEngine;

public static class ShadowBlockAbility
{
    /// <summary>
    /// Coloca el bloque del tipo pedido donde apunta el mouse. Recibe el id y
    /// no un booleano, así que agregar un tipo nuevo no obliga a tocar esto.
    /// </summary>
    public static void ColocarBloque(string idBloque, Jugador jugador)
    {
        if (jugador == null || jugador.gridManager == null || jugador.hudHabilidad == null)
        {
            Log.Aviso(typeof(ShadowBlockAbility), "faltan referencias");
            //Chequear cada uno individualmente para ver cual falta
            if (jugador == null)
                Log.Aviso(typeof(ShadowBlockAbility), "falta referencia a Jugador");

            if (jugador.gridManager == null)
                Log.Aviso(typeof(ShadowBlockAbility), "falta referencia a GridManager");

            if  (jugador.hudHabilidad == null)
                Log.Aviso(typeof(ShadowBlockAbility), "falta referencia a HUDHabilidad");

            return;
        }

        var grid = jugador.gridManager;
        var hud = jugador.hudHabilidad;
        var rango = jugador.rangoHabilidad;

        // El costo lo declara el prefab del bloque, no esta funcion: antes
        // estaba escrito a mano y daba por sentado que solo habia dos tipos.
        int costo = grid.Fabrica.CostoDe(idBloque);

        if (!hud.TieneCargas(costo))
        {
            hud.MostrarAviso("Energía insuficiente.");
            return;
        }

        Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mousePos.z = 0;

        var resultado = grid.IntentarColocarBloque(mousePos, idBloque, jugador.transform.position, rango);

        switch (resultado)
        {
            case ResultadoColocacion.Exito:
                AudioManager.Instance?.ReproducirBloque();
                hud.UsarCargas(costo);
                AbilityManager.OnUsarHabilidad?.Invoke(); //  Notifica al sistema de puntuación
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
