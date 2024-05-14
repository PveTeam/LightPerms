using System.Reflection;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Cube;
using Torch.Managers.PatchManager;
using Torch.Utils;

namespace FloatingObjects.Patches;

internal static class EjectPatch
{
    [ReflectedMethodInfo(typeof(MyShipConnector), "TryThrowOutItem")]
    private static readonly MethodInfo TryThrowOutMethod = null!;

    public static void Patch(PatchContext ctx)
    {
        ctx.GetPattern(TryThrowOutMethod).Prefixes.Add(new Func<MyShipConnector, bool>(Prefix).Method);
    }

    private static bool Prefix(MyShipConnector __instance)
    {
        var inventory = __instance.GetInventory();
        if (inventory.GetItemAt(0) != null) inventory.RemoveItemsAt(0);
        return false;
    }
}