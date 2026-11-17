/// <summary>Arista dirigida de un Graph: hacia que nodo va y cuanto cuesta recorrerla.</summary>
public readonly struct Edge<T>
{
    public readonly T To;
    public readonly int Cost;

    public Edge(T to, int cost)
    {
        To = to;
        Cost = cost;
    }
}
