using System.Diagnostics.CodeAnalysis;
using Sandbox.Definitions;
using Sandbox.Game;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using VRage.Game;
using VRage.Game.ModAPI;
using VRageMath;

namespace BuildAndRepair.Torch;

public static class Utils
{
    /// <summary>
    ///     Is the block damaged/incomplete/projected
    /// </summary>
    public static bool NeedRepair(this IMySlimBlock target, bool functionalOnly)
    {
        //I use target.HasDeformation && target.MaxDeformation > X) as I had several times both situations, a landing gear reporting HasDeformation or a block reporting target.MaxDeformation > 0.1 both weren't repairable and caused welding this blocks forever!
        //Now I had the case that target.HasDeformation = true and target.MaxDeformation=0 and the block was deformed -> I removed the double Check
        //target.IsFullyDismounted is equals to target.IsDestroyed
        var neededIntegrityLevel = functionalOnly ? target.MaxIntegrity * ((MyCubeBlockDefinition)target.BlockDefinition).CriticalIntegrityRatio : target.MaxIntegrity;
        return target is { IsDestroyed: false, FatBlock: not { Closed: true } } && (target.Integrity < neededIntegrityLevel || target.HasDeformation);
    }

    /// <summary>
    ///     Is the grid a projected grid
    /// </summary>
    public static bool IsProjected(this IMyCubeGrid target)
    {
        var cubeGrid = target as MyCubeGrid;
        return cubeGrid?.Projector != null;
    }

    /// <summary>
    ///     Is the block a projected block
    /// </summary>
    public static bool IsProjected(this IMySlimBlock target)
    {
        var cubeGrid = target.CubeGrid as MyCubeGrid;
        return cubeGrid?.Projector != null;
    }

    /// <summary>
    ///     Is the block a projected block
    /// </summary>
    public static bool IsProjected(this IMySlimBlock target, [NotNullWhen(true)] out IMyProjector? projector)
    {
        var cubeGrid = target.CubeGrid as MyCubeGrid;
        projector = cubeGrid?.Projector;
        return projector != null;
    }

    /// <summary>
    ///     Could the projected block be build
    ///     !GUI Thread!
    /// </summary>
    /// <param name="target"></param>
    /// <param name="gui"></param>
    /// <returns></returns>
    public static bool CanBuild(this IMySlimBlock target, bool gui)
    {
        var cubeGrid = target.CubeGrid as MyCubeGrid;
        if (cubeGrid?.Projector == null) return false;

        var projector = (IMyProjector)cubeGrid.Projector;
        return projector.CanBuild(target, gui) == BuildCheckResult.OK;
    }

    /// <summary>
    ///     The inventory is filled to X percent
    /// </summary>
    /// <param name="inventory"></param>
    /// <returns></returns>
    public static float IsFilledToPercent(this IMyInventory inventory)
    {
        return Math.Max((float)inventory.CurrentVolume / (float)inventory.MaxVolume, (float)inventory.CurrentMass / (float)((MyInventory)inventory).MaxMass);
    }

    /// <summary>
    ///     Checks if block is inside the given BoundingBox
    /// </summary>
    /// <param name="block"></param>
    /// <param name="areaBox"></param>
    /// <returns></returns>
    public static bool IsInRange(this IMySlimBlock block, ref MyOrientedBoundingBoxD areaBox, out double distance)
    {
        block.ComputeScaledHalfExtents(out var halfExtents);
        var matrix = block.CubeGrid.WorldMatrix;
        matrix.Translation = block.CubeGrid.GridIntegerToWorld(block.Position);
        var box = new MyOrientedBoundingBoxD(new(-halfExtents, halfExtents), matrix);
        var inRange = areaBox.Intersects(ref box);
        distance = inRange ? (areaBox.Center - box.Center).Length() : 0;
        return inRange;
    }

    /// <summary>
    ///     Get the block name for GUI
    /// </summary>
    /// <param name="slimBlock"></param>
    /// <returns></returns>
    public static string BlockName(this IMySlimBlock? slimBlock)
    {
        if (slimBlock == null)
            return "(none)";
        
        if (slimBlock.FatBlock is IMyTerminalBlock terminalBlock)
            return
                $"{(terminalBlock.CubeGrid != null ? terminalBlock.CubeGrid.DisplayName : "Unknown Grid")}.{terminalBlock.CustomName}";
        return
            $"{(slimBlock.CubeGrid != null ? slimBlock.CubeGrid.DisplayName : "Unknown Grid")}.{slimBlock.BlockDefinition.DisplayNameText}";

    }

    /// <summary>
    ///     Check the ownership of the grid
    /// </summary>
    /// <param name="cubeGrid"></param>
    /// <param name="userId"></param>
    /// <returns></returns>
    public static MyRelationsBetweenPlayerAndBlock GetUserRelationToOwner(this IMyCubeGrid cubeGrid, long userId, bool ignoreCubeGridList = false)
    {
        var enemies = false;
        var neutral = false;
        try
        {
            if (cubeGrid.BigOwners != null && cubeGrid.BigOwners.Count != 0)
                foreach (var key in cubeGrid.BigOwners)
                {
                    var relation = MyIDModule.GetRelationPlayerBlock(key, userId, MyOwnershipShareModeEnum.Faction);
                    if (relation == MyRelationsBetweenPlayerAndBlock.Owner || relation == MyRelationsBetweenPlayerAndBlock.FactionShare)
                        return relation;
                    if (relation == MyRelationsBetweenPlayerAndBlock.Enemies)
                        enemies = true;
                    else if (relation == MyRelationsBetweenPlayerAndBlock.Neutral)
                        neutral = true;
                }
            else if (!ignoreCubeGridList)
            {
                //E.G. the case if a landing gear is directly attatched to piston/rotor (with no ownable block in the same subgrid) and the gear gets connected to something
                var cubegridsList = MyAPIGateway.GridGroups.GetGroup(cubeGrid, GridLinkTypeEnum.Mechanical);
                if (cubegridsList != null)
                    foreach (var cubeGrid1 in cubegridsList)
                    {
                        if (cubeGrid1 == cubeGrid) continue;
                        var relation = cubeGrid1.GetUserRelationToOwner(userId, true); //Do not recurse as this list is already complete
                        if (relation == MyRelationsBetweenPlayerAndBlock.Owner || relation == MyRelationsBetweenPlayerAndBlock.FactionShare)
                            return relation;
                        if (relation == MyRelationsBetweenPlayerAndBlock.Enemies)
                            enemies = true;
                        else if (relation == MyRelationsBetweenPlayerAndBlock.Neutral)
                            neutral = true;
                    }
            }
        }
        catch
        {
            //The list BigOwners could change while iterating -> a silent catch
        }

        if (enemies)
            return MyRelationsBetweenPlayerAndBlock.Enemies;

        if (neutral)
            return MyRelationsBetweenPlayerAndBlock.Neutral;

        return MyRelationsBetweenPlayerAndBlock.NoOwnership;
    }

    /// <summary>
    ///     Return relation between player and grid, in case of 'NoOwnership' check the grid owner.
    /// </summary>
    /// <param name="slimBlock"></param>
    /// <param name="userId"></param>
    /// <returns></returns>
    public static MyRelationsBetweenPlayerAndBlock GetUserRelationToOwner(this IMySlimBlock slimBlock, long userId)
    {
        if (slimBlock == null)
            return MyRelationsBetweenPlayerAndBlock.NoOwnership;
        var fatBlock = slimBlock.FatBlock;
        if (fatBlock != null)
        {
            var relation = fatBlock.GetUserRelationToOwner(userId);
            if (relation == MyRelationsBetweenPlayerAndBlock.NoOwnership)
            {
                relation = GetUserRelationToOwner(slimBlock.CubeGrid, userId);
                return relation;
            }

            return relation;
        }
        else
        {
            var relation = GetUserRelationToOwner(slimBlock.CubeGrid, userId);
            return relation;
        }
    }

    public static int CompareDistance(double a, double b)
    {
        var diff = a - b;
        return Math.Abs(diff) < 0.00001 ? 0 : diff > 0 ? 1 : -1;
    }

    public static bool IsCharacterPlayerAndActive(IMyCharacter? character)
    {
        return character is { IsPlayer: true, Closed: false, InScene: true, IsDead: false };
    }
}