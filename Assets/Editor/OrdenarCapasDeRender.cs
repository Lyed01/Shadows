using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering.Universal;
using UnityEngine.Tilemaps;

/// <summary>
/// Deja las capas de ordenamiento de todo el proyecto segun CapasDeRender:
/// crea las capas que faltan en el TagManager y le asigna a cada sprite o
/// malla su capa segun lo que es. Las entidades ademas reciben
/// OrdenPorAltura, con su orden relativo normalizado a 0.
///
/// Se puede correr las veces que haga falta: lo que ya esta bien no se toca,
/// asi que sirve despues de sumar un nivel o un prefab nuevo. Primero procesa
/// los prefabs y despues las escenas; en las escenas revierte los overrides de
/// capa y orden, para que cada instancia herede lo del prefab.
///
/// Tambien agrega las capas nuevas a las Light2D que iluminaban Default: cada
/// luz 2D guarda la lista de capas que ilumina, y sin esto las entidades y las
/// luces se verian sin iluminacion.
///
/// Los tilemaps no se tocan: ya estan en piso, paredes, objetos o Default, y
/// Default queda debajo de luces y entidades.
/// </summary>
public static class OrdenarCapasDeRender
{
    private enum Rol { Ninguno, Entidad, Luz, Suelo, Efecto }

    private static readonly StringBuilder informe = new();
    private static bool soloVerificar;

    [MenuItem("Shadows/Ordenar capas de render")]
    public static void Ejecutar() => Correr(false);

    /// <summary>
    /// Cuenta lo que habria que cambiar sin guardar nada. Guardar una escena
    /// desde Unity regenera las luces 2D de los focos y llena el diff de ruido.
    /// </summary>
    [MenuItem("Shadows/Verificar capas de render")]
    public static void Verificar() => Correr(true);

    private static void Correr(bool verificar)
    {
        soloVerificar = verificar;
        informe.Clear();
        if (!verificar) AsegurarCapas();

        foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets" }))
            ProcesarPrefab(AssetDatabase.GUIDToAssetPath(guid));
        AssetDatabase.SaveAssets();

        // Segunda vuelta: un prefab que contiene a otro puede haber quedado con
        // valores del anterior. Si todo esta bien, no cambia nada.
        foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets" }))
            ProcesarPrefab(AssetDatabase.GUIDToAssetPath(guid));
        AssetDatabase.SaveAssets();

        foreach (string guid in AssetDatabase.FindAssets("t:Scene", new[] { "Assets/Scenes" }))
            ProcesarEscena(AssetDatabase.GUIDToAssetPath(guid));

        string titulo = soloVerificar ? "Verificacion (no se guardo nada)" : "Listo";
        Debug.Log($"[OrdenarCapasDeRender] {titulo}: " + (informe.Length == 0 ? "nada que cambiar." : "\n" + informe));
    }

    // CAPAS

    private static void AsegurarCapas()
    {
        var tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        SerializedProperty capas = tagManager.FindProperty("m_SortingLayers");

        var ids = new Dictionary<string, long>();
        var actuales = new List<string>();
        for (int i = 0; i < capas.arraySize; i++)
        {
            SerializedProperty capa = capas.GetArrayElementAtIndex(i);
            string nombre = capa.FindPropertyRelative("name").stringValue;
            // longValue: los ids guardados como entero sin signo pasan de 2^31 y con intValue se pierden.
            ids[nombre] = capa.FindPropertyRelative("uniqueID").longValue;
            actuales.Add(nombre);
        }

        if (actuales.Count == CapasDeRender.Orden.Length && SonIguales(actuales, CapasDeRender.Orden))
            return;

        var usados = new HashSet<long>(ids.Values);
        capas.arraySize = CapasDeRender.Orden.Length;
        for (int i = 0; i < CapasDeRender.Orden.Length; i++)
        {
            string nombre = CapasDeRender.Orden[i];
            if (!ids.TryGetValue(nombre, out long id))
            {
                // Los ids son enteros al azar; solo tienen que ser unicos y positivos.
                do { id = Random.Range(1, int.MaxValue); } while (usados.Contains(id));
                usados.Add(id);
            }

            SerializedProperty capa = capas.GetArrayElementAtIndex(i);
            capa.FindPropertyRelative("name").stringValue = nombre;
            capa.FindPropertyRelative("uniqueID").longValue = id;
            capa.FindPropertyRelative("locked").boolValue = false;
        }

        tagManager.ApplyModifiedProperties();
        AssetDatabase.SaveAssets();
        informe.AppendLine("Capas de ordenamiento: " + string.Join(", ", CapasDeRender.Orden));
    }

    private static bool SonIguales(List<string> a, string[] b)
    {
        for (int i = 0; i < b.Length; i++)
            if (a[i] != b[i]) return false;
        return true;
    }

    // CLASIFICACION

    private static Rol Clasificar(Renderer r, out GameObject duenio, out bool estatico)
    {
        duenio = null;
        estatico = true;

        if (r is TilemapRenderer || !(r is SpriteRenderer || r is MeshRenderer))
            return Rol.Ninguno;

        // Lo mas especifico primero: un haz reflejado cuelga de un bloque, pero es luz.
        if (r.GetComponentInParent<ReflectiveLightEmitter>() != null) return Rol.Luz;
        if (r.GetComponentInParent<SpotLightDetector>() != null) return Rol.Luz;
        if (r.GetComponentInParent<TopLightDetector>() != null) return Rol.Luz;
        if (r.GetComponentInParent<RangoVisual>() != null || r.GetComponentInParent<PulseEffect>() != null) return Rol.Suelo;
        if (r.GetComponentInParent<BloquePreview>() != null) return Rol.Efecto;

        // Moviles: se reordenan en cada frame.
        if (Buscar<Jugador>(r, out duenio) || Buscar<NPCInteractivo>(r, out duenio)
            || Buscar<NPCDemostrador>(r, out duenio) || Buscar<AbyssFlame>(r, out duenio))
        {
            estatico = false;
            return Rol.Entidad;
        }

        // Fijas: se ordenan una vez al activarse.
        if (Buscar<ShadowBlock>(r, out duenio) || Buscar<Door>(r, out duenio) || Buscar<DoorHub>(r, out duenio)
            || Buscar<Switch>(r, out duenio) || Buscar<LightReceptor>(r, out duenio))
            return Rol.Entidad;

        return Rol.Ninguno;
    }

    private static bool Buscar<T>(Renderer r, out GameObject duenio) where T : Component
    {
        T c = r.GetComponentInParent<T>();
        duenio = c != null ? c.gameObject : null;
        return c != null;
    }

    // PROCESO

    private static void ProcesarPrefab(string ruta)
    {
        if (ruta.Contains("TextMesh Pro")) return;

        GameObject raiz = PrefabUtility.LoadPrefabContents(ruta);
        try
        {
            int cambios = Procesar(raiz, enEscena: false);
            if (cambios > 0)
            {
                if (!soloVerificar) PrefabUtility.SaveAsPrefabAsset(raiz, ruta);
                informe.AppendLine($"Prefab {Path.GetFileNameWithoutExtension(ruta)}: {cambios} cambios");
            }
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(raiz);
        }
    }

    private static void ProcesarEscena(string ruta)
    {
        string nombre = Path.GetFileNameWithoutExtension(ruta);
        if (nombre == Escenas.CoreManagers || nombre == "TestDataStructures") return;

        Scene escena = EditorSceneManager.OpenScene(ruta, OpenSceneMode.Additive);
        int cambios = 0;
        foreach (GameObject raiz in escena.GetRootGameObjects())
            cambios += Procesar(raiz, enEscena: true);

        if (cambios > 0)
        {
            if (!soloVerificar) EditorSceneManager.SaveScene(escena);
            informe.AppendLine($"Escena {nombre}: {cambios} cambios");
        }
        EditorSceneManager.CloseScene(escena, true);
    }

    /// <summary>Aplica las reglas a todo lo que cuelga de raiz y devuelve cuantos cambios hizo.</summary>
    private static int Procesar(GameObject raiz, bool enEscena)
    {
        int cambios = 0;
        var entidades = new Dictionary<GameObject, List<Renderer>>();
        var estaticas = new Dictionary<GameObject, bool>();

        foreach (Renderer r in raiz.GetComponentsInChildren<Renderer>(true))
        {
            Rol rol = Clasificar(r, out GameObject duenio, out bool estatico);
            if (rol == Rol.Ninguno) continue;

            // En una escena, la instancia hereda lo del prefab: se descartan overrides propios.
            if (enEscena && PrefabUtility.IsPartOfPrefabInstance(r))
                cambios += RevertirOverrides(r);

            switch (rol)
            {
                case Rol.Entidad:
                    if (!entidades.TryGetValue(duenio, out var lista))
                        entidades[duenio] = lista = new List<Renderer>();
                    lista.Add(r);
                    estaticas[duenio] = estatico;
                    break;
                case Rol.Luz: cambios += Fijar(r, CapasDeRender.Luces, null); break;
                case Rol.Suelo: cambios += Fijar(r, CapasDeRender.Luces, -1); break;
                case Rol.Efecto: cambios += Fijar(r, CapasDeRender.Efectos, null); break;
            }
        }

        foreach (KeyValuePair<GameObject, List<Renderer>> par in entidades)
        {
            int menor = int.MaxValue;
            foreach (Renderer r in par.Value) menor = Mathf.Min(menor, r.sortingOrder);

            // Orden relativo: la altura la pone OrdenPorAltura, no un numero a mano.
            foreach (Renderer r in par.Value)
                cambios += Fijar(r, CapasDeRender.Entidades, r.sortingOrder - menor);

            if (par.Key.GetComponent<OrdenPorAltura>() == null)
            {
                var orden = par.Key.AddComponent<OrdenPorAltura>();
                orden.estatico = estaticas[par.Key];
                EditorUtility.SetDirty(par.Key);
                cambios++;
            }
        }

        cambios += ActualizarLuces2D(raiz);
        return cambios;
    }

    // Una luz que no ilumina Default tampoco iluminaba a las entidades: se respeta.
    private static int ActualizarLuces2D(GameObject raiz)
    {
        int cambios = 0;
        foreach (Light2D luz in raiz.GetComponentsInChildren<Light2D>(true))
        {
            var so = new SerializedObject(luz);
            SerializedProperty capas = so.FindProperty("m_ApplyToSortingLayers");
            if (capas == null) continue;

            var lista = new List<int>();
            for (int i = 0; i < capas.arraySize; i++)
                lista.Add(capas.GetArrayElementAtIndex(i).intValue);
            if (!lista.Contains(0)) continue;

            int antes = lista.Count;
            foreach (SortingLayer capa in SortingLayer.layers)
                if (!lista.Contains(capa.id)) lista.Add(capa.id);
            if (lista.Count == antes) continue;

            capas.arraySize = lista.Count;
            for (int i = 0; i < lista.Count; i++)
                capas.GetArrayElementAtIndex(i).intValue = lista[i];
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(luz);
            cambios++;
        }
        return cambios;
    }

    /// <summary>Pone capa y orden (si se pide uno) y cuenta el cambio solo si algo era distinto.</summary>
    private static int Fijar(Renderer r, string capa, int? orden)
    {
        bool cambio = false;

        if (r.sortingLayerName != capa)
        {
            r.sortingLayerName = capa;
            cambio = true;
        }

        if (orden.HasValue && r.sortingOrder != orden.Value)
        {
            r.sortingOrder = orden.Value;
            cambio = true;
        }

        if (!cambio) return 0;

        EditorUtility.SetDirty(r);
        if (PrefabUtility.IsPartOfPrefabInstance(r))
            PrefabUtility.RecordPrefabInstancePropertyModifications(r);
        return 1;
    }

    private static int RevertirOverrides(Renderer r)
    {
        var so = new SerializedObject(r);
        int n = 0;
        foreach (string nombre in new[] { "m_SortingLayerID", "m_SortingLayer", "m_SortingOrder" })
        {
            SerializedProperty p = so.FindProperty(nombre);
            if (p == null || !p.prefabOverride) continue;

            PrefabUtility.RevertPropertyOverride(p, InteractionMode.AutomatedAction);
            n++;
        }
        return n;
    }
}
