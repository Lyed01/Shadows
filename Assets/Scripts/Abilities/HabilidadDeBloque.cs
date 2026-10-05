using UnityEngine;

/// <summary>
/// Lo que comparten las habilidades que colocan un bloque en el suelo. Las dos
/// hacen exactamente lo mismo y solo cambian en que tipo de bloque piden, asi
/// que el procedimiento vive aca una sola vez y cada habilidad concreta
/// aporta su id.
/// </summary>
public abstract class HabilidadDeBloque : IHabilidad
{
    public abstract AbilityType Tipo { get; }

    /// <summary>Id con el que la fabrica conoce al bloque que coloca.</summary>
    protected abstract string IdBloque { get; }

    public void UsarPrincipal(Jugador jugador)
    {
        if (!TieneLoNecesario(jugador)) return;

        GridManager grid = jugador.gridManager;
        HUDHabilidad hud = jugador.hudHabilidad;

        // El costo lo declara el prefab del bloque, no esta clase.
        int costo = grid.Fabrica.CostoDe(IdBloque);

        if (!hud.TieneCargas(costo))
        {
            hud.MostrarAviso("Energía insuficiente.");
            return;
        }

        Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mousePos.z = 0;

        ResultadoColocacion resultado = grid.IntentarColocarBloque(
            mousePos, IdBloque, jugador.transform.position, jugador.rangoHabilidad);

        if (resultado == ResultadoColocacion.Exito)
        {
            AudioManager.Instance?.ReproducirBloque();
            hud.UsarCargas(costo);
            AbilityManager.OnUsarHabilidad?.Invoke();
            return;
        }

        hud.MostrarAviso(MensajeDe(resultado));
    }

    public virtual void UsarSecundaria(Jugador jugador) { }

    private static string MensajeDe(ResultadoColocacion resultado)
    {
        switch (resultado)
        {
            case ResultadoColocacion.NoExisteCelda: return "No hay suelo en ese lugar.";
            case ResultadoColocacion.CeldaBloqueada: return "Esa zona aún no está corrupta.";
            case ResultadoColocacion.CeldaOcupada: return "Ya hay un bloque ahí.";
            case ResultadoColocacion.FueraDeRango: return "Estás demasiado lejos.";
            default: return string.Empty;
        }
    }

    /// <summary>Avisa cual referencia falta, en lugar de fallar en silencio.</summary>
    protected static bool TieneLoNecesario(Jugador jugador)
    {
        if (jugador == null)
        {
            Log.Aviso(typeof(HabilidadDeBloque), "falta referencia a Jugador");
            return false;
        }

        if (jugador.gridManager == null)
        {
            Log.Aviso(typeof(HabilidadDeBloque), "falta referencia a GridManager");
            return false;
        }

        if (jugador.hudHabilidad == null)
        {
            Log.Aviso(typeof(HabilidadDeBloque), "falta referencia a HUDHabilidad");
            return false;
        }

        return true;
    }
}

/// <summary>Coloca el bloque de sombra comun.</summary>
public class HabilidadBloqueSombra : HabilidadDeBloque
{
    public override AbilityType Tipo => AbilityType.ShadowBlocks;
    protected override string IdBloque => ShadowBlock.IdSombra;
}

/// <summary>
/// Coloca el bloque espejo. Es la unica habilidad que usa el click derecho:
/// gira el haz del espejo que este bajo el cursor.
/// </summary>
public class HabilidadBloqueEspejo : HabilidadDeBloque
{
    public override AbilityType Tipo => AbilityType.ReflectiveBlocks;
    protected override string IdBloque => ShadowBlock.IdEspejo;

    public override void UsarSecundaria(Jugador jugador)
    {
        if (jugador == null) return;

        // Ya no hace falta preguntar si la habilidad seleccionada es la del
        // espejo: si esto corre, es porque el jugador la tiene elegida.
        Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector2 mousePos2D = new Vector2(mouseWorld.x, mouseWorld.y);

        Collider2D[] hits = Physics2D.OverlapCircleAll(mousePos2D, 0.25f);

        MirrorBlock espejo = null;
        foreach (Collider2D h in hits)
        {
            espejo = h.GetComponent<MirrorBlock>();
            if (espejo != null) break;
        }

        if (espejo == null)
        {
            Log.Info(typeof(HabilidadBloqueEspejo), "No hay un MirrorBlock bajo el cursor.");
            return;
        }

        float distancia = Vector2.Distance(jugador.transform.position, espejo.transform.position);
        if (distancia > jugador.rangoHabilidad)
        {
            Log.Info(typeof(HabilidadBloqueEspejo), "Bloque fuera del rango de habilidad.");
            return;
        }

        espejo.RotarHaz();
        AbilityManager.OnUsarHabilidad?.Invoke();
    }
}
