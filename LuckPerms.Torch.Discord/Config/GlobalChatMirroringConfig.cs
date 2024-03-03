using Microsoft.Extensions.Configuration;

namespace LuckPerms.Torch.Discord.Config;

#nullable disable
public class GlobalChatMirroringConfig(IConfiguration configuration)
{
    public long ChannelId { get; set; } = configuration.GetValue<long>("channel-id");
    public string IngameAuthorFormat { get; set; } = configuration.GetValue<string>("ingame-author-format");
    public string IngameAuthorColor { get; set; } = configuration.GetValue<string>("ingame-author-color");
    public string DiscordMessageFormat { get; set; } = configuration.GetValue<string>("discord-message-format");
    public bool DiscordMessageAllowMentions { get; set; } = configuration.GetValue<bool>("discord-message-allow-mentions");
}
