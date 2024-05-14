using System.Reflection;
using Sandbox.Definitions;
using Sandbox.Game;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Inventory;
using Torch.Managers.PatchManager;
using Torch.Utils;
using VRage;
using VRage.Game;
using VRage.Game.Entity;
using VRageMath;

namespace FloatingObjects.Patches;

internal static class StackItemsPatch
{
    [ReflectedMethodInfo(typeof(MyCubeBlock), "ReleaseInventoryAsFloatingObjects")]
    private static readonly MethodInfo ReleaseInventoryMethod = null!;

    public static void Patch(PatchContext ctx)
    {
        ctx.GetPattern(ReleaseInventoryMethod).Prefixes
            .Add(new Func<MyCubeBlock, MyInventory, bool, bool>(Prefix).Method);
    }

    private static bool Prefix(MyCubeBlock __instance, MyInventory inventory, bool damageContent)
    {
        foreach (var items in inventory.GetItems().GroupBy(b => b.Content.GetObjectId()))
        {
            var item = items.Aggregate((a, b) =>
                new MyPhysicalInventoryItem(a.Amount + b.Amount, a.Content, a.Scale));
            
            if (damageContent && item.Content.GetObjectId().TypeId == typeof(MyObjectBuilder_Component))
            {
                item.Amount = MyFixedPoint.Floor(item.Amount * MyDefinitionManager.Static.GetComponentDefinition(item.Content.GetObjectId()).DropProbability);
                if (item.Amount <= 0) continue;
            }

            MyFloatingObjects.EnqueueInventoryItemSpawn(item, __instance.PositionComp.WorldAABB,
                __instance.CubeGrid.Physics?.GetVelocityAtPoint(__instance.PositionComp.GetPosition()) ??
                Vector3.Zero);
        }

        return false;
    }
}