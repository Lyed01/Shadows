/// <summary>
/// Lo que sabe hacer una habilidad del jugador. Cada una es una clase aparte
/// que cumple este contrato, y el controlador las usa sin saber cual tiene
/// entre manos.
///
/// Antes el controlador decidia con un switch sobre AbilityType: una rama por
/// habilidad, repetida para el click izquierdo y el derecho. Agregar una
/// habilidad obligaba a encontrar esos switch y sumarles un case.
///
/// Es Strategy y no State porque el cambio lo decide el jugador desde afuera,
/// con el selector: la habilidad no se da cuenta sola de que tiene que
/// convertirse en otra.
/// </summary>
public interface IHabilidad
{
    /// <summary>Con que entrada del selector se corresponde.</summary>
    AbilityType Tipo { get; }

    /// <summary>Click izquierdo.</summary>
    void UsarPrincipal(Jugador jugador);

    /// <summary>Click derecho. La mayoria de las habilidades no hace nada.</summary>
    void UsarSecundaria(Jugador jugador);
}
