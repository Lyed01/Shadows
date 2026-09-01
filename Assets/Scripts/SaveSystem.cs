using UnityEngine;

/// <summary>
/// Unico punto de acceso al progreso guardado del jugador.
///
/// Antes cada sistema armaba sus claves de PlayerPrefs a mano, repartidas en
/// ocho archivos: un error de tipeo en una de ellas guardaba en un lugar y leia
/// de otro sin dar ningun error. Aca los nombres estan una sola vez.
///
/// Los formatos de clave son los que ya usaba el juego, asi que las partidas
/// guardadas siguen siendo validas.
/// </summary>
public static class SaveSystem
{
    public const string ClaveFragmentos = "FragmentosTotales";

    private const string PrefijoNivel = "Nivel_";
    private const string PrefijoHabilidad = "Habilidad_";

    private const string ClaveVolumenMusica = "VolMusica";
    private const string ClaveVolumenEfectos = "VolEfectos";
    private const string ClaveVolumenInterfaz = "VolGeneral";
    private const string ClaveVolumenMaestro = "VolMaster";
    private const string ClaveSilencio = "Mute";

    // ============================================================
    // PROGRESO POR NIVEL
    // ============================================================

    private static string ClaveNivel(string nivelId, string dato) =>
        $"{PrefijoNivel}{nivelId}_{dato}";

    public static int GetEstrellas(string nivelId) =>
        PlayerPrefs.GetInt(ClaveNivel(nivelId, "Estrellas"), 0);

    public static void SetEstrellas(string nivelId, int estrellas)
    {
        PlayerPrefs.SetInt(ClaveNivel(nivelId, "Estrellas"), estrellas);
        PlayerPrefs.Save();
    }

    public static void GuardarResultado(string nivelId, int estrellas, float tiempo,
                                        int muertes, int habilidades)
    {
        PlayerPrefs.SetInt(ClaveNivel(nivelId, "Estrellas"), estrellas);
        PlayerPrefs.SetFloat(ClaveNivel(nivelId, "Tiempo"), tiempo);
        PlayerPrefs.SetInt(ClaveNivel(nivelId, "Muertes"), muertes);
        PlayerPrefs.SetInt(ClaveNivel(nivelId, "Habilidades"), habilidades);
        PlayerPrefs.Save();
    }

    /// <summary>Borra el progreso de un nivel. Devuelve cuantas claves elimino.</summary>
    public static int BorrarProgresoNivel(string nivelId)
    {
        int borradas = 0;

        foreach (string dato in new[] { "Estrellas", "Tiempo", "Muertes", "Habilidades" })
        {
            string clave = ClaveNivel(nivelId, dato);
            if (PlayerPrefs.HasKey(clave))
            {
                PlayerPrefs.DeleteKey(clave);
                borradas++;
            }
        }

        return borradas;
    }

    // ============================================================
    // FRAGMENTOS
    // ============================================================

    /// <summary>
    /// Total de estrellas acumuladas. Lo consultan las puertas que exigen un
    /// minimo para abrirse.
    /// </summary>
    public static int Fragmentos
    {
        get => PlayerPrefs.GetInt(ClaveFragmentos, 0);
        set
        {
            PlayerPrefs.SetInt(ClaveFragmentos, value);
            PlayerPrefs.Save();
        }
    }

    public static int GetFragmentos(string clave) => PlayerPrefs.GetInt(clave, 0);

    // ============================================================
    // HABILIDADES
    // ============================================================

    public static bool GetHabilidad(AbilityType tipo) =>
        PlayerPrefs.GetInt(PrefijoHabilidad + tipo, 0) == 1;

    public static void SetHabilidad(AbilityType tipo, bool desbloqueada)
    {
        PlayerPrefs.SetInt(PrefijoHabilidad + tipo, desbloqueada ? 1 : 0);
        PlayerPrefs.Save();
    }

    public static void BorrarHabilidad(AbilityType tipo) =>
        PlayerPrefs.DeleteKey(PrefijoHabilidad + tipo);

    // ============================================================
    // AUDIO
    // ============================================================

    public static float VolumenMusica
    {
        get => PlayerPrefs.GetFloat(ClaveVolumenMusica, 0.7f);
        set => PlayerPrefs.SetFloat(ClaveVolumenMusica, value);
    }

    public static float VolumenEfectos
    {
        get => PlayerPrefs.GetFloat(ClaveVolumenEfectos, 1f);
        set => PlayerPrefs.SetFloat(ClaveVolumenEfectos, value);
    }

    public static float VolumenInterfaz
    {
        get => PlayerPrefs.GetFloat(ClaveVolumenInterfaz, 0.8f);
        set => PlayerPrefs.SetFloat(ClaveVolumenInterfaz, value);
    }

    public static float VolumenMaestro
    {
        get => PlayerPrefs.GetFloat(ClaveVolumenMaestro, 1f);
        set => PlayerPrefs.SetFloat(ClaveVolumenMaestro, value);
    }

    public static bool Silenciado
    {
        get => PlayerPrefs.GetInt(ClaveSilencio, 0) == 1;
        set => PlayerPrefs.SetInt(ClaveSilencio, value ? 1 : 0);
    }

    // ============================================================
    // GENERAL
    // ============================================================

    /// <summary>Vuelca a disco lo escrito por las propiedades de audio.</summary>
    public static void Guardar() => PlayerPrefs.Save();

    /// <summary>Borra la partida completa: progreso, habilidades y opciones.</summary>
    public static void BorrarTodo()
    {
        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();
    }
}
