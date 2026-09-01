using UnityEngine;
using System.Collections;

public class DialogueSystemWorld : PersistentSingleton<DialogueSystemWorld>
{
    [Header("Configuración")]
    public GameObject prefabBurbuja;
    public Vector3 offset = new Vector3(0, 1.5f, 0);

    private DialogueData dialogoActual;
    private int indiceLinea = 0;
    private Transform npcTransform;
    private NPCDemostrador npcDemostrador;
    private GameObject burbujaActual;
    private bool activo = false;

    public bool EstaActivo => activo;
    public static event System.Action OnDialogueEnd;
    public Transform ultimoNPCQueHablo { get; private set; }



    public void IniciarDialogo(DialogueData data, Transform npc)
    {
        if (activo || data == null || prefabBurbuja == null) return;

        dialogoActual = data;
        npcTransform = npc;

        ultimoNPCQueHablo = npc;   // <---- ESTA LINEA ES LA CLAVE

        npcDemostrador = npc.GetComponent<NPCDemostrador>() ??
                         npc.GetComponentInChildren<NPCDemostrador>() ??
                         npc.GetComponentInParent<NPCDemostrador>();

        indiceLinea = 0;
        activo = true;

        StartCoroutine(MostrarLineas());
    }

    private IEnumerator MostrarLineas()
    {
        while (indiceLinea < dialogoActual.lineas.Length)
        {
            var linea = dialogoActual.lineas[indiceLinea];
            MostrarBurbuja(linea.texto);

            // Esperar a que la burbuja termine de escribirse
            yield return new WaitUntil(() =>
            {
                var b = burbujaActual?.GetComponent<DialogueBubble>();
                return b != null && b.TerminoDeEscribir;
            });

            // Esperar auto avance o permitir tecla manual
            float autoAvanceDelay = 3f;
            float timer = 0f;

            while (timer < autoAvanceDelay)
            {
                if (Input.GetKeyDown(KeyCode.E))
                    break;

                timer += Time.deltaTime;
                yield return null;
            }


            if (linea.accionPorLinea != null)
                EjecutarAccion(linea.accionPorLinea);

            if (burbujaActual) Destroy(burbujaActual);
            indiceLinea++;
        }

        TerminarDialogo();
    }

    private void MostrarBurbuja(string texto)
    {
        if (!npcTransform) return;

        burbujaActual = Instantiate(prefabBurbuja, npcTransform.position + offset, Quaternion.identity);
        DialogueBubble bubble = burbujaActual.GetComponent<DialogueBubble>();

        var npc = npcTransform.GetComponent<NPCInteractivo>();
        AudioClip sonidoNPC = npc ? npc.sonidoDialogo : null;

        bubble?.Configurar(npcTransform, offset, texto, sonidoNPC);
    }

    private void TerminarDialogo()
    {
        activo = false;

        if (burbujaActual) Destroy(burbujaActual);

        if (dialogoActual?.accionFinal != null)
            EjecutarAccion(dialogoActual.accionFinal);

        if (npcDemostrador)
        {
            npcDemostrador.IniciarDemostracion();
            npcDemostrador = null;
        }

        // AQUÍ TODAVÍA necesitamos saber quién habló
        Transform emisorBackup = ultimoNPCQueHablo;

        // 🔥 Disparamos el evento mientras ultimoNPCQueHablo tiene valor
        OnDialogueEnd?.Invoke();

        // recién ahora lo limpiamos
        ultimoNPCQueHablo = null;
        npcTransform = null;
        dialogoActual = null;
        indiceLinea = 0;
    }



    private void EjecutarAccion(DialogueAction accion)
    {
        switch (accion.tipo)
        {
            case DialogueActionType.DesbloquearHabilidad:
                AbilityManager.Instance?.Unlock(accion.habilidad);
                break;
            case DialogueActionType.ActivarObjeto:
                if (accion.objetoActivar)
                    accion.objetoActivar.SetActive(accion.activar);
                break;
            case DialogueActionType.ReproducirSonido:
                if (accion.sonido)
                    AudioManager.Instance?.ReproducirFX(accion.sonido);
                break;
            case DialogueActionType.AbrirPuerta:
                var puerta = GameObject.Find(accion.idPuerta)?.GetComponent<Door>();
                puerta?.Open();
                break;
        }
    }
    public void ForzarCerrarDialogo()
    {
        StopAllCoroutines();
        activo = false;

        if (burbujaActual != null)
            Destroy(burbujaActual);

        Transform emisorBackup = ultimoNPCQueHablo;

        // 🔥 Disparamos el evento ANTES de limpiar
        OnDialogueEnd?.Invoke();

        ultimoNPCQueHablo = null;
        npcTransform = null;
        npcDemostrador = null;
        dialogoActual = null;
        indiceLinea = 0;

        Debug.Log("🛑 Diálogo forzado a cerrar.");
    }


}
