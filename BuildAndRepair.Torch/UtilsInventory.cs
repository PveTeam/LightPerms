using Entities.Blocks;
using Sandbox.Definitions;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.GameSystems;
using Sandbox.ModAPI.Ingame;
using SpaceEngineers.Game.Entities.Blocks;
using VRage;
using VRage.Game;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using MyInventoryItem = VRage.Game.ModAPI.Ingame.MyInventoryItem;
using MyItemType = VRage.Game.ModAPI.Ingame.MyItemType;

namespace BuildAndRepair.Torch;

public static class UtilsInventory
{
    public delegate bool ExcludeInventory(IMyInventory destInventory, IMyInventory srcInventory, ref MyInventoryItem srcItem);

    public enum IntegrityLevel
    {
        Create,
        Functional,
        Complete
    }

    /// <summary>
    ///     Check if all inventories are empty
    /// </summary>
    /// <param name="entity"></param>
    /// <returns></returns>
    public static bool InventoriesEmpty(this IMyEntity entity)
    {
        if (!entity.HasInventory) return true;
        for (var i1 = 0; i1 < entity.InventoryCount; ++i1)
        {
            var srcInventory = entity.GetInventory(i1);
            if (!srcInventory.Empty()) return false;
        }

        return true;
    }

    /// <summary>
    ///     Push all components into destinations
    /// </summary>
    public static bool PushComponents(this IMyCubeBlock src, List<IMyInventory> destinations, ExcludeInventory? exclude)
    {
        var welder = (MyShipWelder)src;

        var success = false;
        foreach (var item in welder.GetInventory().GetItems())
        {
            success |= MyGridConveyorSystem.ItemPushRequest(welder, welder.GetInventory(), item);
        }

        return success;
    }

    /// <summary>
    ///     Retrieve the total amount of componets to build a blueprint
    ///     (blueprint loaded inside projector)
    /// </summary>
    /// <param name="projector"></param>
    /// <param name="componentList"></param>
    public static int NeededComponents4Blueprint(IMyProjector srcProjector, Dictionary<MyDefinitionId, MyFixedPoint> componentList)
    {
        if (componentList == null || srcProjector is not MySpaceProjector { IsWorking: true, ProjectedGrid: { } projectedGrid }) return -1;

        foreach (var component in projectedGrid.GetBlocks().Concat(NanobotBuildAndRepairSystemMod.MultigridProjectorApi.Available ? 
                                                                       Enumerable.Range(0, NanobotBuildAndRepairSystemMod.MultigridProjectorApi.GetSubgridCount(srcProjector.EntityId))
                                                                                 .SelectMany(b => ((MyCubeGrid)NanobotBuildAndRepairSystemMod.MultigridProjectorApi.GetPreviewGrid(srcProjector.EntityId, b)).GetBlocks())
                                                                       : Enumerable.Empty<MySlimBlock>())
                 .SelectMany(block => block.BlockDefinition.Components))
        {
            if (componentList.ContainsKey(component.Definition.Id)) componentList[component.Definition.Id] += component.Count;
            else componentList[component.Definition.Id] = component.Count;
        }

        return componentList.Count;
    }

    /// <summary>
    ///     Retrieve the amount of components to build the block to the given index
    /// </summary>
    /// <param name="block"></param>
    /// <param name="componentList"></param>
    /// <param name="level">integrity level </param>
    public static void GetMissingComponents(this IMySlimBlock block, Dictionary<string, int> componentList, IntegrityLevel level)
    {
        var blockDefinition = block.BlockDefinition as MyCubeBlockDefinition;
        if (blockDefinition.Components == null || blockDefinition.Components.Length == 0) return;

        if (level == IntegrityLevel.Create)
        {
            var component = blockDefinition.Components[0];
            componentList.Add(component.Definition.Id.SubtypeName, 1);
        }
        else
        {
            if (block.IsProjected())
            {
                var maxIdx = level == IntegrityLevel.Functional ? blockDefinition.CriticalGroup + 1 : blockDefinition.Components.Length;
                for (var idx = 0; idx < maxIdx; idx++)
                {
                    var component = blockDefinition.Components[idx];
                    if (componentList.ContainsKey(component.Definition.Id.SubtypeName)) componentList[component.Definition.Id.SubtypeName] += component.Count;
                    else componentList.Add(component.Definition.Id.SubtypeName, component.Count);
                }
            }
            else
            {
                block.GetMissingComponents(componentList);
                if (level == IntegrityLevel.Functional)
                    for (var idx = blockDefinition.CriticalGroup + 1; idx < blockDefinition.Components.Length; idx++)
                    {
                        var component = blockDefinition.Components[idx];
                        if (componentList.ContainsKey(component.Definition.Id.SubtypeName))
                        {
                            var amount = componentList[component.Definition.Id.SubtypeName];
                            if (amount <= component.Count) componentList.Remove(component.Definition.Id.SubtypeName);
                            else componentList[component.Definition.Id.SubtypeName] -= component.Count;
                        }
                    }
            }
        }
    }
}