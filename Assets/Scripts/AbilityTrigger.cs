using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class AbilityTrigger : MonoBehaviour
{
    [Header("Habilidad a desbloquear")]
    public AbilityType habilidad = AbilityType.ShadowBlocks;

    [Header("Opciones")]
    public bool destruirTriggerAlActivar = true; // destruye el trigger después de usarlo

    private void Awake()
    {
        // Asegurarse de que el collider esté en modo trigger
        Collider2D col = GetComponent<Collider2D>();
        if (!col.isTrigger)
            col.isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Solo reacciona al jugador
        Jugador jugador = other.GetComponent<Jugador>();
        if (jugador == null) return;

        // 1️⃣ Desbloquea la habilidad globalmente (AbilityManager)
        if (AbilityManager.Instance != null)
        {
            AbilityManager.Instance.Unlock(habilidad);
            Debug.Log($"🔮 Habilidad desbloqueada: {habilidad}");
        }

        // 2️⃣ Activa la UI o feedback local del jugador (solo si ShadowBlocks)
        if (habilidad == AbilityType.ShadowBlocks)
            jugador.RecibirHabilidad();

        // 3️⃣ Feedback opcional (efecto, sonido, animación)
        // TODO: Instanciar un efecto visual aquí si querés (partículas o sonido)

        // 4️⃣ Elimina el trigger después de activarse
        if (destruirTriggerAlActivar)
            Destroy(gameObject);
    }
}
