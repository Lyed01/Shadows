using System.Collections.Generic;
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

            if (habilidades.ContainsKey(habilidad.Tipo))
            {
                Log.Aviso(this, $"Hay dos habilidades para {habilidad.Tipo}; queda la primera.");
                continue;
            }

            habilidades[habilidad.Tipo] = habilidad;
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
