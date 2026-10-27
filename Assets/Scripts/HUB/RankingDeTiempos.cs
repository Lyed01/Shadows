/// <summary>
/// Ordena los mejores tiempos del jugador, uno por nivel, del mas rapido al mas
/// lento. Es lo que le dice al popup de una puerta del Hub "tu marca en este
/// nivel es la 2 de 7".
///
/// Arma una lista propia con los niveles ya terminados y la ordena con
/// CollectionSort (Quick, ascendente): son pocos datos (una puerta por nivel) y
/// se piden solo al mostrar un popup, asi que no hace falta mantenerla ordenada.
/// </summary>
public static class RankingDeTiempos
{
    private struct Marca
    {
        public string NivelId;
        public float Tiempo;
    }

    /// <summary>
    /// Texto con el mejor tiempo del nivel y su puesto entre todas las marcas, o
    /// vacio si todavia no lo termino.
    /// </summary>
    public static string Resumen(string nivelId)
    {
        float tiempo = SaveSystem.GetMejorTiempo(nivelId);
        if (tiempo <= 0f) return string.Empty;

        ISimpleList<Marca> marcas = Ordenadas();

        int puesto = 1;
        for (int i = 0; i < marcas.Count; i++)
        {
            if (marcas[i].NivelId == nivelId)
            {
                puesto = i + 1;
                break;
            }
        }

        return $"Mejor tiempo: {Formato(tiempo)}  (marca {puesto} de {marcas.Count})";
    }

    /// <summary>Todas las marcas, de la mas rapida a la mas lenta, una por linea. Solo para el panel de depuracion.</summary>
    public static string Detalle()
    {
        ISimpleList<Marca> marcas = Ordenadas();
        var sb = new System.Text.StringBuilder();

        for (int i = 0; i < marcas.Count; i++)
            sb.AppendLine($"{i + 1}. {marcas[i].NivelId}  {Formato(marcas[i].Tiempo)}");

        return sb.ToString();
    }

    private static ISimpleList<Marca> Ordenadas()
    {
        ISimpleList<Marca> marcas = new SimpleArrayList<Marca>();
        SimpleArrayList<DoorHub> puertas = DoorHub.Registradas;

        for (int i = 0; i < puertas.Count; i++)
        {
            if (puertas[i] == null) continue;

            float tiempo = SaveSystem.GetMejorTiempo(puertas[i].idNivel);
            if (tiempo > 0f)
                marcas.Add(new Marca { NivelId = puertas[i].idNivel, Tiempo = tiempo });
        }

        CollectionSort.SortList(marcas, (a, b) => a.Tiempo.CompareTo(b.Tiempo));
        return marcas;
    }

    private static string Formato(float segundos) =>
        $"{(int)(segundos / 60f):00}:{(int)(segundos % 60f):00}";
}
