using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

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
    private readonly Dictionary<AbilityType, IHabilidad> habilidades = new();

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
        HabilidadSO seleccionada = AbilitySelector.Instance?.GetHabilidadActual();
        if (seleccionada == null) return;

        if (!habilidades.TryGetValue(seleccionada.tipo, out IHabilidad habilidad)) return;

        // El clic izquierdo frena a Shawy y usa la habilidad en el mismo gesto:
        // antes habia que apretar Espacio de nuevo cada vez que se movia.
        bool principal = Input.GetMouseButtonDown(0);
        if (principal && !Jugador.ModoHabilidadActivo && PuedeEntrarConClic())
            jugador.EntrarModoHabilidad();

        // Las habilidades solo se usan quieto, en el modo habilidad.
        if (!Jugador.ModoHabilidadActivo) return;

        if (principal)
            habilidad.UsarPrincipal(jugador);

        if (Input.GetMouseButtonDown(1))
            habilidad.UsarSecundaria(jugador);
    }

    // Un clic sobre un menu o con el juego en pausa no es para la habilidad.
    private static bool PuedeEntrarConClic() =>
        GameManager.Instance != null
        && GameManager.Instance.EstadoActual == GameManager.GameState.Jugando
        && !(EventSystem.current != null && EventSystem.current.IsPointerOverGameObject());
}
