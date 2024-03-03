using IKVM.Attributes;
using LuckPerms.Torch.Discord.Abstractions;
using LuckPerms.Torch.Utils.Extensions;
using net.dv8tion.jda.api.events.interaction.command;
using net.dv8tion.jda.api.hooks;
using net.dv8tion.jda.api.interactions.commands;
using net.dv8tion.jda.api.interactions.commands.build;
using net.dv8tion.jda.@internal.interactions.command;
using Sandbox.Game.Multiplayer;
using Torch.API.Managers;
using Torch.Commands;
using Torch.Managers;
using Command = net.dv8tion.jda.api.interactions.commands.Command;

namespace LuckPerms.Torch.Discord.Managers;

public class CommandExecutorManager(string name) : ListenerAdapter, IManager
{
    [Manager.Dependency]
    private readonly CommandManager _commandManager = null!;

    [Manager.Dependency]
    private readonly DiscordManager _discordManager = null!;

    public void Attach()
    {
        var guild = _discordManager.Client.getGuildById(_discordManager.MainGuildId);

        guild.updateCommands().addCommands(
            Commands.slash(name, "Evaluates command directly on server")
                .addOption(OptionType.STRING, "command", "Command to evaulate", true, true)
                .addOption(OptionType.STRING, "args", "Arguments for command", false)
        ).queue();
    }

    public void Detach()
    {
    }

    public override void onSlashCommandInteraction(SlashCommandInteractionEvent e)
    {
        if (CommandInteractionPayloadMixin.__DefaultMethods.getName((CommandInteractionPayloadMixin)e.getInteraction()) != name)
        {
            return;
        }

        var commandName = CommandInteractionPayloadMixin.__DefaultMethods
            .getOptions((CommandInteractionPayloadMixin)e.getInteraction()).iterator().AsEnumerable<OptionMapping>()
            .First(b => b.getName() == "command").getAsString();

        var args = CommandInteractionPayloadMixin.__DefaultMethods
            .getOptions((CommandInteractionPayloadMixin)e.getInteraction()).iterator().AsEnumerable<OptionMapping>()
            .FirstOrDefault(b => b.getName() == "args")?.getAsString() ?? string.Empty;

        e.deferReply(true).queue();

        var hook = e.getHook();
        var content = string.Empty;

        _commandManager.HandleCommandFromServer($"!{commandName} {args}", 
            msg => hook.editOriginal(content += msg.Message).queue());
    }

    public override void onCommandAutoCompleteInteraction(CommandAutoCompleteInteractionEvent e)
    {
        if (e.getName() != name || e.getFocusedOption().getName() != "command")
        {
            return;
        }

        CommandTree.CommandNode? node = null;
        string? filter = null;
        foreach (var c in e.getFocusedOption().getValue().Split([' '], StringSplitOptions.RemoveEmptyEntries))
        {
            var prevNode = node;
            var success = node is null ? _commandManager.Commands.Root.TryGetValue(c, out node) : node.Subcommands.TryGetValue(c, out node);

            if (!success)
            {
                node = prevNode;
                filter = c;
                break;
            }
        }

        var choices = (node?.Subcommands ?? _commandManager.Commands.Root)
            .Select(kv => (kv, TestOption(kv.Key, filter ?? "")))
            .OrderBy(b => b.Item2)
            .Take(10)
            .Where(b => b.Item2 >= 0)
            .Select(b => b.kv)
            .Select(kv => new Command.Choice(string.Join(" ", kv.Value.GetPath()), kv.Key))
            .ToCollection();

        e.replyChoices(choices).queue();
    }

    private static int TestOption(string option, string input)
    {
        if (input.Length == 0)
            return 0;

        if (option.Equals(input, StringComparison.OrdinalIgnoreCase))
            return 0;
        if (option.StartsWith(input, StringComparison.OrdinalIgnoreCase))
            return 1;
        if (option.Contains(input, StringComparison.OrdinalIgnoreCase))
            return 2;

        return -1;
    }
}
