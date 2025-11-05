using UnityEngine;

/// <summary>
/// Sistema modular de control de habilidades del jugador.
/// Ejecuta la habilidad seleccionada cuando el jugador está en modo habilidad.
/// No depende de GridManager ni de referencias externas.
/// </summary>
[RequireComponent(typeof(Jugador))]
public class PlayerAbilityController : MonoBehaviour
{
    private Jugador jugador;
    private AbilityData habilidadActual;

    void Awake()
    {
        jugador = GetComponent<Jugador>();
    }

    void Update()
    {
        // Solo activo si el jugador está en modo habilidad
        if (!Jugador.ModoHabilidadActivo) return;

        habilidadActual = AbilitySelector.Instance?.GetHabilidadActual();
        if (habilidadActual == null) return;

        // Click izquierdo → acción principal
        if (Input.GetMouseButtonDown(0))
            UsarHabilidadPrincipal();

        // Click derecho → acción secundaria
        if (Input.GetMouseButtonDown(1))
            UsarHabilidadSecundaria();
    }

    // === Lógica principal ===
    private void UsarHabilidadPrincipal()
    {
        switch (habilidadActual.tipo)
        {
            case AbilityType.ShadowBlocks:
                ShadowBlockAbility.ColocarBloque(false, jugador);
                break;

            case AbilityType.ReflectiveBlocks:
                ShadowBlockAbility.ColocarBloque(true, jugador);
                break;

            case AbilityType.AbyssFlame:
                AbyssFlameAbility.Lanzar(jugador);
                break;

            case AbilityType.ShadowTp:
                ShadowTpAbility.Teletransportar(jugador);
                break;

        }
    }

    private void UsarHabilidadSecundaria()
    {
        switch (habilidadActual.tipo)
        {
            case AbilityType.ReflectiveBlocks:
                MirrorBlockAbility.RotarReflectivo(jugador);
                break;
        }
    }
}
