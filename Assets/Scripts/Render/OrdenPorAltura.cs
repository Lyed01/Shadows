using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Ordena una entidad por la altura de su base: el que esta mas abajo en
/// pantalla se dibuja delante. Asi Shawy pasa por detras de un NPC o una puerta
/// que tiene mas abajo, y por delante de los que tiene mas arriba.
///
/// Se hace por script y no con el ordenamiento por eje de Unity porque los
/// sprites tienen el pivote en el centro: ordenar por la posicion pondria a un
/// sprite alto delante de uno bajo aunque su base este mas arriba. Aca se mide
/// la base real del sprite (el borde de abajo), sea cual sea el pivote.
///
/// Va en la raiz de la entidad. Los sprites hijos que existen al empezar
/// definen la base y conservan su orden relativo (por ejemplo, las hojas de
/// una puerta). Los que nacen despues, como el contorno o la flecha de un
/// espejo, se suman con Seguir y solo acompanan a los demas.
/// </summary>
public class OrdenPorAltura : MonoBehaviour
{
    // sortingOrder es un short: 100 por unidad de mundo alcanza para +-320 unidades.
    private const int OrdenesPorUnidad = 100;
    private const int Limite = 32000;

    [Tooltip("Marcar si no se mueve (puertas, switches, bloques): se ordena al activarse y no gasta nada por frame.")]
    public bool estatico;

    private SpriteRenderer[] cuerpo;
    private readonly Dictionary<Renderer, int> desfases = new();
    private int ordenActual;

    void Awake()
    {
        cuerpo = System.Array.FindAll(GetComponentsInChildren<SpriteRenderer>(true), r => r.sprite != null);
        if (cuerpo.Length == 0) return;

        int menor = int.MaxValue;
        foreach (SpriteRenderer r in cuerpo) menor = Mathf.Min(menor, r.sortingOrder);
        foreach (SpriteRenderer r in cuerpo) desfases[r] = r.sortingOrder - menor;
    }

    void OnEnable() => Ordenar();

    void LateUpdate()
    {
        if (!estatico) Ordenar();
    }

    /// <summary>
    /// Suma un sprite creado en juego (un contorno, una flecha): conserva el
    /// desfase con el que nacio respecto del orden actual de la entidad y la
    /// sigue de ahi en mas. Quien lo crea tiene que llamarlo.
    /// </summary>
    public void Seguir(Renderer renderer)
    {
        desfases[renderer] = renderer.sortingOrder - ordenActual;
        if (estatico) Ordenar();
    }

    private void Ordenar()
    {
        if (cuerpo == null || cuerpo.Length == 0) return;

        float baseY = float.MaxValue;
        foreach (SpriteRenderer r in cuerpo)
            if (r != null && r.enabled) baseY = Mathf.Min(baseY, r.bounds.min.y);
        if (baseY == float.MaxValue) return;

        ordenActual = Mathf.Clamp(-Mathf.RoundToInt(baseY * OrdenesPorUnidad), -Limite, Limite);

        foreach (KeyValuePair<Renderer, int> par in desfases)
            if (par.Key != null) par.Key.sortingOrder = ordenActual + par.Value;
    }

#if UNITY_EDITOR
    // Al agregarlo desde el Inspector, la entidad pasa a su capa.
    void Reset()
    {
        foreach (SpriteRenderer r in GetComponentsInChildren<SpriteRenderer>(true))
            r.sortingLayerName = CapasDeRender.Entidades;
    }
#endif
}
