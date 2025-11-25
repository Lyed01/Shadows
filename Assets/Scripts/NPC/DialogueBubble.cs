using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;

public class DialogueBubble : MonoBehaviour
{
    [Header("Referencias")]
    public Image fondo;
    public TextMeshProUGUI texto;

    [Header("Animación")]
    public float velocidadEscritura = 0.03f;
    public float alturaFlotante = 0.15f;
    public float velocidadFlotacion = 2f;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip sonidoDialogo;
    [Range(0.05f, 1f)] public float frecuenciaSonido = 0.25f;

    private Transform objetivo;
    private Vector3 offset;
    private Coroutine escribirCoroutine;

    public bool TerminoDeEscribir { get; private set; } = false;

    public void Configurar(Transform seguirA, Vector3 desplazamiento, string contenido, AudioClip sonido)
    {
        objetivo = seguirA;
        offset = desplazamiento;
        sonidoDialogo = sonido;
        TerminoDeEscribir = false;
        MostrarTexto(contenido);
    }

    void Update()
    {
        if (objetivo == null) return;
        Vector3 flotacion = Vector3.up * Mathf.Sin(Time.time * velocidadFlotacion) * alturaFlotante;
        transform.position = objetivo.position + offset + flotacion;
    }

    public void MostrarTexto(string contenido)
    {
        contenido = DialogueHighlighter.Procesar(contenido); // 🟣 aplicar resaltado

        if (escribirCoroutine != null)
            StopCoroutine(escribirCoroutine);

        escribirCoroutine = StartCoroutine(EscribirTexto(contenido));
    }


    private IEnumerator EscribirTexto(string contenido)
    {
        texto.text = "";
        TerminoDeEscribir = false;

        int contador = 0;
        int i = 0;

        while (i < contenido.Length)
        {
            // Detectar etiqueta de RichText
            if (contenido[i] == '<')
            {
                int cierre = contenido.IndexOf('>', i);
                if (cierre != -1)
                {
                    // Copiar etiqueta completa instantáneamente
                    string etiqueta = contenido.Substring(i, cierre - i + 1);
                    texto.text += etiqueta;
                    i = cierre + 1;
                    continue;
                }
            }

            // Escribir letra normal
            texto.text += contenido[i];
            contador++;

            // Sonido
            if (sonidoDialogo && contador % Mathf.RoundToInt(1f / frecuenciaSonido) == 0)
            {
                if (audioSource == null)
                    audioSource = gameObject.AddComponent<AudioSource>();

                audioSource.pitch = Random.Range(0.95f, 1.05f);
                audioSource.PlayOneShot(sonidoDialogo, 0.4f);
            }

            i++;
            yield return new WaitForSeconds(velocidadEscritura);
        }

        TerminoDeEscribir = true;
    }

}
