using Discord;
using Discord.WebSocket;
using NLog;
using NLog.Fluent;
using PetaPoco;
using Torch.API;
using Torch.Managers;
namespace LightPerms.Discord;

public class DiscordManager : Manager
{
    private static readonly ILogger Log = LogManager.GetCurrentClassLogger();
    
    private readonly Config _config;
    [Dependency]
    private readonly IPermissionsManager _permissionsManager = null!;
    private readonly DiscordSocketClient _client = new(new()
    {
        GatewayIntents = GatewayIntents.Guilds | GatewayIntents.GuildMessages | GatewayIntents.GuildMembers
    });
    
    private IDatabase _db = null!;

    public Dictionary<string, ulong> WaitingUsers { get; } = new();

    public DiscordManager(ITorchBase torchInstance, Config config) : base(torchInstance)
    {
        _config = config;
        _client.SlashCommandExecuted += ClientOnSlashCommandExecutedAsync;
        _client.GuildMemberUpdated += ClientOnGuildMemberUpdatedAsync;
        _client.Ready += ClientOnReady;
        _client.Log += message =>
        {
            NLog.Fluent.Log.Level(message.Severity switch
            {
                LogSeverity.Critical => LogLevel.Fatal,
                LogSeverity.Error => LogLevel.Error,
                LogSeverity.Warning => LogLevel.Warn,
                LogSeverity.Info => LogLevel.Info,
                LogSeverity.Debug => LogLevel.Debug,
                LogSeverity.Verbose => LogLevel.Trace,
                _ => throw new ArgumentOutOfRangeException()
            }).LoggerName(message.Source).Exception(message.Exception).Message(message.Message).Write();
            return Task.CompletedTask;
        };
    }
    private async Task ClientOnReady()
    {
        var guild = _client.GetGuild(_config.GuildId);
        
        if (_config.RoleConfigs.Any(b => guild.Roles.All(c => c.Id != b.RoleId)))
            throw new InvalidOperationException("Some roles are not exists in the guild");

        await AddSlashCommandsAsync(guild);

        await guild.DownloadUsersAsync();
        await Task.WhenAll(guild.Users.Select(b => ClientOnGuildMemberUpdatedAsync(default, b)));
    }

    public override void Attach()
    {
        base.Attach();
        _db = _permissionsManager.Db;
        _db.Execute(LinkedUser.CreateTableSql);
        
        Task.Run(AttachAsync);
    }

    private async Task AttachAsync()
    {
        await _client.LoginAsync(TokenType.Bot, _config.Token);
        await _client.StartAsync();
    }
    
    private async Task ClientOnGuildMemberUpdatedAsync(Cacheable<SocketGuildUser, ulong> cacheable, SocketGuildUser user)
    {
        if (await _db.SingleOrDefaultAsync<LinkedUser>(Sql.Builder.Where("discord_id = @0", user.Id.ToString())) is not { } linkedUser ||
            await _db.SingleOrDefaultAsync<GroupMember>(Sql.Builder.Where("client_id = @0", linkedUser.ClientId)) is not { } groupMember)
            return;

        bool HasRole(ulong id) => user.Roles.Any(b => b.Id == id);

        foreach (var (role, group) in _config.RoleConfigs.Select(b => (b, _permissionsManager.GetGroup(b.GroupName)!)))
        {
            if (!HasRole(role.RoleId) && groupMember.GroupUid == group.Uid)
            {
                _permissionsManager.AssignGroup(linkedUser.ClientIdNumber, "player");
                Log.Info($"Removed group {role.GroupName} from {linkedUser.ClientId}");
            }
            else if (HasRole(role.RoleId) && groupMember.GroupUid != group.Uid)
            {
                _permissionsManager.AssignGroup(linkedUser.ClientIdNumber, group.Name);
                groupMember.GroupUid = group.Uid;
                Log.Info($"Assigned group {role.GroupName} to {linkedUser.ClientId}");
            }
        }
    }

    private async Task ClientOnSlashCommandExecutedAsync(SocketSlashCommand arg)
    {
        if (arg.CommandName != "lp-link")
            return;

        if (GetClientIdByDiscordId(arg.User.Id) is { })
        {
            await arg.RespondAsync("You are already linked with lp.");
            return;
        }

        var username = Format.UsernameAndDiscriminator(arg.User, false);

        if (!WaitingUsers.ContainsKey(username))
        {
            await arg.RespondAsync("Type `!lp link` in-game first.");
            return;
        }

        var clientId = WaitingUsers[username];
        WaitingUsers.Remove(username);
        
        await _db.InsertAsync(new LinkedUser
        {
            DiscordId = arg.User.Id.ToString(),
            ClientId = clientId.ToString()
        });
        await arg.RespondAsync("Linked successfully.");
    }

    private Task AddSlashCommandsAsync(SocketGuild guild)
    {
        return guild.CreateApplicationCommandAsync(new SlashCommandBuilder()
            .WithName("lp-link")
            .WithDescription("Light perms link with game")
            .Build());
    }

    public ulong? GetClientIdByDiscordId(ulong discordId)
    {
        if (_db.SingleOrDefault<LinkedUser>(Sql.Builder.Where("discord_id = @0", discordId.ToString())) is not { } user)
            return null;
        return ulong.Parse(user.ClientId);
    }
}
