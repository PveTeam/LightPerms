using System.Reflection;
using me.lucko.luckperms.common.api;
using Torch.Managers.PatchManager;
using Torch.Utils;

namespace LuckPerms.Torch.Impl.CompatPatches;

[PatchShim]
public static class LoaderCheckPatch
{
    // TODO sort this out when i get to plugin api stuff

    [ReflectedMethodInfo(typeof(LuckPermsApiProvider), nameof(LuckPermsApiProvider.ensureApiWasLoadedByPlugin))]
    private static MethodInfo TargetMethod = null!;

    [ReflectedMethodInfo(typeof(LoaderCheckPatch), nameof(Prefix))]
    private static MethodInfo PrefixMethod = null!;

    public static void Patch(PatchContext context)
    {
        context.GetPattern(TargetMethod).Prefixes.Add(PrefixMethod);
    }

    private static bool Prefix() => false;
}