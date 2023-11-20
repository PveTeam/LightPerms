using java.lang;
using LuckPerms.Torch.Utils.Extensions;
using Maintenance.Extensions;
using Microsoft.Extensions.Configuration;
using net.luckperms.api;
using net.luckperms.api.model.user;
using NLog;
using Sandbox;
using Sandbox.Engine.Multiplayer;
using Torch.API;
using Torch.API.Event;
using Torch.Commands;
using Torch.Managers;
using Torch.Server.Managers;
using Torch.Utils;
using VRage.Game.ModAPI;
using VRage.Network;

namespace Maintenance.Managers;

public class MaintenanceManager(ITorchBase torch) : Manager(torch), IEventHandler
{
    public const string BypassPermission = "maintenance.bypass";
    
    [ReflectedStaticMethod(Type = typeof(MyDedicatedServerBase))]
    private static readonly Func<ulong, string> ConvertSteamIDFrom64 = null!;

    private static readonly ILogger Log = LogManager.GetCurrentClassLogger();
    
    [Dependency]
    private readonly IEventManager _eventManager = null!;
    
    [Dependency]
    private readonly ConfigManager _configManager = null!;

    [Dependency]
    private readonly MultiplayerManagerDedicated _multiplayerManager = null!;

    [Dependency]
    private readonly CommandManager _commandManager = null!;

    private bool _maintenanceEnabled;
    private IDisposable? _disposable;

    public bool MaintenanceEnabled
    {
        get => _maintenanceEnabled;
        set
        {
            if (value == _maintenanceEnabled) return;
            _maintenanceEnabled = value;
            
            Log.Info(_maintenanceEnabled ? "Maintenance Enabled" : "Maintenance Disabled");

            ExecuteCommandsOnModeChange();

            if (!_maintenanceEnabled ||
                !_configManager.Configuration.GetValue<bool>(ConfigKeys.KickOnlinePlayers)) return;
            
            Torch.Invoke(() =>
            {
                Log.Info("Kicking online players");
                foreach (var steamId in _multiplayerManager.Players.Keys)
                {
                    if (!GetIsAllowedToJoin(steamId))
                        MyMultiplayer.Static.DisconnectClient(steamId);
                }
            });
        }
    }

    private void ExecuteCommandsOnModeChange()
    {
        var commandsOnEnable =
            _configManager.Configuration.GetSection(ConfigKeys.CommandsOnMaintenanceEnable).GetChildren()
                .Select(b => b.Value!).ToArray();
        var commandsOnDisable =
            _configManager.Configuration.GetSection(ConfigKeys.CommandsOnMaintenanceDisable).GetChildren()
                .Select(b => b.Value!).ToArray();

        if (commandsOnEnable.Length <= 0 && commandsOnDisable.Length <= 0) return;
        
        Torch.Invoke(() =>
        {
            switch (_maintenanceEnabled)
            {
                case true when commandsOnEnable.Length > 0:
                {
                    foreach (var command in commandsOnEnable)
                    {
                        _commandManager.HandleCommandFromServer(command,
                            msg => Log.Info("Feedback from `{0}`: `{1}`", command, msg.Message));
                    }

                    break;
                }
                case false when commandsOnDisable.Length > 0:
                {
                    foreach (var command in commandsOnDisable)
                    {
                        _commandManager.HandleCommandFromServer(command,
                            msg => Log.Info("Feedback from `{0}`: `{1}`", command, msg.Message));
                    }

                    break;
                }
            }
        });
    }

    public override void Attach()
    {
        _eventManager.RegisterHandler(this);
        _disposable = _configManager.Configuration.GetRequiredSection(ConfigKeys.MaintenanceEnabled).GetReloadToken()
            .RegisterChangeCallback(
                _ => MaintenanceEnabled = _configManager.Configuration.GetValue<bool>(ConfigKeys.MaintenanceEnabled),
                null);
    }

    public override void Detach()
    {
        _disposable?.Dispose();
    }

    [EventHandler]
    private void OnValidateAuthTicket(ref ValidateAuthTicketEvent info)
    {
        if (!MaintenanceEnabled) return;

        var steamId = info.SteamID;
        var response = info.SteamResponse;

        info.FutureVerdict = FutureVerdict();

        async Task<JoinResult> FutureVerdict()
        {
            if (await GetIsAllowedToJoinAsync(steamId)) return response;

            Log.Info("Rejecting {0}", steamId);
            return JoinResult.TicketCanceled;
        }
    }
    
    private static async ValueTask<bool> GetIsAllowedToJoinAsync(ulong steamId)
    {
        try
        {
            var api = LuckPermsProvider.get();
            var user = await api.getUserManager().loadUser(steamId.GetUuid()).ToTask<User>();

            return user.getCachedData().getPermissionData().checkPermission(BypassPermission).asBoolean();
        }
        catch (IllegalStateException)
        {
            // we dont have api initialized
        }
        
        var stringId = steamId.ToString();

        return MySandboxGame.ConfigDedicated.Administrators.Any(
            b => b == stringId || b == ConvertSteamIDFrom64(steamId));
    }
    
    private bool GetIsAllowedToJoin(ulong steamId)
    {
        try
        {
            var api = LuckPermsProvider.get();
            return api.getPlayerAdapter(typeof(IPlayer)).getPermissionData(_multiplayerManager.Players[steamId])
                .checkPermission(BypassPermission).asBoolean();
        }
        catch (IllegalStateException)
        {
            // we dont have api initialized
        }

        return _multiplayerManager.GetUserPromoteLevel(steamId) == MyPromoteLevel.Owner;
    }
}