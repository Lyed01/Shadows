/// <summary>
/// Nodo del arbol binario: guarda un valor y apunta a sus dos hijos. Un hijo
/// null significa que ahi no hay subarbol.
///
/// Es una clase propia (no un KeyValuePair ni un arreglo) por el mismo motivo
/// que los nodos de las listas: el arbol se rearma cambiando punteros, y el
/// valor se puede reemplazar sin tocar la estructura.
/// </summary>
public class TreeNode<T>
{
    public T Value;
    public TreeNode<T> Left;
    public TreeNode<T> Right;

    /// <summary>Altura del subarbol que cuelga de este nodo. Solo la mantiene el AVL.</summary>
    public int Height = 1;

    public TreeNode(T value)
    {
        Value = value;
        Left = null;
        Right = null;
    }

    public bool IsLeaf => Left == null && Right == null;
}
