using me.lucko.luckperms.common.command.utils;
using me.lucko.luckperms.common.plugin;
using Torch.API.Managers;
using Torch.Commands;
using Torch.Managers;
using Torch.Server.Managers;
using VRage.Game.ModAPI;

namespace LuckPerms.Torch.Impl;

public class LpCommandManager(LuckPermsPlugin plugin, LpSenderFactory senderFactory) : me.lucko.luckperms.common.command.CommandManager(plugin), IManager
{
    private static readonly string[] Aliases = { "luckperms", "lp", "perm", "perms", "permission", "permissions" };
    
    [Manager.Dependency]
    private CommandManager _commandManager = null!;

    [Manager.Dependency]
    private MultiplayerManagerDedicated _multiplayerManager = null!;
    
    public void Attach()
    {
        foreach (var alias in Aliases)
        {
            _commandManager.Commands.AddCommand(new(alias, "LuckPerms commands", (ctx, _) => Execute(alias, ctx),
                ((LpTorchBootstrap)getPlugin().getBootstrap()).GetTorchPlugin(), MyPromoteLevel.None));
        }
    }

    private void Execute(string prefix, CommandContext context)
    {
        executeCommand(
            senderFactory.Wrap(context.SentBySelf
                ? context.Torch
                : _multiplayerManager.Players[context.Player.SteamUserId], context), prefix,
            ArgumentTokenizer.EXECUTE.tokenizeInput(context.RawArgs));
    }

    public void Detach()
    {
    }
}