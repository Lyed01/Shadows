using UnityEngine;

public class NPCMuerePorLuz_Advanced : MonoBehaviour
{
    [Header("NPC que ejecutará su demostración al morir")]
    public NPCDemostrador npcDemostrador;

    [Header("Flag requerida para permitir que muera (opcional)")]
    public string flagRequerida;

    [Header("Flag que se activa al morir (opcional)")]
    public string flagAlMorir;

    private bool yaMurio = false;

    /// <summary>
    /// Método público llamado por Spotlight o Reflector cuando la luz toca al NPC.
    /// </summary>
    public void MorirPorLuz()
    {
        if (yaMurio) return;

        // Si requiere una flag antes de morir
        if (!string.IsNullOrEmpty(flagRequerida))
        {
            if (!SceneStateManager.Instance.HasFlag(flagRequerida))
                return; // La luz NO lo mata aún
        }

        yaMurio = true;

        Log.Info(this, "NPC murió por la luz: " + name);

        // Activar flag si corresponde
        if (!string.IsNullOrEmpty(flagAlMorir))
        {
            SceneStateManager.Instance.SetFlag(flagAlMorir);
        }

        // Si tiene demostración, iniciarla
        npcDemostrador?.IniciarDemostracion();

        // (Opcional) destruir el NPC luego de una animación
        Destroy(gameObject, 0.1f);
    }
}
