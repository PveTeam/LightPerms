using java.util;
using LuckPerms.Torch.Utils.Extensions;
using Torch.API;
using Torch.API.Managers;
using Torch.Server.Managers;

namespace LuckPerms.Torch.Extensions;

public static class MultiplayerManagerExtensions
{
    public static IPlayer GetPlayer(this IMultiplayerManagerServer manager, ulong steamId) =>
        ((MultiplayerManagerDedicated)manager).Players[steamId];
    
    public static IPlayer GetPlayer(this IMultiplayerManagerServer manager, UUID uuid) =>
        ((MultiplayerManagerDedicated)manager).Players[uuid.GetSteamId()];
}