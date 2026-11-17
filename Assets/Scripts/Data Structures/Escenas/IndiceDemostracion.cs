using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>Escena indice: un boton por demo, con los TP que muestra.</summary>
public class IndiceDemostracion : MonoBehaviour
{
    void Start()
    {
        Font fuente = UIRapida.Fuente(20);
        RectTransform raiz = UIRapida.CrearCanvas(transform, 10, true);
        RectTransform fondo = UIRapida.Rect("Fondo", raiz, new Color(0.04f, 0.05f, 0.08f, 1f));
        UIRapida.Rellenar(fondo, 0, 0, 1, 1, 0);

        Text titulo = UIRapida.Etiqueta(fondo, fuente, "ESTRUCTURAS DE DATOS - NIVELES DE DEMOSTRACION", 26);
        UIRapida.Rellenar(titulo.rectTransform, 0.05f, 0.88f, 0.95f, 0.98f, 0);

        float alto = 0.17f, y = 0.84f;
        foreach (Demo demo in Demostracion.Todas)
        {
            string escena = demo.Escena;
            UIRapida.Boton(fondo, fuente, $"{demo.Titulo}\n{demo.Descripcion}",
                new Color(0.12f, 0.16f, 0.24f, 1f), 20, () => SceneManager.LoadScene(escena), out Text etiqueta);
            UIRapida.Rellenar((RectTransform)etiqueta.transform.parent, 0.05f, y - alto, 0.95f, y, 0);
            y -= alto + 0.015f;
        }

        Text pie = UIRapida.Etiqueta(fondo, fuente, "En cualquier nivel: F1 muestra u oculta las estructuras en vivo - F2 vuelve a este indice", 16);
        UIRapida.Rellenar(pie.rectTransform, 0.05f, 0.01f, 0.95f, 0.06f, 0);

        ranking = UIRapida.Etiqueta(fondo, fuente, "", 18);
        UIRapida.Rellenar(ranking.rectTransform, 0.05f, 0.08f, 0.95f, 0.27f, 0);
    }

    private Text ranking;

    void Update()
    {
        if (ranking == null) return;

        // TP07: las marcas de los niveles terminados, ordenadas por CollectionSort
        string detalle = RankingDeTiempos.Detalle();
        ranking.text = "RANKING DE TIEMPOS (ordenado con CollectionSort)\n" + (detalle.Length == 0 ? "(todavia no terminaste ningun nivel)" : detalle);
    }
}
