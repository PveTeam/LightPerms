using System.Reflection;
using System.Reflection.Emit;
using System.Text;
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
using Torch.Utils;
namespace LightPerms.TorchCommands;

[PatchShim]
public static class PermissionPatch
{
    [ReflectedMethodInfo(typeof(CommandManager), nameof(CommandManager.HasPermission))]
    private static readonly MethodInfo HasPermissionMethod = null!;
    
    [ReflectedMethodInfo(typeof(Torch.Commands.TorchCommands), nameof(Torch.Commands.TorchCommands.Help))]
    private static readonly MethodInfo HelpMethod = null!;
    
    [ReflectedMethodInfo(typeof(Torch.Commands.TorchCommands), nameof(Torch.Commands.TorchCommands.LongHelp))]
    private static readonly MethodInfo LongHelpMethod = null!;

    [ReflectedMethodInfo(typeof(PermissionPatch), nameof(Prefix))]
    private static readonly MethodInfo PrefixMethod = null!;
    
    [ReflectedMethodInfo(typeof(PermissionPatch), nameof(PrefixHelp))]
    private static readonly MethodInfo PrefixHelpMethod = null!;
    
    [ReflectedMethodInfo(typeof(PermissionPatch), nameof(PrefixLongHelp))]
    private static readonly MethodInfo PrefixLongHelpMethod = null!;
    
    [ReflectedMethodInfo(typeof(PermissionPatch), nameof(Transpiler))]
    private static readonly MethodInfo TranspilerMethod = null!;

    [ReflectedGetter(Name = "Torch")]
    private static readonly Func<Manager, ITorchBase> TorchGetter = null!;

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
                __instance.Context.Respond("You are not authorized to use this command.");
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
        __result = steamId == Sync.MyId || 
                   MySession.Static.IsUserAdmin(steamId) || 
                   TorchGetter(__instance).Managers.GetManager<IPermissionsManager>().HasPermission(steamId, command.GetPermissionString());
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
