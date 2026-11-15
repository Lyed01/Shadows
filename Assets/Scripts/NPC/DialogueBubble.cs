using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// Globo de dialogo. Se dibuja en un Canvas de pantalla con la escala de la UI
/// y sigue al NPC: en el mundo la camara tiene otro zoom que la UI, y la fuente
/// pixel quedaba enorme o borrosa entre pixeles.
/// </summary>
public class DialogueBubble : MonoBehaviour
{
    [Header("Referencias")]
    [Tooltip("Punto de la punta de la cola; se mueve a la pantalla sobre el NPC.")]
    public RectTransform ancla;
    [Tooltip("Caja 9-slice; el texto va adentro, estirado con sus margenes.")]
    public Image fondo;
    public RectTransform cola;
    public TextMeshProUGUI texto;

    [Header("Tamaño (px de la UI)")]
    public float anchoMaximo = 160f;
    [Tooltip("Para que la cola entre debajo de la caja.")]
    public float anchoMinimo = 41f;
    [Tooltip("Distancia minima de la caja al borde de la pantalla.")]
    public float margenPantalla = 2f;

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
    private Canvas canvas;
    private Vector2 posCaja;

    public bool TerminoDeEscribir { get; private set; } = false;

    void Awake()
    {
        canvas = ancla.GetComponentInParent<Canvas>();
        posCaja = fondo.rectTransform.anchoredPosition;
    }

    public void Configurar(Transform seguirA, Vector3 desplazamiento, string contenido, AudioClip sonido)
    {
        objetivo = seguirA;
        offset = desplazamiento;
        sonidoDialogo = sonido;
        TerminoDeEscribir = false;
        MostrarTexto(contenido);
    }

    // LateUpdate: la camara ya siguio al jugador en este frame.
    void LateUpdate()
    {
        Camera cam = Camera.main;
        if (objetivo == null || cam == null) return;

        Vector3 flotacion = Vector3.up * Mathf.Sin(Time.time * velocidadFlotacion) * alturaFlotante;
        Vector2 pantalla = cam.WorldToScreenPoint(objetivo.position + offset + flotacion);
        float k = canvas.scaleFactor;
        // Pixel entero de la UI: entre pixeles la fuente pixel se ve borrosa.
        float x = Mathf.Round(pantalla.x / k);
        ancla.anchoredPosition = new Vector2(x, Mathf.Round(pantalla.y / k));

        MantenerEnPantalla(x, ((RectTransform)canvas.transform).rect.width);
    }

    // Corre la caja para que no se salga por los costados; la cola sigue sobre el NPC.
    private void MantenerEnPantalla(float x, float anchoPantalla)
    {
        float ancho = fondo.rectTransform.sizeDelta.x;
        float derecha = x + posCaja.x;
        float corrimiento = 0f;
        if (derecha - ancho < margenPantalla) corrimiento = margenPantalla - (derecha - ancho);
        else if (derecha > anchoPantalla - margenPantalla) corrimiento = anchoPantalla - margenPantalla - derecha;

        // Sin destapar la cola: la caja la cubre siempre con 4 px de esquina a cada lado.
        Rect c = cola.rect;
        float colaIzq = cola.anchoredPosition.x + c.xMin, colaDer = cola.anchoredPosition.x + c.xMax;
        corrimiento = Mathf.Clamp(corrimiento, colaDer + 4f - posCaja.x, colaIzq - 4f - posCaja.x + ancho);

        fondo.rectTransform.anchoredPosition = posCaja + new Vector2(corrimiento, 0f);
    }

    public void MostrarTexto(string contenido)
    {
        // Los textos de los assets terminan en "\r\n", que sumaba un renglon vacio a la caja.
        contenido = DialogueHighlighter.Procesar(contenido.TrimEnd()); // aplicar resaltado
        AjustarTamano(contenido);

        if (escribirCoroutine != null)
            StopCoroutine(escribirCoroutine);

        escribirCoroutine = StartCoroutine(EscribirTexto(contenido));
    }


    // La caja se mide con el texto completo antes de escribirlo, asi no crece mientras aparece.
    private void AjustarTamano(string contenido)
    {
        if (fondo == null) return;

        // El alto se mide de nuevo con el ancho final: con menos ancho los renglones cortan distinto.
        float ancho = Mathf.Min(Mathf.Ceil(texto.GetPreferredValues(contenido, anchoMaximo, 0f).x) + 1f, anchoMaximo);
        Vector2 tamTexto = new Vector2(ancho, Mathf.Ceil(texto.GetPreferredValues(contenido, ancho, 0f).y));
        // El texto esta estirado dentro de la caja: su sizeDelta (negativo) son los margenes.
        Vector2 caja = tamTexto - texto.rectTransform.sizeDelta;
        caja.x = Mathf.Max(caja.x, anchoMinimo);
        fondo.rectTransform.sizeDelta = caja;
    }

    // Se escribe el texto entero y se van mostrando letras: las palabras ya
    // estan en su renglon y no saltan al siguiente a mitad de escribirse.
    private IEnumerator EscribirTexto(string contenido)
    {
        TerminoDeEscribir = false;
        texto.text = contenido;
        texto.maxVisibleCharacters = 0;
        texto.ForceMeshUpdate();
        int total = texto.textInfo.characterCount;

        for (int contador = 1; contador <= total; contador++)
        {
            texto.maxVisibleCharacters = contador;

            // Sonido
            if (sonidoDialogo && contador % Mathf.RoundToInt(1f / frecuenciaSonido) == 0)
            {
                if (audioSource == null)
                    audioSource = gameObject.AddComponent<AudioSource>();

                // Play y no PlayOneShot: el clip dura mas que el intervalo entre
                // blips, y apilados se escuchaban tres a la vez. Asi cada uno corta al anterior.
                audioSource.pitch = Random.Range(0.95f, 1.05f);
                audioSource.clip = sonidoDialogo;
                audioSource.volume = 0.4f;
                audioSource.Play();
            }

            yield return new WaitForSeconds(velocidadEscritura);
        }

        TerminoDeEscribir = true;
    }

}
