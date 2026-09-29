using UnityEngine;

/// <summary>
/// Decide cual de los popups ocupa la pantalla cuando hay mas de uno pidiendo
/// aparecer.
///
/// Antes cada popup se mostraba por su cuenta, sin saber de los demas: en el
/// Hub el de nivel y el de fragmentos podian quedar encimados, y una
/// notificacion de habilidad se dibujaba arriba de cualquiera de los dos.
///
/// Ahora cada popup se postula en su Update con una prioridad fija, y el
/// manager resuelve en LateUpdate, cuando ya se postularon todos: le da la
/// pantalla al de mayor prioridad y manda a ocultarse al resto.
///
/// Usa SimpleArrayPriorityQueue porque en cada frame entran unas pocas
/// postulaciones y se saca una sola: conviene que Enqueue sea O(1) aunque
/// Dequeue tenga que recorrer.
/// </summary>
public class PopupManager : PersistentSingleton<PopupManager>
{
    // La cola atiende primero al numero mas chico, como los puestos de una
    // carrera.
    public const int PrioridadHabilidad = 1;
    public const int PrioridadNivel = 2;
    public const int PrioridadFragmentos = 3;

    private readonly ISimplePriorityQueue<IPopupArbitrado> postulantes =
        new SimpleArrayPriorityQueue<IPopupArbitrado>();

    private readonly NotificadorDeHabilidades notificador = new();

    /// <summary>
    /// Devuelve el manager y lo crea si todavia no existe. Los popups viven en
    /// escenas distintas y ninguna los contiene a todos, asi que lo levanta el
    /// primero que lo necesita.
    /// </summary>
    public static PopupManager Obtener()
    {
        if (Instance != null) return Instance;

        return new GameObject(nameof(PopupManager)).AddComponent<PopupManager>();
    }

    /// <summary>Un popup avisa que tiene algo para mostrar en este frame.</summary>
    public void Postular(IPopupArbitrado popup, int prioridad)
    {
        if (popup == null || postulantes.Contains(popup)) return;

        postulantes.Enqueue(popup, prioridad);
    }

    /// <summary>
    /// Suma una notificacion de habilidad a la fila de espera. No se muestra al
    /// toque: espera a que termine la que este en pantalla.
    /// </summary>
    public void EncolarHabilidad(PopupHabilidadUI vista, DatosHabilidad datos, int prioridad)
    {
        notificador.Encolar(vista, datos, prioridad);
    }

    private void LateUpdate()
    {
        if (notificador.TieneAlgoQueMostrar)
            Postular(notificador, PrioridadHabilidad);

        if (postulantes.Count == 0) return;

        bool yaHayGanador = false;

        while (postulantes.Count > 0)
        {
            IPopupArbitrado popup = postulantes.Dequeue();

            // Un popup de escena puede haberse destruido entre que se postulo y
            // este momento, por ejemplo al salir del Hub.
            if (popup is MonoBehaviour vista && vista == null) continue;

            if (yaHayGanador)
            {
                popup.OcultarPopup();
            }
            else
            {
                popup.MostrarPopup();
                yaHayGanador = true;
            }
        }
    }
}

/// <summary>
/// Fila de espera de las notificaciones de habilidad.
///
/// Antes cada desbloqueo llamaba directo a PopupHabilidadUI.Mostrar, que cortaba
/// la animacion en curso: si un dialogo entregaba dos habilidades seguidas, la
/// primera se perdia sin que el jugador llegara a leerla. Ahora se encolan y
/// salen de a una.
///
/// Usa SimpleLinkedPriorityQueue porque la fila vive entre frames y se consume
/// de a un elemento por vez: conviene que Dequeue sea O(1), aunque encolar
/// tenga que recorrer para ubicar la nueva en su lugar.
///
/// No es un MonoBehaviour a proposito: el Canvas del popup se apaga solo cuando
/// termina, asi que no puede llevar la cuenta de lo que quedo pendiente.
/// </summary>
public class NotificadorDeHabilidades : IPopupArbitrado
{
    private readonly ISimplePriorityQueue<DatosHabilidad> pendientes =
        new SimpleLinkedPriorityQueue<DatosHabilidad>();

    private PopupHabilidadUI vista;

    /// <summary>Hay algo esperando turno, o algo todavia en pantalla.</summary>
    public bool TieneAlgoQueMostrar => pendientes.Count > 0 || EstaEnPantalla;

    private bool EstaEnPantalla => vista != null && vista.gameObject.activeSelf;

    public void Encolar(PopupHabilidadUI popup, DatosHabilidad datos, int prioridad)
    {
        if (popup == null || datos == null) return;

        vista = popup;
        pendientes.Enqueue(datos, prioridad);
    }

    public void MostrarPopup()
    {
        // Mientras haya una en pantalla no se saca la siguiente: el turno es de
        // la que ya esta corriendo su animacion.
        if (EstaEnPantalla || pendientes.Count == 0) return;

        DatosHabilidad datos = pendientes.Dequeue();
        vista.Mostrar(datos.icono, datos.titulo, datos.descripcion);
    }

    /// <summary>
    /// Es el mas prioritario de todos, asi que nunca pierde mientras se
    /// postula.
    /// </summary>
    public void OcultarPopup() { }
}
