using System.Reflection;
using System.Reflection.Emit;
using Sandbox.Engine.Utils;
using Sandbox.Game.Entities;
using Torch.Managers.PatchManager;
using Torch.Managers.PatchManager.MSIL;
using Torch.Utils;
using VRage.Game;

namespace FloatingObjects.Patches;

internal static class AmmoDetonationPatch
{
    [ReflectedMethodInfo(typeof(MyCubeBlock), nameof(MyCubeBlock.Init), 
        Parameters = [typeof(MyObjectBuilder_CubeBlock), typeof(MyCubeGrid)])]
    private static readonly MethodInfo CubeBlockInitMethod = null!;
    
    [ReflectedMethodInfo(typeof(MyCubeBlock), nameof(MyCubeBlock.OnDestroy))]
    private static readonly MethodInfo CubeBlockDestroyMethod = null!;
    
    [ReflectedMethodInfo(typeof(MyCargoContainerInventoryBagEntity), nameof(MyCargoContainerInventoryBagEntity.DoDamage))]
    private static readonly MethodInfo BagDoDamageMethod = null!;

    [ReflectedMethodInfo(typeof(MyCargoContainerInventoryBagEntity), "InitDetonationData")]
    private static readonly MethodInfo BagInitDetonationDataMethod = null!;

    [ReflectedMethodInfo(typeof(MyCargoContainerInventoryBagEntity), "OnInventoryComponentAdded")]
    private static readonly MethodInfo BagOnInventoryComponentAddedMethod = null!;

    [ReflectedFieldInfo(typeof(MyFakes), nameof(MyFakes.ENABLE_AMMO_DETONATION))]
    private static readonly FieldInfo EnableDetonationField = null!;
    
    public static void Patch(PatchContext ctx)
    {
        var transpilerDel = Transpiler;
        var transpiler = transpilerDel.Method;
        
        ctx.GetPattern(CubeBlockInitMethod).Transpilers.Add(transpiler);
        ctx.GetPattern(CubeBlockDestroyMethod).Transpilers.Add(transpiler);
        ctx.GetPattern(BagDoDamageMethod).Transpilers.Add(transpiler);
        ctx.GetPattern(BagInitDetonationDataMethod).Transpilers.Add(transpiler);
        ctx.GetPattern(BagOnInventoryComponentAddedMethod).Transpilers.Add(transpiler);
    }

    private static IEnumerable<MsilInstruction> Transpiler(IEnumerable<MsilInstruction> instructions)
    {
        foreach (var instruction in instructions)
        {
            if (instruction.OpCode == OpCodes.Ldsfld &&
                instruction.Operand is MsilOperandInline<FieldInfo> { Value: { } operand } &&
                operand == EnableDetonationField)
            {
                yield return new MsilInstruction(OpCodes.Ldc_I4_0);
                continue;
            }
            
            yield return instruction;
        }
    }
}