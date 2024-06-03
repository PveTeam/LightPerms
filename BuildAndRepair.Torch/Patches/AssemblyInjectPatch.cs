using System.Reflection;
using Sandbox.Game.World;
using Torch.Managers.PatchManager;
using Torch.Utils;
using VRage.Game;
using VRage.Utils;

namespace BuildAndRepair.Torch.Patches;

[PatchShim]
internal static class AssemblyInjectPatch
{
    [ReflectedMethodInfo(typeof(MyScriptManager), "LoadScripts")]
    private static readonly MethodInfo LoadScriptsMethod = null!;

    [ReflectedMethodInfo(typeof(AssemblyInjectPatch), nameof(Prefix))]
    private static readonly MethodInfo PrefixMethod = null!;

    public static void Patch(PatchContext ctx)
    {
        ctx.GetPattern(LoadScriptsMethod).Prefixes.Add(PrefixMethod);
    }

    private static bool Prefix(MyScriptManager __instance, MyModContext? mod)
    {
        if (mod?.ModItem.PublishedFileId != Plugin.ModId)
            return true;

        __instance.AddAssembly(mod, MyStringId.GetOrCompute("BuildAndRepair.Torch"), typeof(Plugin).Assembly);

        return false;
    }
}