using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Guarda los bloques que ya no estan en juego en vez de destruirlos, y los
/// vuelve a entregar cuando hace falta uno.
///
/// Los bloques de sombra son la mecanica central: el jugador los coloca, la luz
/// los consume y vuelve a colocarlos, todo el tiempo. Con Instantiate y Destroy
/// cada ciclo pedia memoria nueva y dejaba basura para el recolector, en pleno
/// loop de juego. Reciclar el mismo objeto evita las dos cosas.
///
/// Cada prefab tiene su propia fila de reserva: un bloque normal no sirve para
/// reemplazar a uno reflectante. La fila es una pila, porque el ultimo bloque
/// que se guardo es el que sigue caliente en memoria.
///
/// No es un MonoBehaviour: lo crea y lo tiene el GridManager, asi que no hay
/// que agregar nada en las escenas. Los objetos guardados cuelgan de un
/// contenedor propio para que la jerarquia no se llene de bloques apagados.
/// </summary>
public class PoolDeBloques
{
    // Estatico: hay una fila por prefab de bloque (dos o tres en total), asi que
    // buscar la fila es recorrer muy pocas claves, y se hace en cada Obtener y
    // cada Devolver.
    private readonly ISimpleDictionary<GameObject, SimpleArrayList<ShadowBlock>> reservas =
        new SimpleArrayDictionary<GameObject, SimpleArrayList<ShadowBlock>>();
    private Transform contenedor;

    /// <summary>Cuantos bloques hay guardados, sumando todos los prefabs.</summary>
    public int EnReserva
    {
        get
        {
            int total = 0;
            foreach (var par in reservas)
                total += par.Value.Count;

            return total;
        }
    }

    /// <summary>
    /// Entrega un bloque del prefab pedido, ya colocado y reiniciado. Saca uno
    /// de la reserva si hay; si no, instancia uno nuevo.
    /// </summary>
    public ShadowBlock Obtener(GameObject prefab, Vector3 posicion)
    {
        if (prefab == null) return null;

        SimpleArrayList<ShadowBlock> reserva = ReservaDe(prefab);

        // Se saca del final: es O(1) en el arreglo, porque no hay que correr nada.
        while (reserva.Count > 0)
        {
            int ultimo = reserva.Count - 1;
            ShadowBlock guardado = reserva[ultimo];
            reserva.RemoveAt(ultimo);

            // Un bloque puede haberse destruido con su escena mientras esperaba.
            if (guardado == null) continue;

            guardado.transform.SetPositionAndRotation(posicion, Quaternion.identity);
            guardado.transform.SetParent(null);
            guardado.gameObject.SetActive(true);
            guardado.Reiniciar();

            return guardado;
        }

        GameObject nuevo = Object.Instantiate(prefab, posicion, Quaternion.identity);

        ShadowBlock bloque = nuevo.GetComponent<ShadowBlock>();
        if (bloque == null)
        {
            Log.Aviso(typeof(PoolDeBloques), $"El prefab {prefab.name} no tiene ShadowBlock.");
            Object.Destroy(nuevo);
            return null;
        }

        bloque.pool = this;
        bloque.prefabDeOrigen = prefab;

        // Tambien se reinicia el recien creado, aunque Start lo haria igual al
        // final del frame: asi el bloque sale listo por los dos caminos y no
        // hay un rato en el que su vida todavia valga cero.
        bloque.Reiniciar();

        return bloque;
    }

    /// <summary>
    /// Recibe un bloque que salio de juego, lo apaga y lo deja listo para la
    /// proxima. Lo llama el propio bloque al destruirse.
    /// </summary>
    public void Devolver(ShadowBlock bloque)
    {
        if (bloque == null) return;

        if (bloque.prefabDeOrigen == null)
        {
            // Sin prefab de origen no se sabe a que fila vuelve.
            Object.Destroy(bloque.gameObject);
            return;
        }

        SimpleArrayList<ShadowBlock> reserva = ReservaDe(bloque.prefabDeOrigen);

        if (reserva.Contains(bloque)) return; // ya estaba guardado

        bloque.gameObject.SetActive(false);
        bloque.transform.SetParent(Contenedor());
        reserva.Add(bloque);
    }

    /// <summary>Destruye de verdad lo guardado. Se llama al descargar la escena.</summary>
    public void Vaciar()
    {
        foreach (var par in reservas)
        {
            SimpleArrayList<ShadowBlock> reserva = par.Value;

            for (int i = 0; i < reserva.Count; i++)
                if (reserva[i] != null)
                    Object.Destroy(reserva[i].gameObject);

            reserva.Clear();
        }

        reservas.Clear();

        if (contenedor != null)
            Object.Destroy(contenedor.gameObject);
    }

    private SimpleArrayList<ShadowBlock> ReservaDe(GameObject prefab)
    {
        if (!reservas.TryGetValue(prefab, out var reserva))
        {
            reserva = new SimpleArrayList<ShadowBlock>();
            reservas[prefab] = reserva;
        }

        return reserva;
    }

    private Transform Contenedor()
    {
        if (contenedor == null)
            contenedor = new GameObject("BloquesEnReserva").transform;

        return contenedor;
    }
}
