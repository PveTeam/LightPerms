using System.Reflection;
using System.Reflection.Emit;
using Sandbox.Game.Entities.Blocks;
using Torch.Managers.PatchManager;
using Torch.Managers.PatchManager.MSIL;
using Torch.Utils;

namespace ZLimits.Patches;

internal static class ProjectionPcuPatch
{
    [ReflectedMethodInfo(typeof(ProjectionPcuPatch), nameof(Transpiler))]
    private static readonly MethodInfo TranspilerMethod = null!;
    
    [ReflectedMethodInfo(typeof(MyProjectorBase), "InitializeClipboard")]
    private static readonly MethodInfo InitializeProjectionMethod = null!;
    
    [ReflectedMethodInfo(typeof(MyProjectorBase), "RemoveProjection")]
    private static readonly MethodInfo RemoveProjectionMethod = null!;
    
    public static void Patch(PatchContext ctx)
    {
        ctx.GetPattern(InitializeProjectionMethod).Transpilers.Add(TranspilerMethod);
        ctx.GetPattern(RemoveProjectionMethod).Transpilers.Add(TranspilerMethod);
    }
    
    private static IEnumerable<MsilInstruction> Transpiler(IEnumerable<MsilInstruction> instructions)
    {
        var ins = instructions.ToList();

        var index = ins.FindIndex(b => b.OpCode == OpCodes.Stloc_1) + 1; // index of load local 1 (if (identity != null) statement)

        ins[index] = new(OpCodes.Ldc_I4_0); // always branch away (if (false) statement)

        return ins;
    }
}