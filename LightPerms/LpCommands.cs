using PetaPoco;
using Torch.API.Managers;
using Torch.Commands;
using Torch.Commands.Permissions;
using VRage.Game.ModAPI;
namespace LightPerms;

[Category("lp")]
public class LpCommands : CommandModule
{
    private IPermissionsManager Pm => Context.Torch.Managers.GetManager<IPermissionsManager>();

#if DEBUG
    [Command("gen")]
    public void GenTest()
    {
        var db = Pm.Db;
        
        db.Execute("delete from groups");
        db.Execute("delete from group_members");
        db.Execute("delete from permissions");
            
        var playerGroup = (long)db.Insert(new Group {Name = "player"});
        db.Insert(new Permission {Value = "test.permission.player", GroupUid = playerGroup});

        var adminGroup = (long)db.Insert(new Group {Name = "admin"});
        db.Insert(new Permission {Value = "test.permission.admin", GroupUid = adminGroup});

        db.Insert(new GroupMember {Name = Context.Player.DisplayName, ClientId = Context.Player.SteamUserId.ToString(), GroupUid = playerGroup});
    }
#endif

    [Command("get members")]
    [Permission(MyPromoteLevel.Admin)]
    public void GetGroupMembers(string groupName)
    {
        if (Pm.Db.SingleOrDefault<Group>(Sql.Builder.Where("name = @0", groupName)) is not { } group)
        {
            Context.Respond("No group found with given name");
            return;
        }

        var members = Pm.Db.Query<GroupMember>(Sql.Builder.Where("group_uid = @0", group.Uid)).ToArray();
        
        Context.Respond($"Members of {groupName} group ({members.Length} total):\n   {string.Join("\n   ", members.Select(b => $"{b.Name} ({b.ClientId})"))}");
    }
    
    [Command("has perm")]
    [Permission(MyPromoteLevel.Admin)]
    public void HasPermission(string perm, ulong clientId = 0)
    {
        clientId = Context.Player?.SteamUserId ?? clientId;
        
        if (clientId == 0)
        {
            Context.Respond("Specify valid client id.");
            return;
        }
        
        Context.Respond(Pm.HasPermission(clientId, perm) ? "Yes" : "No");
    }

    [Command("get groups")]
    [Permission(MyPromoteLevel.Admin)]
    public void GetGroups()
    {
        Context.Respond($"Available groups:\n   {string.Join("\n   ", Pm.Db.Query<Group>().Select(b => $"{b.Name} - id {b.Uid}"))}");
    }

    [Command("get perms")]
    [Permission(MyPromoteLevel.Admin)]
    public void GetPerms(string groupName)
    {
        if (Pm.Db.SingleOrDefault<Group>(Sql.Builder.Where("name = @0", groupName)) is not { } group)
        {
            Context.Respond("No group found with given name");
            return;
        }
        
        Context.Respond($"Available perms:\n   {string.Join("\n   ", Pm.Db.Query<Permission>(Sql.Builder.Where("group_uid = @0", group.Uid)).Select(b => b.Value).OrderBy(b => b))}");
    }

    [Command("add group")]
    [Permission(MyPromoteLevel.Admin)]
    public void AddGroup(string groupName)
    {
        if (Pm.Db.Exists<Group>("name = @0", groupName))
        {
            Context.Respond("Group already exists");
            return;
        }
        
        Pm.Db.Insert(new Group
        {
            Name = groupName
        });
        Context.Respond("Added");
    }
    
    [Command("add perm")]
    [Permission(MyPromoteLevel.Admin)]
    public void AddPerm(string groupName, string permission)
    {
        if (Pm.Db.SingleOrDefault<Group>(Sql.Builder.Where("name = @0", groupName)) is not { } group)
        {
            Context.Respond("No group found with given name");
            return;
        }

        if (Pm.Db.Exists<Permission>("group_uid = @0 and value like @1", group.Uid, permission.Replace('*', '%')))
        {
            Context.Respond("Permission already exists");
            return;
        }

        Pm.Db.Insert(new Permission
        {
            Value = permission,
            GroupUid = group.Uid
        });
        Context.Respond("Added");
    }
    
    [Command("del group")]
    [Permission(MyPromoteLevel.Admin)]
    public void DelGroup(string groupName)
    {
        if (Pm.Db.Delete<Group>(Sql.Builder.Where("name = @0", groupName)) != 1)
        {
            Context.Respond("Group not exists");
            return;
        }
        Context.Respond("Deleted");
    }
    
    [Command("del perm")]
    [Permission(MyPromoteLevel.Admin)]
    public void DelPerm(string groupName, string permission)
    {
        if (Pm.Db.SingleOrDefault<Group>(Sql.Builder.Where("name = @0", groupName)) is not { } group)
        {
            Context.Respond("No group found with given name");
            return;
        }

        var count = Pm.Db.Delete<Permission>(Sql.Builder.Where("group_uid = @0 and value like @1", group.Uid, permission.Replace('*', '%')));
        if (count > 0)
        {
            Context.Respond($"Deleted {count} permissions");
            return;
        }
        
        Context.Respond("No permissions found");
    }

    [Command("assign group")]
    [Permission(MyPromoteLevel.Admin)]
    public void AssignGroup(string groupName, ulong clientId = 0)
    {
        if (clientId == 0)
            clientId = Context.Player.SteamUserId;
        
        if (Pm.Db.SingleOrDefault<Group>(Sql.Builder.Where("name = @0", groupName)) is not { } group)
        {
            Context.Respond("No group found with given name");
            return;
        }

        if (Pm.Db.Exists<GroupMember>("group_uid = @0 and client_id = @1", group.Uid, clientId))
        {
            Context.Respond("User already assigned to this group");
            return;
        }
        
        Pm.AssignGroup(clientId, groupName);
        Context.Respond("User assigned to the group");
    }
}
