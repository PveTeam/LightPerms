using System.Reflection;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.World;
using Torch.Managers.PatchManager;
using Torch.Utils;
using ZLimits.Managers;

namespace ZLimits.Patches;

[PatchShim]
public static class IdentityBlockLimitHook
{
    [ReflectedMethodInfo(typeof(IdentityBlockLimitHook), nameof(BlockLimitsGetterPrefix))]
    private static readonly MethodInfo PrefixMethod = null!;
    
    [ReflectedGetter(Name = "m_blockLimits")]
    private static readonly Func<MyIdentity, MyBlockLimits> BlockLimitsGetter = null!;
    
    public static void Patch(PatchContext ctx)
    {
        ctx.GetPattern(typeof(MyIdentity).GetProperty(nameof(MyIdentity.BlockLimits))!.GetMethod)
            .Prefixes.Add(PrefixMethod);
    }
    
    private static bool BlockLimitsGetterPrefix(MyIdentity __instance, ref MyBlockLimits __result)
    {
        if (!Sync.Players.TryGetPlayerId(__instance.IdentityId, out _))
            return true;
        
        __result = LimitsManager.BlockLimitsMap.GetOrAdd(__instance, BlockLimitsGetter);
        return false;
    }
}