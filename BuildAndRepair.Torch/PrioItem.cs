namespace BuildAndRepair.Torch;

public class PrioItem
{
    public string Alias;
    public int Key;

    public PrioItem(int key, string alias)
    {
        Key = key;
        Alias = alias;
    }

    public override string ToString()
    {
        return Alias;
    }
}