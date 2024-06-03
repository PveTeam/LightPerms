using ProtoBuf;
using Sandbox.ModAPI;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRageMath;

namespace BuildAndRepair.Torch;

[ProtoContract(UseProtoMembersOnly = true)]
public class SyncEntityId
{
    [ProtoMember(1)]
    public long EntityId { get; set; }

    [ProtoMember(2)]
    public long GridId { get; set; }

    [ProtoMember(3)]
    public Vector3I? Position { get; set; }

    [ProtoMember(4)]
    public BoundingBoxD? Box { get; set; }

    public override string ToString()
    {
        return $"EntityId={EntityId}, GridId={GridId}, Position={Position}, Box={Box}";
    }

    public static SyncEntityId GetSyncId(object item)
    {
        if (item == null) return null;
        if (item is IMySlimBlock slimBlock)
        {
            if (slimBlock.FatBlock != null)
                return new()
                    { EntityId = slimBlock.FatBlock.EntityId, GridId = slimBlock.CubeGrid?.EntityId ?? 0, Position = slimBlock.Position };
            if (slimBlock.CubeGrid != null)
                return new()
                    { EntityId = 0, GridId = slimBlock.CubeGrid.EntityId, Position = slimBlock.Position };
        }

        if (item is IMyVoxelBase voxelBase)
            return new()
                { Box = voxelBase.WorldAABB };

        if (item is IMyEntity entity)
            return new()
                { EntityId = entity.EntityId };

        if (item is Vector3D position)
            return new()
                { Position = new Vector3I((int)position.X, (int)position.Y, (int)position.Z) };

        return null;
    }

    public static object GetItem(SyncEntityId id)
    {
        if (id == null) return null;

        if (id.EntityId != 0)
        {
            if (MyAPIGateway.Entities.TryGetEntityById(id.EntityId, out var entity))
                return entity;
        }

        if (id.GridId != 0 && id.Position != null)
        {
            if (MyAPIGateway.Entities.TryGetEntityById(id.GridId, out var entity))
            {
                var grid = entity as IMyCubeGrid;
                return grid?.GetCubeBlock(id.Position.Value);
            }
        }

        if (id.Position != null)
            return id.Position;
        if (id.Box != null)
        {
            IMyEntity entity;
            var box = id.Box.Value;
            if ((entity = MyAPIGateway.Session.VoxelMaps.GetVoxelMapWhoseBoundingBoxIntersectsBox(ref box, null)) != null)
                return entity;
        }

        return null;
    }

    public static IMySlimBlock GetItemAsSlimBlock(SyncEntityId id)
    {
        var item = GetItem(id);
        if (item is IMySlimBlock slimBlock) return slimBlock;

        var block = item as IMyCubeBlock;
        return block?.SlimBlock;
    }

    public static T GetItemAs<T>(SyncEntityId id) where T : class
    {
        return GetItem(id) as T;
    }

    public override bool Equals(object obj)
    {
        if (obj == null || obj is not SyncEntityId syncObj)
            return false;
        return EntityId == syncObj.EntityId && GridId == syncObj.GridId &&
               Position.Equals(syncObj.Position) && Box.Equals(syncObj.Box);
    }

    public override int GetHashCode()
    {
        return EntityId.GetHashCode() + (GridId.GetHashCode() << 8) + ((Position.HasValue ? Position.Value.GetHashCode() : 0) << 16) + ((Box.HasValue ? Box.Value.GetHashCode() : 0) << 24);
    }
}