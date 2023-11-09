using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Torch.Commands;
using Torch.Managers.PatchManager;
using Torch.Managers.PatchManager.MSIL;
using Torch.Utils;

namespace LuckPerms.Torch.PlatformHooks;

internal static class CommandPrefixPatch
{
    private const char DefaultPrefix = '!';
    private const char SlashPrefix = '/';

    [ReflectedMethodInfo(typeof(CommandPrefixPatch), nameof(CheckPrefix))]
    private static MethodInfo CheckPrefixMethod = null!;
    
    [ReflectedMethodInfo(typeof(CommandPrefixPatch), nameof(IsCommandPrefix))]
    private static MethodInfo IsCommandPrefixMethod = null!;
    
    [ReflectedMethodInfo(typeof(CommandPrefixPatch), nameof(HandleCommandTranspiler))]
    private static MethodInfo HandleCommandTranspilerMethod = null!;

    [ReflectedMethodInfo(typeof(CommandManager), nameof(CommandManager.IsCommand))]
    private static MethodInfo IsCommandMethod = null!;
    
    public static void Patch(PatchContext context)
    {
        context.GetPattern(IsCommandMethod).Prefixes.Add(IsCommandPrefixMethod);
        
        var handleCommandMethod = typeof(CommandManager).GetMethods().Single(b =>
            b.Name == nameof(CommandManager.HandleCommand) && b.GetParameters().Length == 4);
        context.GetPattern(handleCommandMethod).Transpilers.Add(HandleCommandTranspilerMethod);
    }

    private static bool IsCommandPrefix(string command, out bool __result)
    {
        __result = !string.IsNullOrEmpty(command) && command[0] is DefaultPrefix or SlashPrefix;
        return false;
    }

    private static IEnumerable<MsilInstruction> HandleCommandTranspiler(IEnumerable<MsilInstruction> instructions)
    {
        var list = instructions.ToList();

        var index = list.FindIndex(b =>
            b.OpCode == OpCodes.Call && b.Operand is MsilOperandInline.MsilOperandReflected<MethodBase> { Value.Name: "get_Prefix" });

        list[index - 1] = new(OpCodes.Nop);
        list[index] = list[index].InlineValue(CheckPrefixMethod);
        list[index + 1] = new MsilInstruction(OpCodes.Brtrue_S).InlineTarget(list[index + 3].Labels.First());

        return list;
    }

    private static bool CheckPrefix(char prefix) => prefix is DefaultPrefix or SlashPrefix;
}