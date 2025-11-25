using System.Collections.Generic;
using System.Text.RegularExpressions;

public static class DialogueHighlighter
{
    // ==========================
    // 🎨 DICCIONARIO DE COLORES
    // ==========================
    public static Dictionary<string, string> coloresPorPalabra =
        new Dictionary<string, string>()
    {
        { "luz",        "#FFE066" }, // amarillo cálido
        { "shadowblock","#6A4C93" }, // violeta profundo
        { "mirrorblock","#46C7C7" }, // cian
        { "DarkTp",     "#A02CF0" }, // violeta mágico
        { "AbyssFlame", "#C500FF" }, // fuego abismal
        { "Sombra",     "#4444FF" }, // azul sombra
        { "Abismo",     "#8F00FF" }, // púrpura profundo
        { "roja",       "#FF4A4A" }, // rojo alerta
        { "amarilla",   "#FFE066" },  // amarillo
        { "WASD",        "#FF4A4A" },
        { "Umbra",      "#2E2B5F" }, // azul oscuro abismal
        { "Silhuette",  "#D65CE5" }, // rosa-violeta suave y misterioso
        { "Ignos",      "#FF7A00" }, // naranja energía/llama
        { "Noxel",      "#00D6A1" }, // verde-menta tecnológico
        { "ShadowShards",      "#A02CF0" },
    };

    // =============================================================
    // 🔥 FUNCIÓN PRINCIPAL
    // Procesa un string y envuelve cada palabra en <color=…><b>…</b></color>
    // =============================================================
    public static string Procesar(string textoOriginal)
    {
        string resultado = textoOriginal;

        foreach (var kv in coloresPorPalabra)
        {
            string palabra = kv.Key;
            string colorHex = kv.Value;

            // Regex:
            // \\b → límite de palabra (evita reemplazar dentro de otras palabras)
            // (?i) → case-insensitive
            string pattern = $@"(?i)\b{Regex.Escape(palabra)}\b";

            string reemplazo = $"<b><color={colorHex}>{palabra}</color></b>";

            resultado = Regex.Replace(resultado, pattern, reemplazo);
        }

        return resultado;
    }
}
