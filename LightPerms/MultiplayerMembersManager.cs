using Torch.API;
using Torch.API.Managers;
using Torch.Managers;
namespace LightPerms;

public class MultiplayerMembersManager : Manager
{
    private readonly Config _config;

    [Dependency]
    private readonly IPermissionsManager _permissionsManager = null!;
    [Dependency]
    private readonly IMultiplayerManagerServer _multiplayerManager = null!;

    public MultiplayerMembersManager(ITorchBase torchInstance, Config config) : base(torchInstance)
    {
        _config = config;
    }

    public override void Attach()
    {
        base.Attach();
        _multiplayerManager.PlayerJoined += MultiplayerManagerOnPlayerJoined;
    }
    
    private void MultiplayerManagerOnPlayerJoined(IPlayer player)
    {
        if (_permissionsManager.Db.Exists<GroupMember>("client_id = @0", player.SteamId))
            return;

        var defaultGroup = _permissionsManager.GetGroup(_config.DefaultGroupName) ?? throw new ArgumentException("Default group does not exist or name was supplied incorrectly");
        
        var groupMember = new GroupMember
        {
            Name = player.Name,
            ClientId = player.SteamId.ToString(),
            GroupUid = defaultGroup.Uid
        };
        
        _permissionsManager.Db.Insert(groupMember);
    }
}
