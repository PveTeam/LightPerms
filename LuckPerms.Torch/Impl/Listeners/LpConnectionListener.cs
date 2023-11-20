using LuckPerms.Torch.Extensions;
using LuckPerms.Torch.PlatformApi;
using LuckPerms.Torch.Utils.Extensions;
using me.lucko.luckperms.common.plugin;
using me.lucko.luckperms.common.plugin.util;
using Torch.API;
using Torch.API.Managers;
using Torch.Managers;
using Torch.Server.Managers;

namespace LuckPerms.Torch.Impl.Listeners;

public class LpConnectionListener(LuckPermsPlugin plugin) : AbstractConnectionListener(plugin), IManager
{
    [Manager.Dependency]
    private MultiplayerManagerDedicated _multiplayerManager = null!;

    public void Attach()
    {
        _multiplayerManager.PlayerJoined += MultiplayerManagerOnPlayerJoined;
        _multiplayerManager.PlayerLeft += MultiplayerManagerOnPlayerLeft;
    }

    private void MultiplayerManagerOnPlayerLeft(IPlayer player)
    {
        handleDisconnect(player.SteamId.GetUuid());
    }

    private void MultiplayerManagerOnPlayerJoined(IPlayer player)
    {
        plugin.getBootstrap().getScheduler().async().execute(() => OnPlayerJoined(player));
    }

    private void OnPlayerJoined(IPlayer player)
    {
        var uuid = player.SteamId.GetUuid();
        var user = loadUser(uuid, player.Name);
        
        recordConnection(uuid);
        plugin.getEventDispatcher().dispatchPlayerLoginProcess(uuid, player.Name, user);  // TODO move that to validate phase? but need to get username from steam somehow

        ((LpPlayerModel)player).InitializePermissions(user);
        plugin.getContextManager().signalContextUpdate(player);
    }

    public void Detach()
    {
    }
}