/// <summary>
/// Lo implementan los popups que compiten por la atencion de la pantalla.
///
/// Cada popup decide en su Update si tiene algo para mostrar y, si lo tiene, se
/// postula ante el PopupManager con su prioridad. El manager le contesta
/// llamando a uno de estos dos metodos.
/// </summary>
public interface IPopupArbitrado
{
    /// <summary>Gano el turno: le toca ocupar la pantalla en este frame.</summary>
    void MostrarPopup();

    /// <summary>Lo gano otro de mayor prioridad y tiene que dejarle el lugar.</summary>
    void OcultarPopup();
}
