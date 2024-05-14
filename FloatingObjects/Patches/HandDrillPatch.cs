using System.Reflection;
using Sandbox.Game.Entities;
using Sandbox.Game.Weapons;
using Torch.Managers.PatchManager;
using Torch.Utils;
using VRage;

namespace FloatingObjects.Patches;

internal static class HandDrillPatch
{
    [ReflectedMethodInfo(typeof(MyDrillBase), "SpawnOrePieces")]
    private static readonly MethodInfo SpawnOrePiecesMethod = null!;

    [ReflectedSetter(Name = "m_inventoryCollectionRatio")]
    private static readonly Action<MyDrillBase, MyFixedPoint> InventoryCollectionRatioSetter = null!;

    public static void Patch(PatchContext ctx)
    {
        ctx.GetPattern(SpawnOrePiecesMethod).Prefixes.Add(new Func<MyDrillBase, bool>(Prefix).Method);
    }

    private static bool Prefix(MyDrillBase __instance)
    {
        if (__instance.OutputInventory == null ||
            __instance.IgnoredEntities.ElementAtOrDefault(0) is not MyHandDrill { Owner: not null } drill) return false;
        
        __instance.OutputInventory = drill.Owner.GetInventory();
        InventoryCollectionRatioSetter(__instance, (MyFixedPoint)0.9f);
        return false;

    }
}