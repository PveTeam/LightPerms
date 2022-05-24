using PetaPoco;
namespace LightPerms;

[TableName("groups")]
[PrimaryKey(nameof(Uid), AutoIncrement = true)]
public class Group
{
    [Column]
    public long Uid { get; set; }
    [Column]
    public string Name { get; set; } = "group";

    internal const string CreateTableSql = "create table if not exists groups (uid INTEGER PRIMARY KEY AUTOINCREMENT, name TEXT NOT NULL);";
}

[TableName("permissions")]
[PrimaryKey(nameof(Uid), AutoIncrement = true)]
public class Permission
{
    [Column]
    public long Uid { get; set; }
    [Column]
    public string Value { get; set; } = "*";
    [Column]
    public long GroupUid { get; set; }
    
    internal const string CreateTableSql = "create table if not exists permissions (uid INTEGER PRIMARY KEY AUTOINCREMENT, value TEXT NOT NULL, group_uid INTEGER, FOREIGN KEY(group_uid) REFERENCES groups(uid));";
}

[TableName("group_members")]
[PrimaryKey(nameof(Uid), AutoIncrement = true)]
public class GroupMember
{
    [Column]
    public long Uid { get; set; }
    
    [Column]
    public string Name { get; set; } = "no name";

    [Column]
    public string ClientId { get; set; } = "0";
    
    [Column]
    public long GroupUid { get; set; }

    internal const string CreateTableSql = "create table if not exists group_members (uid INTEGER PRIMARY KEY AUTOINCREMENT, name TEXT NOT NULL, client_id INTEGER, group_uid INTEGER, FOREIGN KEY(group_uid) REFERENCES groups(uid));";
}
