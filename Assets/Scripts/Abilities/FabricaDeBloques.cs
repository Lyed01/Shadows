using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Sabe que prefab le corresponde a cada tipo de bloque y lo arma. El que pide
/// un bloque no conoce los prefabs ni como se construyen: dice "dame un espejo"
/// y recibe uno listo.
///
/// Antes esto era un ternario dentro del GridManager:
///
///     GameObject prefab = reflectante ? prefabBloqueReflectante : prefabBloque;
///
/// Eso obligaba a abrir la clase y tocar codigo que ya andaba cada vez que se
/// queria un tipo de bloque nuevo. Con el diccionario no se toca ni una linea:
/// se arrastra un prefab mas al inspector y se le escribe su id.
///
/// El catalogo se arma una sola vez, leyendo el id que cada prefab declara en
/// su propio componente. La fabrica no tiene la lista de tipos escrita adentro.
///
/// Fabricar y reciclar son dos trabajos distintos: la fabrica decide QUE
/// construir, y el pool decide SI hace falta construirlo o alcanza con reusar
/// uno guardado.
/// </summary>
public class FabricaDeBloques
{
    // Estatico: se carga una vez con los prefabs de bloque (un punado de ids) y
    // despues solo se consulta, cada vez que el jugador coloca un bloque.
    private readonly ISimpleDictionary<string, GameObject> catalogo =
        new SimpleArrayDictionary<string, GameObject>();
    private readonly PoolDeBloques pool;

    public FabricaDeBloques(PoolDeBloques pool)
    {
        this.pool = pool;
    }

    /// <summary>Los ids que la fabrica sabe construir.</summary>
    public IEnumerable<string> TiposConocidos => catalogo.Keys();

    /// <summary>
    /// Suma prefabs al catalogo, cada uno bajo el id que trae declarado. Un
    /// prefab sin ShadowBlock, sin id, o con un id repetido se ignora con
    /// aviso: es un error de configuracion, no algo que deba tirar el juego.
    /// </summary>
    public void Registrar(params GameObject[] prefabs)
    {
        if (prefabs == null) return;

        foreach (GameObject prefab in prefabs)
        {
            if (prefab == null) continue;

            ShadowBlock bloque = prefab.GetComponent<ShadowBlock>();
            if (bloque == null)
            {
                Log.Aviso(typeof(FabricaDeBloques), $"El prefab {prefab.name} no tiene ShadowBlock.");
                continue;
            }

            string id = bloque.idTipo;
            if (string.IsNullOrWhiteSpace(id))
            {
                Log.Aviso(typeof(FabricaDeBloques), $"El prefab {prefab.name} no declara idTipo.");
                continue;
            }

            if (catalogo.TryGetValue(id, out GameObject yaEstaba))
            {
                if (yaEstaba != prefab)
                    Log.Aviso(typeof(FabricaDeBloques),
                        $"El id '{id}' lo declaran {yaEstaba.name} y {prefab.name}; queda el primero.");

                continue;
            }

            catalogo.Add(id, prefab);
        }
    }

    /// <summary>Si la fabrica sabe construir ese tipo.</summary>
    public bool Conoce(string id) => id != null && catalogo.ContainsKey(id);

    /// <summary>
    /// Construye el bloque del tipo pedido, o null si ese id no esta en el
    /// catalogo. Pasa por el pool, asi que puede devolver uno reciclado.
    /// </summary>
    public ShadowBlock Crear(string id, Vector3 posicion)
    {
        if (!catalogo.TryGetValue(id ?? string.Empty, out GameObject prefab))
        {
            Log.Aviso(typeof(FabricaDeBloques), $"No hay ningun bloque registrado con el id '{id}'.");
            return null;
        }

        return pool.Obtener(prefab, posicion);
    }

    /// <summary>
    /// Cuantas cargas cuesta ese tipo de bloque. El dato vive en el prefab, no
    /// en quien lo coloca: antes el costo estaba escrito a mano en la
    /// habilidad, que daba por sentado cuantos tipos habia.
    /// </summary>
    public int CostoDe(string id)
    {
        if (!catalogo.TryGetValue(id ?? string.Empty, out GameObject prefab))
            return 1;

        ShadowBlock bloque = prefab.GetComponent<ShadowBlock>();
        return bloque != null ? Mathf.Max(1, bloque.costoCargas) : 1;
    }
}
