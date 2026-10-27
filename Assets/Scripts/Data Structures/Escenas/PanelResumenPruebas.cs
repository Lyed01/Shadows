using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Escena PruebaTodo: corre las baterias de los nueve TP de Estructuras de Datos
/// y muestra una tabla con el resultado de cada una. Un clic en una fila
/// muestra el detalle de esa bateria.
///
/// Arma las pruebas y la UI por codigo: la escena solo necesita un GameObject
/// con este componente.
/// </summary>
public class PanelResumenPruebas : MonoBehaviour
{
    private class Fila
    {
        public string Titulo;
        public Action Correr;
        public Func<string> Salida;
        public Button Boton;
        public Text Etiqueta;
        public string Texto = "";
        public bool Ok;
        public int Pasaron;
        public int Total;
    }

    private static readonly Color Verde = new Color(0.18f, 0.5f, 0.28f);
    private static readonly Color Rojo = new Color(0.65f, 0.2f, 0.2f);
    private static readonly Color Gris = new Color(0.3f, 0.3f, 0.36f);

    private readonly List<Fila> filas = new List<Fila>();
    private Font fuente;
    private Text resumen;
    private Text detalle;

    private void Start()
    {
        fuente = UIRapida.Fuente(20);

        Registrar<PruebaListas>("TP01 · List", p => p.CorrerTodo(), p => p.UltimaSalida);
        Registrar<PruebaPilas>("TP02 · Stack", p => p.CorrerTodo(), p => p.UltimaSalida);
        Registrar<PruebaColas>("TP03 · Queue", p => p.CorrerTodo(), p => p.UltimaSalida);
        Registrar<PruebaColasDePrioridad>("TP04 · Priority Queue", p => p.CorrerTodo(), p => p.UltimaSalida);
        Registrar<PruebaConjuntos>("TP05 · Set", p => p.CorrerTodo(), p => p.UltimaSalida);
        Registrar<PruebaDiccionarios>("TP06 · Dictionary", p => p.CorrerTodo(), p => p.UltimaSalida);
        Registrar<PruebaSorting>("TP07 · Sorting", p => p.CorrerTodo(), p => p.UltimaSalida);
        Registrar<PruebaBST>("TP08 · BST", p => p.CorrerTodo(), p => p.UltimaSalida);
        Registrar<PruebaAVL>("TP09 · AVL", p => p.CorrerTodo(), p => p.UltimaSalida);

        ConstruirUI();
        CorrerTodo();
    }

    private void Registrar<T>(string titulo, Action<T> correr, Func<T, string> salida) where T : MonoBehaviour
    {
        T prueba = gameObject.AddComponent<T>();

        // Corren cuando este panel lo pide, no solas en su Start: si no, la
        // consola recibiria cada resultado dos veces.
        var campo = typeof(T).GetField("correrAlIniciar");
        if (campo != null) campo.SetValue(prueba, false);

        filas.Add(new Fila { Titulo = titulo, Correr = () => correr(prueba), Salida = () => salida(prueba) });
    }

    private void CorrerTodo()
    {
        int tpEnVerde = 0;
        int comprobaciones = 0;

        foreach (Fila fila in filas)
        {
            try
            {
                fila.Correr();
                fila.Texto = fila.Salida();

                Match m = Regex.Match(fila.Texto, @"(\d+) de (\d+) comprobaciones");
                fila.Pasaron = m.Success ? int.Parse(m.Groups[1].Value) : 0;
                fila.Total = m.Success ? int.Parse(m.Groups[2].Value) : 0;
                fila.Ok = m.Success && fila.Pasaron == fila.Total;
            }
            catch (Exception e)
            {
                fila.Texto = $"La bateria tiro una excepcion:\n{e}";
                fila.Pasaron = 0;
                fila.Total = 0;
                fila.Ok = false;
            }

            if (fila.Ok) tpEnVerde++;
            comprobaciones += fila.Pasaron;

            fila.Boton.GetComponent<Image>().color = fila.Ok ? Verde : Rojo;
            fila.Etiqueta.text = $"{(fila.Ok ? "OK   " : "FALLA")}  {fila.Titulo,-24} {fila.Pasaron}/{fila.Total}";
        }

        resumen.text = $"{tpEnVerde} de {filas.Count} TP en verde   ·   {comprobaciones} comprobaciones que pasan";
        resumen.color = tpEnVerde == filas.Count ? new Color(0.6f, 1f, 0.7f) : new Color(1f, 0.6f, 0.6f);

        Mostrar(filas.Find(f => !f.Ok) ?? filas[0]);
    }

    private void Mostrar(Fila fila) => detalle.text = fila.Titulo + "\n" + fila.Texto;

    private void ConstruirUI()
    {
        RectTransform raiz = UIRapida.CrearCanvas(transform, 0, true);
        UIRapida.Rellenar(UIRapida.Rect("Fondo", raiz, new Color(0.09f, 0.1f, 0.13f)), 0, 0, 1, 1, 0);

        RectTransform cabecera = UIRapida.Rect("Cabecera", raiz, new Color(0.14f, 0.16f, 0.2f));
        UIRapida.Rellenar(cabecera, 0.01f, 0.9f, 0.99f, 0.98f, 4);

        Text titulo = UIRapida.Etiqueta(cabecera, fuente, "Estructuras de Datos · prueba de los 9 TP", 34);
        UIRapida.Rellenar(titulo.rectTransform, 0, 0.5f, 1, 1, 10);

        resumen = UIRapida.Etiqueta(cabecera, fuente, "", 24);
        UIRapida.Rellenar(resumen.rectTransform, 0, 0, 1, 0.5f, 10);

        RectTransform tabla = UIRapida.Rect("Tabla", raiz, new Color(0.14f, 0.16f, 0.2f));
        UIRapida.Rellenar(tabla, 0.01f, 0.02f, 0.38f, 0.89f, 4);

        float alto = 1f / (filas.Count + 1);

        for (int i = 0; i < filas.Count; i++)
        {
            Fila fila = filas[i];
            Button boton = UIRapida.Boton(tabla, fuente, fila.Titulo, Gris, 20, () => Mostrar(fila), out Text etiqueta);

            var rt = (RectTransform)boton.transform;
            UIRapida.Rellenar(rt, 0.02f, 1f - alto * (i + 1), 0.98f, 1f - alto * i, 3);

            fila.Boton = boton;
            fila.Etiqueta = etiqueta;
        }

        Button correr = UIRapida.Boton(tabla, fuente, "Correr todo de nuevo", new Color(0.25f, 0.35f, 0.6f), 20, CorrerTodo, out _);
        UIRapida.Rellenar((RectTransform)correr.transform, 0.02f, 0f, 0.98f, alto, 3);

        RectTransform panelDetalle = UIRapida.Rect("Detalle", raiz, new Color(0.14f, 0.16f, 0.2f));
        UIRapida.Rellenar(panelDetalle, 0.39f, 0.02f, 0.99f, 0.89f, 4);
        detalle = UIRapida.TextoEnScroll(panelDetalle, fuente, 16);
    }
}
