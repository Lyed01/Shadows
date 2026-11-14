using UnityEditor;
using UnityEngine;

/// <summary>
/// Comprueba SpotLightDetector.RecortarFranjas. Es un menu y no un test de NUnit porque
/// los scripts del juego viven en Assembly-CSharp, que un assembly de test no puede referenciar.
/// </summary>
public static class PruebaRecorteFranjas
{
    [MenuItem("Shadows/Pruebas/Recorte de franjas")]
    public static void Ejecutar()
    {
        int fallos = 0;

        fallos += Caso("Pared plana: no recorta",
            new[] { 5f, 5f, 5f, 5f, 5f }, 1f, 1f, new[] { 5f, 5f, 5f, 5f, 5f });

        fallos += Caso("Pared plana con rayos de largo distinto: conserva cada largo",
            new[] { 5.2f, 5.1f, 5f, 5.1f, 5.2f }, 1f, 1f, new[] { 5.2f, 5.1f, 5f, 5.1f, 5.2f });

        fallos += Caso("Franja fina en el centro: se corta al obstaculo",
            new[] { 2f, 2f, 8f, 2f, 2f }, 0.1f, 1f, new[] { 2f, 2f, 2f, 2f, 2f });

        fallos += Caso("Franja fina en el borde: se corta al obstaculo",
            new[] { 8f, 2f, 2f, 2f, 2f }, 0.1f, 1f, new[] { 2f, 2f, 2f, 2f, 2f });

        // El caso real de Nivel 1: el piso es plano y los rayos de la franja no miden lo mismo.
        fallos += Caso("Franja fina con largos distintos: se corta",
            new[] { 1.5f, 1.5f, 4.62f, 4.63f, 4.64f, 0.6f, 0.4f }, 0.05f, 1f,
            new[] { 1.5f, 1.5f, 1.5f, 1.5f, 1.5f, 0.6f, 0.4f });

        fallos += Caso("Un rayo rozando una esquina: el haz angosto no colapsa",
            new[] { 5f, 5f, 5f, 5f, 0.4f }, 0.1f, 1f, new[] { 5f, 5f, 5f, 5f, 0.4f });

        fallos += Caso("Franja ancha: sigue",
            new[] { 2f, 8f, 8f, 8f, 2f }, 1f, 1f, new[] { 2f, 8f, 8f, 8f, 2f });

        fallos += Caso("Minimo 0: no recorta",
            new[] { 2f, 2f, 8f, 2f, 2f }, 0.1f, 0f, new[] { 2f, 2f, 8f, 2f, 2f });

        if (fallos == 0)
            Debug.Log("Recorte de franjas: todas las comprobaciones en verde");
        else
            Debug.LogError($"Recorte de franjas: {fallos} comprobaciones fallaron");
    }

    private static int Caso(string nombre, float[] largos, float separacion, float minimo, float[] esperado)
    {
        int total = largos.Length;
        Vector2[] salidas = new Vector2[total];
        Vector2[] direcciones = new Vector2[total];

        for (int i = 0; i < total; i++)
        {
            salidas[i] = new Vector2(i * separacion, 0f);
            direcciones[i] = Vector2.up;
        }

        float[] obtenido = new float[total];
        SpotLightDetector.RecortarFranjas(salidas, direcciones, largos, minimo, obtenido);

        for (int i = 0; i < total; i++)
        {
            if (Mathf.Abs(obtenido[i] - esperado[i]) > 0.001f)
            {
                Debug.LogError($"{nombre}: esperaba [{string.Join(", ", esperado)}] " +
                               $"y dio [{string.Join(", ", obtenido)}]");
                return 1;
            }
        }

        return 0;
    }
}
