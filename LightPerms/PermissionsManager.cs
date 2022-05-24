using heh;
using NLog;
using PetaPoco;
using Sandbox.Game.Multiplayer;
using Torch.API;
using Torch.API.Managers;
using Torch.Managers;
namespace LightPerms;

public interface IPermissionsManager : IManager
{
    bool HasPermission(ulong clientId, string permission);

    void AssignGroup(ulong clientId, string groupName);
    
    Group? GetGroup(string groupName);
    
    IDatabase Db { get; }
}

public class PermissionsManager : Manager, IPermissionsManager
{
    private static readonly ILogger Log = LogManager.GetCurrentClassLogger();
    private static readonly char[] InvalidPermissionChars = {'*', '?'};
    
    [Dependency]
    private readonly IDbManager _dbManager = null!;

    public PermissionsManager(ITorchBase torchInstance) : base(torchInstance)
    {
    }

    public override void Attach()
    {
        base.Attach();
        Db = _dbManager.Create("light_perms");
        Db.Execute(Group.CreateTableSql);
        Db.Execute(Permission.CreateTableSql);
        Db.Execute(GroupMember.CreateTableSql);

        if (GetGroup("player") is null)
            Db.Insert(new Group
            {
                Name = "player"
            });
    }

    public bool HasPermission(ulong clientId, string permission)
    {
        if (permission.IndexOfAny(InvalidPermissionChars) > -1)
            throw new InvalidOperationException("Permission should not contain any invalid characters");
            
        var member = Db.SingleOrDefault<GroupMember>(Sql.Builder.Where("client_id = @0", clientId));
        var result = member is not null && Db.Exists<Permission>("group_uid = @0 and @1 like replace(replace(value,'*','%'),'?','_')", member.GroupUid, permission);

        if (!result)
            Log.Info("User ({0}) has no permission '{1}'", clientId, permission);
            
        return result;
    }
    public void AssignGroup(ulong clientId, string groupName)
    {
        if (Sync.Players.TryGetPlayerIdentity(new(clientId)) is not { } identity)
            throw new InvalidOperationException($"Invalid client id {clientId}");

        if (Db.SingleOrDefault<Group>(Sql.Builder.Where("name = @0", groupName)) is not { } group)
            throw new InvalidOperationException($"Invalid group name {groupName}");
        
        if (Db.SingleOrDefault<GroupMember>(Sql.Builder.Where("client_id = @0", clientId)) is { } groupMember)
            Db.Delete(groupMember);
        
        Db.Insert(new GroupMember
        {
            Name = identity.DisplayName,
            ClientId = clientId.ToString(),
            GroupUid = group.Uid
        });
    }
    public Group? GetGroup(string groupName)
    {
        return Db.SingleOrDefault<Group>(Sql.Builder.Where("name = @0", groupName));
    }

    public IDatabase Db { get; private set; } = null!;
}
