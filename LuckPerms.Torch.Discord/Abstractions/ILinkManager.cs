using Torch.API;
using Torch.API.Managers;

namespace LuckPerms.Torch.Discord.Abstractions;

public interface ILinkManager : IManager
{
    bool IsSteamIdLinked(IPlayer player);
    
    long? ResolveDiscordId(IPlayer player);
}