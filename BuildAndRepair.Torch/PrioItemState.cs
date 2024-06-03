namespace BuildAndRepair.Torch;

public class PrioItemState<T> where T : PrioItem
{
    public PrioItemState(T prioItem, bool enabled, bool visible)
    {
        PrioItem = prioItem;
        Enabled = enabled;
        Visible = visible;
    }

    public T PrioItem { get; }
    public bool Enabled { get; set; }
    public bool Visible { get; set; }
}