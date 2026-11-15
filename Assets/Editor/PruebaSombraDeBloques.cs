using UnityEditor;
using UnityEngine;

/// <summary>
/// Comprueba SombraDeBloques.CruceConRectangulo, el calculo de donde la sombra de
/// un bloque corta un rayo. Menu y no NUnit por lo mismo que PruebaRecorteFranjas.
/// </summary>
public static class PruebaSombraDeBloques
{
    [MenuItem("Shadows/Pruebas/Sombra de bloques")]
    public static void Ejecutar()
    {
        int fallos = 0;
        Vector2 abajo = Vector2.down;
        Vector2 centro = new Vector2(0f, -3f); // bloque 3 celdas debajo de la luz

        fallos += Caso("De frente: corta en el borde de arriba", Vector2.zero, abajo, centro, 1f, 10f, true, 2.5f);
        fallos += Caso("Ancho 2 de frente: igual corta en la cara del bloque", Vector2.zero, abajo, centro, 2f, 10f, true, 2.5f);
        fallos += Caso("Rayo al costado, ancho 1: pasa", new Vector2(0.8f, 0f), abajo, centro, 1f, 10f, false, 0f);
        fallos += Caso("Rayo al costado, ancho 2: lo tapa en la cara", new Vector2(0.8f, 0f), abajo, centro, 2f, 10f, true, 2.5f);
        fallos += Caso("Ancho 0,5: el borde del bloque deja pasar", new Vector2(0.4f, 0f), abajo, centro, 0.5f, 10f, false, 0f);
        fallos += Caso("Bloque mas lejos que el largo: no corta", Vector2.zero, abajo, centro, 1f, 2f, false, 0f);
        fallos += Caso("Origen dentro del bloque: corta en 0", centro, abajo, centro, 1f, 10f, true, 0f);
        fallos += Caso("Ancho 0: no tapa", Vector2.zero, abajo, centro, 0f, 10f, false, 0f);
        fallos += Caso("En diagonal: entra por la esquina", new Vector2(-3f, 0f), new Vector2(1f, -1f).normalized,
            centro, 1f, 10f, true, Mathf.Sqrt(2f) * 2.5f);

        if (fallos == 0)
            Debug.Log("Sombra de bloques: todas las comprobaciones en verde");
        else
            Debug.LogError($"Sombra de bloques: {fallos} comprobaciones fallaron");
    }

    private static int Caso(string nombre, Vector2 origen, Vector2 dir, Vector2 centro, float lado, float largo,
        bool esperaCorte, float esperaDistancia)
    {
        Vector2 mitad = SombraDeBloques.MitadDeSombra(dir, new Vector2(0.5f, 0.5f), lado);
        bool corta = SombraDeBloques.CruceConRectangulo(origen, dir, centro, mitad, largo, out float distancia);
        if (corta == esperaCorte && (!corta || Mathf.Abs(distancia - esperaDistancia) < 1e-4f)) return 0;

        Debug.LogError($"{nombre}: esperaba corte={esperaCorte} a {esperaDistancia} y dio corte={corta} a {distancia}");
        return 1;
    }
}
