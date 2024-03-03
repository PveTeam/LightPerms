using System.Text.RegularExpressions;
using LuckPerms.Torch.Discord.Config;
using net.dv8tion.jda.api.entities;
using net.dv8tion.jda.api.events.message;
using net.dv8tion.jda.api.hooks;
using Sandbox.Game.Gui;
using Torch.API;
using Torch.API.Managers;
using Torch.Managers;
using Torch.Managers.ChatManager;
using Torch.Server.Managers;
using Torch.Utils;
using VRage.Game;

namespace LuckPerms.Torch.Discord.Managers;

public class GlobalChatMirrorManager(GlobalChatMirroringConfig config, ITorchBase torch) : ListenerAdapter, IManager
{
    [Manager.Dependency]
    private readonly ChatManagerServer _chatManager = null!;

    [Manager.Dependency]
    private readonly DiscordManager _discordManager = null!;

    [Manager.Dependency]
    private readonly MultiplayerManagerDedicated _multiplayerManager = null!;

    private readonly Regex _mentionRegex = new(@"@(?<mention>\S*)");
    private readonly Regex _rawMentionRegex = new(@"<@!*&*[0-9]+>");

    public void Attach()
    {
        _chatManager.MessageProcessing += OnMessageProcessing;
        _chatManager.MessageSending += OnMessageSending;
    }

    public void Detach()
    {
        _chatManager.MessageProcessing -= OnMessageProcessing;
        _chatManager.MessageSending -= OnMessageSending;
    }

    public override void onMessageReceived(MessageReceivedEvent e)
    {
        if (string.IsNullOrEmpty(e.getMessage().getContentRaw()) ||
             e.isWebhookMessage() || !e.isFromGuild() || e.getGuildChannel().getIdLong() != config.ChannelId)
            return;

        if (e.getAuthor().getIdLong() == _discordManager.Client.getSelfUser().getIdLong())
            return;

        var author = string.Format(config.IngameAuthorFormat, e.getAuthor().getEffectiveName());

        _chatManager.SendMessageAsOther(author, e.getMessage().getContentRaw(), ColorUtils.TranslateColor(config.IngameAuthorColor));
    }

    private void OnMessageSending(string msg, ref bool consumed)
    {
        MirrorMessage(torch.Config.ChatName, msg);
    }

    private void OnMessageProcessing(TorchChatMessage msg, ref bool consumed)
    {
        if (msg.Channel is not (ChatChannel.Global or ChatChannel.GlobalScripted))
            return;

        var authorName = msg.AuthorSteamId.HasValue ? _multiplayerManager.GetSteamUsername(msg.AuthorSteamId.Value) : msg.Author;

        MirrorMessage(authorName, msg.Message);
    }

    private void MirrorMessage(string author, string message)
    {
        message = _rawMentionRegex.Replace(message, string.Empty);

        if (config.DiscordMessageAllowMentions)
        {
            var guild = _discordManager.Client.getGuildById(_discordManager.MainGuildId);
            var matches = _mentionRegex.Matches(message);
            foreach (Match match in matches)
            {
                var mention = match.Groups["mention"].Value.ToLowerInvariant();
                if (mention is "everyone" or "here")
                    continue;

                var members = guild.getMembersByName(mention, true);

                if (members.size() == 0)
                {
                    message = message.Replace(match.Value, string.Empty);
                    continue;
                }

                message = message.Replace(match.Value, ((Member)members.get(0)).getAsMention());
            }
        }
        else
        {
            message = _mentionRegex.Replace(message, string.Empty);
        }

        _discordManager.Client.getTextChannelById(config.ChannelId)
            .sendMessage(string.Format(config.DiscordMessageFormat, author, message))
            .queue();
    }
}
