using System.Reflection;
using System.Reflection.Emit;
using Sandbox.Definitions;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Blocks;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.World;
using Torch.Managers.PatchManager;
using Torch.Managers.PatchManager.MSIL;
using Torch.Utils;
using VRage.Game;
using ZLimits.Extensions;
using ZLimits.Managers;

namespace ZLimits.Patches;

[PatchShim]
public static class BlockLimitHook
{
    [ReflectedMethod]
    private static readonly Func<MyCubeGrid, long, int, int, string, bool> IsWithinWorldLimits = null!;

    [ReflectedMethodInfo(typeof(MySession), nameof(MySession.Load))]
    private static readonly MethodInfo SessionLoadMethod = null!;

    [ReflectedMethodInfo(typeof(BlockLimitHook), nameof(LoadPrefix))]
    private static readonly MethodInfo SessionLoadPrefixMethod = null!;
    
    [ReflectedMethodInfo(typeof(MyCubeGrid), "BuildBlockRequestInternal")]
    private static readonly MethodInfo BuildBlockGridMethod = null!;
    
    [ReflectedMethodInfo(typeof(BlockLimitHook), nameof(BuildBlockRequestTranspiler))]
    private static readonly MethodInfo BuildBlockGridTranspilerMethod = null!;
    
    [ReflectedMethodInfo(typeof(MyProjectorBase), "BuildInternal")]
    private static readonly MethodInfo BuildBlockProjectorMethod = null!;
    
    [ReflectedMethodInfo(typeof(BlockLimitHook), nameof(BuildBlockInternalTranspiler))]
    private static readonly MethodInfo BuildBlockProjectorTranspilerMethod = null!;

    [ReflectedMethodInfo(typeof(MyCubeGrid), "IsWithinWorldLimits")]
    private static readonly MethodInfo IsWithinWorldLimitsMethod = null!;
    
    [ReflectedMethodInfo(typeof(BlockLimitHook), nameof(IsWithinWorldLimitsSuffix))]
    private static readonly MethodInfo IsWithinWorldLimitsSuffixMethod = null!;
    
    [ReflectedMethodInfo(typeof(MySlimBlock), "AddAuthorship")]
    private static readonly MethodInfo AddAuthorshipMethod = null!;
    
    [ReflectedMethodInfo(typeof(BlockLimitHook), nameof(AddAuthorshipSuffix))]
    private static readonly MethodInfo AddAuthorshipSuffixMethod = null!;
    
    public static void Patch(PatchContext ctx)
    {
        ctx.GetPattern(SessionLoadMethod).Prefixes.Add(SessionLoadPrefixMethod);
        ctx.GetPattern(BuildBlockGridMethod).Transpilers.Add(BuildBlockGridTranspilerMethod);
        ctx.GetPattern(BuildBlockProjectorMethod).Transpilers.Add(BuildBlockProjectorTranspilerMethod);
        ctx.GetPattern(IsWithinWorldLimitsMethod).Suffixes.Add(IsWithinWorldLimitsSuffixMethod);
        ctx.GetPattern(AddAuthorshipMethod).Suffixes.Add(AddAuthorshipSuffixMethod);
    }
    
    public static void LoadPrefix(MyObjectBuilder_Checkpoint checkpoint)
    {
        LimitsManager.Instance.LoadSessionLimits(checkpoint);
    }

    public static IEnumerable<MsilInstruction> BuildBlockRequestTranspiler(IEnumerable<MsilInstruction> ins)
    {
        var list = ins.ToList();
        for (var i = 0; i < list.Count; i++)
        {
            var instruction = list[i];
            if (instruction.OpCode == OpCodes.Callvirt &&
                instruction.Operand is MsilOperandInline<MethodInfo> operand &&
                operand.Value.Name == "TryGetCubeBlockDefinition")
            {
                yield return instruction; //call virt
                yield return list[++i]; //pop

                yield return new(OpCodes.Ldarg_0);
                yield return new MsilInstruction(OpCodes.Ldarg_S).InlineValue(6);
                yield return new(OpCodes.Ldc_I4_1);
                yield return new(OpCodes.Dup);
                yield return new MsilInstruction(OpCodes.Ldfld).InlineValue(
                    typeof(MyCubeBlockDefinition).GetField(nameof(MyCubeBlockDefinition.BlockPairName)));

                yield return new MsilInstruction(OpCodes.Call).InlineValue(
                    typeof(MyCubeGrid).GetMethod("IsWithinWorldLimits", BindingFlags.NonPublic | BindingFlags.Instance));
                var label = new MsilLabel();
                yield return new MsilInstruction(OpCodes.Brtrue_S).InlineTarget(label);
                // yield return new(OpCodes.Ldarg_0);
                // yield return new MsilInstruction(OpCodes.Call).InlineValue(
                //     new Action<MyCubeGrid>(HandleLimitsReached).Method);
                yield return new(OpCodes.Ret);
                yield return list[++i].LabelWith(label);
                continue;
            }
            yield return instruction;
        }
    }

    public static IEnumerable<MsilInstruction> BuildBlockInternalTranspiler(IEnumerable<MsilInstruction> ins)
    {
        var list = ins.ToList();
        for (var i = 0; i < list.Count; i++)
        {
            var instruction = list[i];
            if (instruction.OpCode == OpCodes.Stloc_1)
            {
                yield return instruction;
                yield return new(OpCodes.Ldarg_0);
                yield return new(OpCodes.Ldloc_1);
                yield return new(OpCodes.Ldarg_2);
                yield return new MsilInstruction(OpCodes.Call).InlineValue(
                    new Func<MyProjectorBase, MySlimBlock, long, bool>(IsInProjectorLimits).Method);
                var label = new MsilLabel();
                yield return new MsilInstruction(OpCodes.Brtrue_S).InlineTarget(label);
                yield return new(OpCodes.Ret);
                yield return list[++i].LabelWith(label);
                continue;
            }

            yield return instruction;
        }
    }

    public static void IsWithinWorldLimitsSuffix(MyCubeGrid __instance, ref bool __result, string name, int blocksToBuild, int pcu, long ownerID)
    {
        var limits = LimitsManager.Instance.GetBlockLimits(__instance);
        var result = LimitsManager.Instance.GetLimitsGroups(name).All(info =>
            !limits.BlockTypeBuilt.TryGetValue(name, out var limitData) || 
            limitData.BlocksBuilt + blocksToBuild <= info.Max);
            
        if (LimitsManager.Instance.Config.LimitGridsPcu)
        {
            result &= limits.PCUBuilt + pcu <= (__instance.GridSizeEnum == MyCubeSize.Large
                ? LimitsManager.Instance.Config.LargeGridPcu
                : LimitsManager.Instance.Config.SmallGridPcu);
        }
        
        if (!result)
            LimitsManager.NotifyLimitsReached(ownerID);
        
        __result &= result;
    }

    public static void AddAuthorshipSuffix(MySlimBlock __instance)
    {
        if (LimitsManager.Instance.GetLimitsGroup(__instance.BlockDefinition.BlockPairName, "PerGrid") is null)
            return;
        
        var limits = LimitsManager.Instance.GetBlockLimits(__instance.CubeGrid);
        limits.IncreaseBlocksBuilt(__instance.BlockDefinition.BlockPairName, __instance.BlockDefinition.PCU, __instance.CubeGrid);
    }
    
    private static bool IsInProjectorLimits(MyProjectorBase projector, MySlimBlock block, long owner)
    {
        return IsWithinWorldLimits(projector.CubeGrid, owner, 1, block.GetPcu(), block.BlockDefinition.BlockPairName);
    }
}