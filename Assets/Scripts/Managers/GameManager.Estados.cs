using UnityEngine;

/// <summary>
/// Los estados del juego como objetos.
///
/// Antes el GameManager guardaba el estado en un enum y cada metodo preguntaba
/// en cual estaba antes de actuar: que hace Escape, que pasa con el tiempo al
/// pausar, que se avisa al salir de la pausa. Ahora cada estado se encarga de
/// lo suyo: lo que hace al entrar, al salir y ante Escape. El GameManager solo
/// sabe cual es el estado actual y como cambiar a otro.
///
/// Las clases estan anidadas en el GameManager para poder disparar sus eventos,
/// que desde afuera solo admiten suscripciones.
/// </summary>
public partial class GameManager
{
    private abstract class EstadoJuego
    {
        protected readonly GameManager juego;
        protected EstadoJuego(GameManager juego) => this.juego = juego;

        public abstract GameState Tipo { get; }
        public virtual void Entrar() { }
        public virtual void Salir() { }
        public virtual void AlPresionarEscape() { }
    }

    private class EstadoJugando : EstadoJuego
    {
        public EstadoJugando(GameManager juego) : base(juego) { }
        public override GameState Tipo => GameState.Jugando;
        public override void AlPresionarEscape() => juego.PausarJuego();
    }

    private class EstadoPausado : EstadoJuego
    {
        public EstadoPausado(GameManager juego) : base(juego) { }
        public override GameState Tipo => GameState.Pausado;

        public override void Entrar()
        {
            Time.timeScale = 0f;
            OnPause?.Invoke();
            UIManager.Instance?.MostrarPausa();
        }

        public override void Salir()
        {
            Time.timeScale = 1f;
            OnResume?.Invoke();
            UIManager.Instance?.MostrarHUD();
        }

        public override void AlPresionarEscape() => juego.ReanudarJuego();
    }

    private class EstadoMuerte : EstadoJuego
    {
        public EstadoMuerte(GameManager juego) : base(juego) { }
        public override GameState Tipo => GameState.Muerte;

        public override void Entrar()
        {
            OnPlayerDeath?.Invoke(); // LevelScoreManager escucha esto
            juego.Invoke(nameof(ReiniciarFlujoDeJuego), juego.tiempoReinicio);
        }
    }

    private class EstadoTransicion : EstadoJuego
    {
        public EstadoTransicion(GameManager juego) : base(juego) { }
        public override GameState Tipo => GameState.Transicion;

        // Una carga de escena nunca debe quedar con el tiempo congelado.
        public override void Entrar() => Time.timeScale = 1f;
    }
}
