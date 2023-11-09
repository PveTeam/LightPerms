using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using LuckPerms.Torch.Extensions;
using LuckPerms.Torch.PlatformApi;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.World;
using Torch.API;
using Torch.API.Managers;
using Torch.Commands;
using Torch.Managers;
using Torch.Managers.PatchManager;
using Torch.Managers.PatchManager.MSIL;
using Torch.Mod;
using Torch.Mod.Messages;
using Torch.Server.Managers;
using Torch.Utils;

namespace LuckPerms.Torch.PlatformHooks;

internal static class CommandPermissionsPatch
{
    [ReflectedMethodInfo(typeof(CommandManager), nameof(CommandManager.HasPermission))]
    private static MethodInfo HasPermissionMethod = null!;
    
    [ReflectedMethodInfo(typeof(TorchCommands), nameof(TorchCommands.Help))]
    private static MethodInfo HelpMethod = null!;
    
    [ReflectedMethodInfo(typeof(TorchCommands), nameof(TorchCommands.LongHelp))]
    private static MethodInfo LongHelpMethod = null!;

    [ReflectedMethodInfo(typeof(CommandPermissionsPatch), nameof(Prefix))]
    private static MethodInfo PrefixMethod = null!;
    
    [ReflectedMethodInfo(typeof(CommandPermissionsPatch), nameof(PrefixHelp))]
    private static MethodInfo PrefixHelpMethod = null!;
    
    [ReflectedMethodInfo(typeof(CommandPermissionsPatch), nameof(PrefixLongHelp))]
    private static MethodInfo PrefixLongHelpMethod = null!;
    
    [ReflectedMethodInfo(typeof(CommandPermissionsPatch), nameof(Transpiler))]
    private static MethodInfo TranspilerMethod = null!;

    [ReflectedGetter(Name = "Torch")]
    private static Func<Manager, ITorchBase> TorchGetter = null!;

    public static void Patch(PatchContext context)
    {
        context.GetPattern(HasPermissionMethod).Prefixes.Add(PrefixMethod);
        context.GetPattern(typeof(CommandManager).GetMethod(nameof(CommandManager.HandleCommand), 
            new []{typeof(string), typeof(ulong), typeof(bool).MakeByRefType(), typeof(bool)})).Transpilers.Add(TranspilerMethod);

        context.GetPattern(HelpMethod).Prefixes.Add(PrefixHelpMethod);
        context.GetPattern(LongHelpMethod).Prefixes.Add(PrefixLongHelpMethod);
    }

    private static bool PrefixHelp(CommandModule __instance)
    {
        var commandManager = __instance.Context.Torch.CurrentSession.Managers.GetManager<CommandManager>();
        commandManager.Commands.GetNode(__instance.Context.Args, out var node);

        if (node is null)
        {
            __instance.Context.Respond(
                $"Command not found. Use the {commandManager.Prefix}longhelp command for a full list of commands.");
            return false;
        }
        
        var command = node.Command;
        var children = node.Subcommands.Where(e => __instance.Context.Player == null ||
                                                   !e.Value.IsCommand ||
                                                   !commandManager.HasPermission(__instance.Context.Player.SteamUserId, e.Value.Command))
            .Select(x => x.Key);

        var sb = new StringBuilder();

        if (command is not null)
        {
            if (__instance.Context.Player is not null && !commandManager.HasPermission(__instance.Context.Player.SteamUserId, command))
            {
                __instance.Context.Respond("You are not allowed to use this command.");
                return false;
            }
            
            sb.AppendLine($"Syntax: {command.SyntaxHelp}");
            sb.Append(command.HelpText);
        }

        if (node.Subcommands.Count > 0)
            sb.Append($"\nSubcommands: {string.Join(", ", children)}");

        __instance.Context.Respond(sb.ToString());
        return false;
    }

    private static bool PrefixLongHelp(CommandModule __instance)
    {
        var commandManager = __instance.Context.Torch.CurrentSession.Managers.GetManager<CommandManager>();
        commandManager.Commands.GetNode(__instance.Context.Args, out var node);

        if (node != null)
        {
            var command = node.Command;
            var children = node.Subcommands.Where(e => __instance.Context.Player == null ||
                                                       !e.Value.IsCommand ||
                                                       !commandManager.HasPermission(__instance.Context.Player.SteamUserId, e.Value.Command));

            var sb = new StringBuilder();

            if (command != null && (__instance.Context.Player is null || commandManager.HasPermission(__instance.Context.Player.SteamUserId, command)))
            {
                sb.AppendLine($"Syntax: {command.SyntaxHelp}");
                sb.Append(command.HelpText);
            }

            if (node.Subcommands.Count > 0)
                sb.Append($"\nSubcommands: {string.Join(", ", children)}");

            __instance.Context.Respond(sb.ToString());
        }
        else
        {
            var sb = new StringBuilder();
            foreach (var command in commandManager.Commands.WalkTree())
            {
                if (command.IsCommand && (__instance.Context.Player is null || commandManager.HasPermission(__instance.Context.Player.SteamUserId, command.Command)))
                    sb.AppendLine($"{command.Command.SyntaxHelp}\n    {command.Command.HelpText}");
            }

            if (!__instance.Context.SentBySelf)
            {
                var m = new DialogMessage("Torch Help", "Available commands:", sb.ToString());
                ModCommunication.SendMessageTo(m, __instance.Context.Player!.SteamUserId);
            }
            else
                __instance.Context.Respond($"Available commands: {sb}");
        }
        return false;
    }

    private static bool Prefix(Manager __instance, ulong steamId, Command command, ref bool __result)
    {
        if (steamId == Sync.MyId)
        {
            __result = true;
            return false;
        }

        var player = (LpPlayerModel)TorchGetter(__instance).CurrentSession.Managers.GetManager<MultiplayerManagerDedicated>()
            .Players[steamId];

        var result = player.HasPermission(command.GetPermissionString());
        
        __result = result.asBoolean();
        return false;
    }

    private static IEnumerable<MsilInstruction> Transpiler(IEnumerable<MsilInstruction> ins)
    {
        foreach (var instruction in ins)
        {
            if (instruction.OpCode == OpCodes.Ldstr &&
                instruction.Operand is MsilOperandInline.MsilOperandString {Value: "You need to be a {0} or higher to use that command."})
                yield return instruction.InlineValue("You don't have permission to use that command.");
            else
                yield return instruction;
        }
    }
}