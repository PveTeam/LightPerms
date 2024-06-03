using System.Collections.Concurrent;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using SpaceEngineers.Game.ModAPI;
using VRage.Game;
using VRage.Game.ModAPI;
using VRageMath;

namespace BuildAndRepair.Torch;

public static class SafeZoneProtection
{
    private static readonly int IntervalToCacheSeconds = 10;

    public static ConcurrentDictionary<long, EntityProtectedState> GrindingNotAllowedCache = new();

    public static T CastProhibit<T>(T ptr, object val)
    {
        return (T)val;
    }

    private static bool? IsGrindingAllowed(long entityId)
    {
        try
        {
            if (GrindingNotAllowedCache.ContainsKey(entityId))
            {
                if (MyAPIGateway.Session.ElapsedPlayTime.Subtract(GrindingNotAllowedCache[entityId].Checked).TotalSeconds > IntervalToCacheSeconds)
                {
                    Deb.Write("Cache expired.");
                    EntityProtectedState state;
                    GrindingNotAllowedCache.TryRemove(entityId, out state);
                }
                else
                {
                    Deb.Write("Cached found");
                    return GrindingNotAllowedCache[entityId].IsAllowed;
                }
            }
        }
        catch
        {
            // ignored
        }

        return null;
    }

    public static void SetIsAllowed(long entityId, bool isAllowed)
    {
        try
        {
            GrindingNotAllowedCache[entityId] = new()
            {
                IsAllowed = isAllowed,
                Checked = MyAPIGateway.Session.ElapsedPlayTime
            };
            Deb.Write($"Set cache: {isAllowed}");
        }
        catch
        {
            // ignored
        }
    }

    public static bool IsGridAllowedGrinding(MyCubeGrid grid)
    {
        try
        {
            var isAllowed = IsGrindingAllowed(grid.EntityId);
            if (isAllowed.HasValue)
                return isAllowed.Value;
            isAllowed = MySessionComponentSafeZones.IsActionAllowed(grid, CastProhibit(MySessionComponentSafeZones.AllowedActions, 16));
            SetIsAllowed(grid.EntityId, isAllowed.Value);
            return isAllowed.Value;
        }
        catch
        {
            // ignored
        }

        return true;
    }

    public static bool IsProtected(IMySlimBlock targetBlock, IMyCubeBlock attackerBlock)
    {
        try
        {
            if (targetBlock != null && attackerBlock != null)
            {
                long fatBlockId = 0;
                if (targetBlock.FatBlock != null)
                {
                    fatBlockId = targetBlock.FatBlock.EntityId;
                    var cached = IsGrindingAllowed(fatBlockId);
                    if (cached != null)
                        return cached.Value;
                }

                var sphere = new BoundingSphereD(attackerBlock.GetPosition(), 500);
                var list = MyAPIGateway.Entities.GetEntitiesInSphere(ref sphere);
                var safeZones = list.OfType<MySafeZone>().ToList();

                if (safeZones.Any())
                {
                    targetBlock.GetWorldBoundingBox(out var targetBox);

                    BoundingBoxD attackerBox;
                    attackerBlock.SlimBlock.GetWorldBoundingBox(out attackerBox);

                    foreach (var safeZone in safeZones)
                    {
                        // Create a new sphere for the safe zone.
                        var checkSphere = new BoundingSphereD(safeZone.PositionComp.GetPosition(), safeZone.Radius);

                        // Get intersections checks..
                        var targetIntersects = checkSphere.Intersects(targetBox);
                        if (targetIntersects)
                        {
                            // If it is a safe-zone block.
                            if (safeZone.SafeZoneBlockId > 0)
                            {
                                if (MyEntities.GetEntityByName(safeZone.SafeZoneBlockId.ToString()) is IMySafeZoneBlock safeZoneBlock && safeZoneBlock.Enabled && safeZoneBlock.IsSafeZoneEnabled())
                                {
                                    var isAllowed = safeZone.IsActionAllowed(CastProhibit(MySessionComponentSafeZones.AllowedActions, 16), 0L, targetBox);
                                    if (isAllowed)
                                    {
                                        var safeZoneBlockOwner = UtilsPlayer.GetOwner(safeZoneBlock.CubeGrid);
                                        var targetGridOwner = UtilsPlayer.GetOwner(targetBlock.CubeGrid);
                                        var attackerGridOwner = UtilsPlayer.GetOwner(attackerBlock.CubeGrid);

                                        if (safeZoneBlockOwner == attackerGridOwner || targetGridOwner == attackerGridOwner)
                                        {
                                            if (fatBlockId > 0)
                                                SetIsAllowed(fatBlockId, true);
                                            return false;
                                        }

                                        // Check relation between owners.
                                        var relation = attackerBlock.GetUserRelationToOwner(targetGridOwner);
                                        if (relation == MyRelationsBetweenPlayerAndBlock.Owner ||
                                            relation == MyRelationsBetweenPlayerAndBlock.FactionShare)
                                        {
                                            if (fatBlockId > 0)
                                                SetIsAllowed(fatBlockId, true);
                                            return false;
                                        }
                                    }

                                    if (fatBlockId > 0)
                                        SetIsAllowed(fatBlockId, false);
                                    return true;
                                }
                            }

                            if (fatBlockId > 0)
                                SetIsAllowed(fatBlockId, false);
                            return true;
                        }
                    }
                }
            }
        }
        catch
        {
            // ignored
        }

        return false;
    }

    public struct EntityProtectedState
    {
        public bool IsAllowed;
        public TimeSpan Checked;
    }
}