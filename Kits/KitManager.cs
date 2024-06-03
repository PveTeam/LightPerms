using System.Diagnostics.CodeAnalysis;
using heh;
using net.luckperms.api;
using NLog;
using PetaPoco;
using Sandbox.Game;
using Sandbox.Game.Entities;
using Sandbox.Game.GameSystems.BankingAndCurrency;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.World;
using Torch.API;
using Torch.API.Managers;
using Torch.Managers;
using Torch.Server.Managers;
using VRage;
using VRage.Game.Entity;
using VRage.Library.Utils;
using VRage.ObjectBuilders;
namespace Kits;

public class KitManager : Manager, IKitManager
{
    private static readonly ILogger Log = LogManager.GetCurrentClassLogger();
    
    private readonly Config _config;
    
    [Dependency]
    private readonly IDbManager _dbManager = null!;

    [Dependency]
    private readonly MultiplayerManagerDedicated _multiplayerManager = null!;

    private IDatabase _db = null!;
    
    public KitManager(ITorchBase torchInstance, Config config) : base(torchInstance)
    {
        _config = config;
    }

    public override void Attach()
    {
        base.Attach();
        _db = _dbManager.Create("kits");
        MyVisualScriptLogicProvider.RespawnShipSpawned += RespawnShipSpawned;
    }
    private void RespawnShipSpawned(long shipEntityId, long playerId, string respawnShipPrefabName)
    {
        if (!MyEntities.TryGetEntityById(shipEntityId, out MyCubeGrid grid) ||
            !Sync.Players.TryGetPlayerId(playerId, out var playerClientId) || 
            Sync.Players.GetPlayerById(playerClientId) is not { } player)
            return;
        foreach (var kit in _config.Kits.Where(b => CanGiveRespawnKit(player, b, respawnShipPrefabName, out _)))
        {
            GiveKit(player, grid.GetFatBlocks().First(b => b is MyCargoContainer or MyCockpit).GetInventoryBase(), kit);
        }
    }

    public void GiveKit(MyPlayer player, MyInventoryBase inventory, string kitName)
    {
        GiveKit(player, inventory, GetKit(kitName));
    }
    public bool CanGiveKit(MyPlayer player, string kitName, [NotNullWhen(false)] out string? reason)
    {
        return CanGiveKit(player, GetKit(kitName), out reason);
    }
    public bool CanGiveRespawnKit(MyPlayer player, string kitName, string respawnName, [NotNullWhen(false)] out string? reason)
    {
        return CanGiveRespawnKit(player, GetKit(kitName), respawnName, out reason);
    }
    public KitViewModel GetKit(string kitName)
    {
        return _config.Kits.FirstOrDefault(b => b.Name == kitName) ?? throw new KitNotFoundException(kitName);
    }
    public bool TryGetKit(string kitName, [NotNullWhen(true)] out KitViewModel? kit)
    {
        kit = _config.Kits.FirstOrDefault(b => b.Name == kitName);
        return kit is not null;
    }

    public IReadOnlyCollection<KitViewModel> ListKits()
    {
        return _config.Kits.ToArray();
    }

    public IReadOnlyCollection<(KitViewModel kit, string? reason)> ListKits(MyPlayer player)
    {
        return _config.Kits
            .Select(b => CanGiveKit(player, b, out var reason) ? (b, reason) : (b, null))
            .ToArray();
    }

    public void GiveKit(MyPlayer player, MyInventoryBase inventory, KitViewModel kit)
    {
        if (kit.UseCooldownMinutes > 0)
        {
            CheckTable();
            _db.Insert(new PlayerCooldown {Id = player.Id.SteamId.ToString(), KitName = kit.Name, LastUsed = DateTime.Now});
        }

        MyBankingSystem.ChangeBalance(player.Identity.IdentityId, kit.UseCost);

        foreach (var item in kit.Items.Where(b => b.Probability >= 1 || b.Probability < MyRandom.Instance.GetRandomFloat(0, 1)))
        {
            inventory.AddItems((MyFixedPoint)item.Amount, MyObjectBuilderSerializer.CreateNewObject(item.Id));
        }
        
        Log.Info($"Given kit {kit.Name} to {player.DisplayName} ({player.Id.SteamId})");
    }
    
    public bool CanGiveKit(MyPlayer player, KitViewModel kit, [NotNullWhen(false)] out string? reason)
    {
        reason = null;
        
        var level = MySession.Static.GetUserPromoteLevel(player.Id.SteamId);
        if (level < kit.RequiredPromoteLevel ||
            !string.IsNullOrEmpty(kit.LpPermission))
        {
            var api = LuckPermsProvider.get();
            var torchPlayer = _multiplayerManager.Players[player.Id.SteamId];
            
            if (!api.getPlayerAdapter(typeof(IPlayer)).getPermissionData(torchPlayer).checkPermission(kit.LpPermission).asBoolean())
            {
                reason = "Not enough rights to acquire this";
                return false;
            }
        }

        if (kit.UseCost > 0 && kit.UseCost > MyBankingSystem.GetBalance(player.Identity.IdentityId))
        {
            reason = "Not enough money to acquire this";
            return false;
        }

        if (kit.UseCooldownMinutes <= 0)
            return true;

        var sql = Sql.Builder.Where("id = @0", player.Id.SteamId).Append("AND kit_name = @0", kit.Name);
        CheckTable();
        var playerCooldown = _db.SingleOrDefault<PlayerCooldown>(sql);
        var cooldown = DateTime.Now - playerCooldown?.LastUsed;
            
        if (cooldown is null)
            return true;

        if (cooldown > TimeSpan.FromMinutes(kit.UseCooldownMinutes))
        {
            _db.Delete<PlayerCooldown>(sql);
            return true;
        }
        
        reason = $@"Next use available in {TimeSpan.FromMinutes(kit.UseCooldownMinutes) - cooldown:dd\.hh\:mm\:ss}";
        return false;

    }
    public bool CanGiveRespawnKit(MyPlayer player, KitViewModel kit, string respawnName, [NotNullWhen(false)] out string? reason)
    {
        reason = "Invalid respawn name";
        return kit.RespawnPodWildcards.Any(respawnName.Glob) && CanGiveKit(player, kit, out reason);
    }

    private void CheckTable()
    {
        _db.Execute("create table if not exists cooldown (uid INTEGER PRIMARY KEY AUTOINCREMENT, id TEXT NOT NULL, kit_name TEXT NOT NULL, last_used DATETIME NOT NULL)");
    }
}

[TableName("cooldown")]
[PrimaryKey(nameof(Uid), AutoIncrement = true)]
public class PlayerCooldown
{
    [Column]
    public long Uid { get; set; }
    [Column]
    public string Id { get; set; } = string.Empty;
    [Column]
    public string KitName { get; set; } = string.Empty;
    [Column]
    public DateTime LastUsed { get; set; }
}

public interface IKitManager : IManager
{
    void GiveKit(MyPlayer player, MyInventoryBase inventory, string kitName);
    void GiveKit(MyPlayer player, MyInventoryBase inventory, KitViewModel kit);
    bool CanGiveKit(MyPlayer player, string kitName, [NotNullWhen(false)] out string? reason);
    bool CanGiveKit(MyPlayer player, KitViewModel kit, [NotNullWhen(false)] out string? reason);
    bool CanGiveRespawnKit(MyPlayer player, KitViewModel kit, string respawnName, [NotNullWhen(false)] out string? reason);
    bool CanGiveRespawnKit(MyPlayer player, string kitName, string respawnName, [NotNullWhen(false)] out string? reason);
    KitViewModel GetKit(string kitName);
    bool TryGetKit(string kitName, [NotNullWhen(true)] out KitViewModel? kit);
    IReadOnlyCollection<KitViewModel> ListKits();
    IReadOnlyCollection<(KitViewModel kit, string? reason)> ListKits(MyPlayer player);
}

public class KitNotFoundException(string kitName) : KeyNotFoundException($"Kit {kitName} not found.");