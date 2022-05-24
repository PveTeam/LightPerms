using PetaPoco;
namespace LightPerms.Discord;

[TableName("linked_users")]
[PrimaryKey(nameof(Uid), AutoIncrement = true)]
public class LinkedUser
{
    [Column]
    public long Uid { get; set;}
    
    [Column]
    public string DiscordId { get; set; } = "0";
    [Column]
    public string ClientId { get; set; } = "0";
    
    internal const string CreateTableSql = "create table if not exists linked_users (uid INTEGER PRIMARY KEY AUTOINCREMENT, discord_id TEXT NOT NULL, client_id TEXT NOT NULL);";

    [Ignore]
    public ulong ClientIdNumber => ulong.Parse(ClientId);
}
