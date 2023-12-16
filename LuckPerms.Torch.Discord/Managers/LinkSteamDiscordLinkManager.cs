using LuckPerms.Torch.Discord.Abstractions;
using Torch.Managers;

namespace LuckPerms.Torch.Discord.Managers;

#if false
public class LinkSteamDiscordLinkManager : ILinkManager
{
    [Manager.Dependency]
    private readonly LinkSteamDiscordClientManager _apiManager = null!;
    
    public void Attach()
    {
    }

    public void Detach()
    {
    }

    public Task<bool> IsSteamIdLinkedAsync(ulong steamId) => _apiManager.IsSteamIdLinkedAsync(steamId);

    public async Task<bool> IsDiscordIdLinkedAsync(ulong discordId) =>
        await _apiManager.LookupSteamIdAsync(discordId) is not null;

    public Task<ulong?> ResolveSteamIdAsync(ulong discordId) => _apiManager.LookupSteamIdAsync(discordId);

    public Task<ulong?> ResolveDiscordIdAsync(ulong steamId) => _apiManager.LookupSteamIdAsync(steamId);
}
#endif