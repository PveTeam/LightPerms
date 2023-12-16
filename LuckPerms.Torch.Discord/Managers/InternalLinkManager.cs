using System.Collections.Concurrent;
using LuckPerms.Torch.Api.Managers;
using LuckPerms.Torch.Discord.Abstractions;
using LuckPerms.Torch.Utils.Extensions;
using net.dv8tion.jda.api.events.interaction.command;
using net.dv8tion.jda.api.hooks;
using net.dv8tion.jda.api.interactions.commands;
using net.dv8tion.jda.api.interactions.commands.build;
using net.dv8tion.jda.@internal.interactions.command;
using net.luckperms.api.model.user;
using net.luckperms.api.node;
using net.luckperms.api.node.types;
using NLog;
using Sandbox.Game.World;
using Torch.API;
using Torch.API.Plugins;
using Torch.Commands;
using Torch.Managers;
using Torch.Server.Managers;
using VRage.Game.ModAPI;
using VRage.Library.Utils;

namespace LuckPerms.Torch.Discord.Managers;

public class InternalLinkManager(ITorchPlugin plugin) : ListenerAdapter, ILinkManager
{
    private static readonly ILogger Log = LogManager.GetCurrentClassLogger();

    private const string DiscordIdNode = "luckperms.discord.userid";

    [Manager.Dependency]
    private readonly CommandManager _commandManager = null!;
    
    [Manager.Dependency]
    private readonly DiscordManager _discordManager = null!;
    
    [Manager.Dependency]
    private readonly ILuckPermsPlatformManager _luckPermsPlatformManager = null!;

    [Manager.Dependency]
    private readonly MultiplayerManagerDedicated _multiplayerManager = null!;

    private readonly ConcurrentDictionary<int, ulong> _pendingLinks = [];
    
    public void Attach()
    {
        _commandManager.Commands.AddCommand(new("link", "Links your Discord account to your Steam account", Link,
            plugin, MyPromoteLevel.None));

        _discordManager.Client.updateCommands().addCommands(
            Commands.slash("link", "Links your Discord account to your Steam account")
                .setGuildOnly(true)
                .addOption(OptionType.INTEGER, "code", "The link code", true)
        ).queue();
    }

    public override void onSlashCommandInteraction(SlashCommandInteractionEvent args)
    {
        var name = CommandInteractionPayloadMixin.__DefaultMethods.getName((CommandInteractionPayloadMixin)args.getInteraction());
        if (name != "link")
            return;
        
        var code = CommandInteractionPayloadMixin.__DefaultMethods
            .getOptions((CommandInteractionPayloadMixin)args.getInteraction()).iterator().AsEnumerable<OptionMapping>()
            .First(b => b.getName() == "code").getAsInt();

        if (!_pendingLinks.TryRemove(code, out var steamId))
        {
            args.reply("Invalid code").setEphemeral(true).queue();
            return;
        }

        var discordId = args.getUser().getId();

        var consumer = (User user) =>
        {
            user.data().add(MetaNode.builder(DiscordIdNode, discordId).build());
        };

        _luckPermsPlatformManager.Api.getUserManager().modifyUser(steamId.GetUuid(), consumer.ToConsumer());
        
        if (_multiplayerManager.Players.TryGetValue(steamId, out var player))
            _luckPermsPlatformManager.Api.getContextManager().signalContextUpdate(player);

        args.reply("Successfully linked").setEphemeral(true).queue();
    }

    private void Link(CommandContext context, object[] arguments)
    {
        if (context.Player is not MyPlayer player)
        {
            context.Respond("You must be in the game to link your Discord account");
            return;
        }

        if (IsSteamIdLinked(_multiplayerManager.Players[player.Id.SteamId]))
        {
            context.Respond("You are already linked");
            return;
        }

        var code = MyRandom.Instance.Next(1000, 99999);

        context.Respond(_pendingLinks.TryAdd(code, player.Id.SteamId)
            ? $"Please enter command `/link {code}` on the discord server to link your Discord and Steam accounts"
            : "An error occurred while trying to link your Discord and Steam accounts");
    }

    public void Detach()
    {
    }

    public bool IsSteamIdLinked(IPlayer player)
    {
        return _luckPermsPlatformManager.Api.getPlayerAdapter(typeof(IPlayer)).getUser(player).getNodes(NodeType.META)
            .iterator().AsEnumerable<MetaNode>().Any(b => b.getMetaKey() == DiscordIdNode);
    }

    public long? ResolveDiscordId(IPlayer player)
    {
        var idValue = _luckPermsPlatformManager.Api.getPlayerAdapter(typeof(IPlayer)).getUser(player)
            .getNodes(NodeType.META)
            .iterator().AsEnumerable<MetaNode>().FirstOrDefault(b => b.getMetaKey() == DiscordIdNode)?.getMetaValue();
        return idValue is null ? null : long.Parse(idValue);
    }
}