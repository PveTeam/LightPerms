namespace BuildAndRepair.Torch;

/// <summary>
///     Hash list for TargetEntityData
/// </summary>
public class TargetEntityDataHashList : HashList<TargetEntityData, SyncTargetEntityData>
{
    public override List<SyncTargetEntityData> GetSyncList()
    {
        var result = new List<SyncTargetEntityData>();
        var idx = 0;
        foreach (var item in this)
        {
            result.Add(new()
                           { Entity = SyncEntityId.GetSyncId(item.Entity), Distance = item.Distance });
            idx++;
            if (idx > SyncBlockState.MaxSyncItems) break;
        }

        return result;
    }

    public override void RebuildHash()
    {
        uint hash = 0;
        var idx = 0;
        lock (this)
        {
            foreach (var entry in this)
            {
                hash ^= UtilsSynchronization.RotateLeft((uint)entry.Entity.GetHashCode(), idx + 1);
                idx++;
                if (idx >= SyncBlockState.MaxSyncItems) break;
            }

            CurrentCount = Count;
            CurrentHash = hash;
        }
    }
}