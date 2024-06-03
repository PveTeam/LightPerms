namespace BuildAndRepair.Torch;

[Flags]
public enum SearchModes
{
    /// <summary>
    ///     Search Target blocks only inside connected blocks
    /// </summary>
    Grids = 0x0001,

    /// <summary>
    ///     Search Target blocks in bounding boy independend of connection
    /// </summary>
    BoundingBox = 0x0002
}