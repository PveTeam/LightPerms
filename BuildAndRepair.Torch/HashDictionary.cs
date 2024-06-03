namespace BuildAndRepair.Torch;

/// <summary>
///     List including Hash Values to detect changes
/// </summary>
/// <typeparam name="T"></typeparam>
public abstract class HashDictionary<T, T1, ST> : Dictionary<T, T1>
{
    public long CurrentHash { get; protected set; }

    public long LastHash { get; set; }

    public int CurrentCount { get; protected set; }

    public abstract void RebuildHash();

    public abstract List<ST> GetSyncList();
}