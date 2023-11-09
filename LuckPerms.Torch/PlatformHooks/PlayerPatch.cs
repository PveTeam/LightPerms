using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using LuckPerms.Torch.PlatformApi;
using Torch.Managers;
using Torch.Managers.PatchManager;
using Torch.Managers.PatchManager.MSIL;
using Torch.Utils;

namespace LuckPerms.Torch.PlatformHooks;

internal static class PlayerPatch
{
    [ReflectedMethodInfo(typeof(MultiplayerManagerBase), "RaiseClientJoined")]
    private static MethodInfo Target = null!;

    [ReflectedMethodInfo(typeof(PlayerPatch), nameof(Transpiler))]
    private static MethodInfo TranspilerMethod = null!;
    
    public static void Patch(PatchContext context)
    {
        context.GetPattern(Target).Transpilers.Add(TranspilerMethod);
    }

    private static IEnumerable<MsilInstruction> Transpiler(IEnumerable<MsilInstruction> instructions)
    {
        var list = instructions.ToList();

        var newIndex = list.FindIndex(b => b.OpCode == OpCodes.Newobj);

        list[newIndex] = list[newIndex].InlineValue(typeof(LpPlayerModel).GetConstructors(BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.Instance)[0]);

        return list;
    }
}