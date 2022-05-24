using Torch.API;
using Torch.API.Managers;
using Torch.Managers;
namespace LightPerms;

public class MultiplayerMembersManager : Manager
{
    [Dependency]
    private readonly IPermissionsManager _permissionsManager = null!;
    [Dependency]
    private readonly IMultiplayerManagerServer _multiplayerManager = null!;

    public MultiplayerMembersManager(ITorchBase torchInstance) : base(torchInstance)
    {
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
        
        var groupMember = new GroupMember
        {
            Name = player.Name,
            ClientId = player.SteamId.ToString(),
            GroupUid = 0
        };
        
        _permissionsManager.Db.Insert(groupMember);
    }
}
