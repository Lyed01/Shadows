using UnityEngine;

/// <summary>
/// Ejecuta la habilidad que el jugador tiene elegida en el selector.
///
/// No sabe que hace cada habilidad ni cuantas hay: busca la que corresponde al
/// tipo seleccionado y le pide que actue. Antes esto eran dos switch sobre
/// AbilityType, uno para el click izquierdo y otro para el derecho, y sumar
/// una habilidad obligaba a tocar los dos.
/// </summary>
[RequireComponent(typeof(Jugador))]
public class PlayerAbilityController : MonoBehaviour
{
    private Jugador jugador;

    /// <summary>
    /// Las habilidades que el jugador puede usar, por tipo. Cada una se
    /// registra sola con el tipo que declara, asi que sumar una es agregarla
    /// a esta lista y nada mas.
    ///
    /// Diccionario estatico: son cuatro entradas que se cargan una vez en
    /// Awake y no se quitan nunca, y se consultan en cada frame del modo
    /// habilidad. Con tan pocas claves el recorrido es corto, y un solo
    /// arreglo evita pagar un nodo por habilidad.
    /// </summary>
    private readonly ISimpleDictionary<AbilityType, IHabilidad> habilidades =
        new SimpleArrayDictionary<AbilityType, IHabilidad>();

    private void Awake()
    {
        jugador = GetComponent<Jugador>();

        Registrar(
            new HabilidadBloqueSombra(),
            new HabilidadBloqueEspejo(),
            new HabilidadLlamaAbisal(),
            new HabilidadTeletransporte());
    }

    private void Registrar(params IHabilidad[] nuevas)
    {
        foreach (IHabilidad habilidad in nuevas)
        {
            if (habilidad == null) continue;

            // Repetir un tipo es un error de configuracion, no algo esperable:
            // TryAdd deja la primera y avisa en vez de pisarla.
            if (!habilidades.TryAdd(habilidad.Tipo, habilidad))
                Log.Aviso(this, $"Hay dos habilidades para {habilidad.Tipo}; queda la primera.");
        }
    }

    private void Update()
    {
        // Solo activo si el jugador está en modo habilidad
        if (!Jugador.ModoHabilidadActivo) return;

        AbilityData seleccionada = AbilitySelector.Instance?.GetHabilidadActual();
        if (seleccionada == null) return;

        if (!habilidades.TryGetValue(seleccionada.tipo, out IHabilidad habilidad)) return;

        if (Input.GetMouseButtonDown(0))
            habilidad.UsarPrincipal(jugador);

        if (Input.GetMouseButtonDown(1))
            habilidad.UsarSecundaria(jugador);
    }
}
