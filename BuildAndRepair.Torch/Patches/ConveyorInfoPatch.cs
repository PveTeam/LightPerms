using System.Reflection;
using Sandbox.Game;
using Sandbox.Game.Entities;
using Sandbox.Game.GameSystems.Conveyors;
using Sandbox.Game.World;
using SpaceEngineers.Game.Entities.Blocks;
using Torch.Managers.PatchManager;
using Torch.Utils;

namespace BuildAndRepair.Torch.Patches;

[PatchShim]
internal static class ConveyorInfoPatch
{
    [ReflectedMethodInfo(typeof(MyShipWelder), nameof(MyShipWelder.GetPushInformation))]
    private static readonly MethodInfo GetPushInformationMethod = null!;

    [ReflectedMethodInfo(typeof(ConveyorInfoPatch), nameof(Suffix))]
    private static readonly MethodInfo SuffixMethod = null!;

    public static void Patch(PatchContext ctx)
    {
        ctx.GetPattern(GetPushInformationMethod).Suffixes.Add(SuffixMethod);
    }

    private static void Suffix(MyShipWelder __instance, ref PullInformation __result)
    {
        if (__instance.GameLogic is not NanobotBuildAndRepairSystemBlock)
            return; // not a nanobot block

        __result = new()
        {
            Inventory = __instance.GetInventory(),
            OwnerID = __instance.OwnerId,
            Constraint = new("Empty constraint")
        };
    }
}