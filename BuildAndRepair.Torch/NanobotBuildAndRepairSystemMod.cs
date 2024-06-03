using System.IO;
using BuildAndRepair.Torch.Managers;
using BuildAndRepair.Torch.Messages;
using Microsoft.Extensions.Configuration;
using Sandbox.Game.Multiplayer;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Weapons;
using Torch.API.Managers;
using VRage;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.ModAPI;

namespace BuildAndRepair.Torch;

[MySessionComponentDescriptor(MyUpdateOrder.BeforeSimulation)]
public class NanobotBuildAndRepairSystemMod : MySessionComponentBase
{
    private const string Version = "V2.1.5 2020-04-12";

    private const string CmdKey = "/nanobars";
    private const string CmdHelp1 = "-?";
    private const string CmdHelp2 = "-help";
    private const string CmdCwsf = "-cwsf";
    private const string CmdCpsf = "-cpsf";
    private const string CmdLogLevel = "-loglevel";
    private const string CmdWritePerfCounter = "-writeperf";
    private const string CmdWriteTranslation = "-writetranslation";

    private const string CmdLogLevel_All = "all";
    private const string CmdLogLevel_Default = "default";
    public const int MaxBackgroundTasks_Default = 4;
    public const int MaxBackgroundTasks_Max = 10;
    public const int MaxBackgroundTasks_Min = 1;

    private static readonly ushort MSGID_MOD_DATAREQUEST = 40000;
    private static readonly ushort MSGID_MOD_SETTINGS = 40001;
    private static readonly ushort MSGID_MOD_COMMAND = 40010;
    private static readonly ushort MSGID_BLOCK_DATAREQUEST = 40100;
    private static readonly ushort MSGID_BLOCK_SETTINGS_FROM_SERVER = 40102;
    private static readonly ushort MSGID_BLOCK_SETTINGS_FROM_CLIENT = 40103;
    private static readonly ushort MSGID_BLOCK_STATE_FROM_SERVER = 40104;
    public static bool SettingsValid;
    public static SyncModSettings Settings = new();
    private static TimeSpan _LastSourcesAndTargetsUpdateTimer;
    private static readonly TimeSpan SourcesAndTargetsUpdateTimerInterval = new(0, 0, 2);
    private static TimeSpan _LastSyncModDataRequestSend;

    public static Guid ModGuid = new("8B57046C-DA20-4DE1-8E35-513FD21E3B9F");

    private static int ActualBackgroundTaskCount;

    /// <summary>
    ///     Current known Build and Repair Systems in world
    /// </summary>
    private static Dictionary<long, NanobotBuildAndRepairSystemBlock>? _BuildAndRepairSystems;

    private bool _Init;

    public static Dictionary<long, NanobotBuildAndRepairSystemBlock> BuildAndRepairSystems => _BuildAndRepairSystems ??= new();

    public static MultigridProjectorModAgent MultigridProjectorApi { get; private set; } = null!;

    /// <summary>
    /// </summary>
    public void Init()
    {
        Mod.Log.Write("BuildAndRepairSystemMod: Initializing IsServer={0}, IsDedicated={1}", MyAPIGateway.Session.IsServer, MyAPIGateway.Utilities.IsDedicated);
        _Init = true;

        Settings = Plugin.Torch.Managers.GetManager<ConfigManager>().Configuration.Get<SyncModSettings>()!;
        SettingsValid = MyAPIGateway.Session.IsServer;
        SettingsChanged();

        MultigridProjectorApi = new();

        MyAPIGateway.Session.DamageSystem.RegisterBeforeDamageHandler(0, BeforeDamageHandlerNoDamageByBuildAndRepairSystem);
        if (MyAPIGateway.Session.IsServer)
        {
            //Detect friendly damage (only needed on server)
            MyAPIGateway.Session.DamageSystem.RegisterAfterDamageHandler(100, AfterDamageHandlerNoDamageByBuildAndRepairSystem);

            MyAPIGateway.Multiplayer.RegisterSecureMessageHandler(MSGID_MOD_COMMAND, SyncModCommandReceived);
            MyAPIGateway.Multiplayer.RegisterSecureMessageHandler(MSGID_MOD_DATAREQUEST, SyncModDataRequestReceived);
            MyAPIGateway.Multiplayer.RegisterSecureMessageHandler(MSGID_BLOCK_DATAREQUEST, SyncBlockDataRequestReceived);
            //Same as MSGID_BLOCK_SETTINGS but SendMessageToOthers sends also to self, which will result in stack overflow
            MyAPIGateway.Multiplayer.RegisterSecureMessageHandler(MSGID_BLOCK_SETTINGS_FROM_CLIENT, SyncBlockSettingsReceived);
        }
        else
        {
            MyAPIGateway.Multiplayer.RegisterSecureMessageHandler(MSGID_MOD_SETTINGS, SyncModSettingsReceived);
            MyAPIGateway.Multiplayer.RegisterSecureMessageHandler(MSGID_BLOCK_SETTINGS_FROM_SERVER, SyncBlockSettingsReceived);
            MyAPIGateway.Multiplayer.RegisterSecureMessageHandler(MSGID_BLOCK_STATE_FROM_SERVER, SyncBlockStateReceived);
            SyncModDataRequestSend();
        }

        MyAPIGateway.Utilities.MessageEntered += Utilities_MessageEntered;

        Mod.Log.Write("BuildAndRepairSystemMod: Initialized.");
    }

    /// <summary>
    /// </summary>
    private static void SettingsChanged()
    {
        if (SettingsValid)
        {
            foreach (var entry in BuildAndRepairSystems)
                entry.Value.SettingsChanged();
            InitControls();
        }
    }

    /// <summary>
    /// </summary>
    public static void InitControls()
    {
        //Call also on dedicated else the properties for the scripting interface are not initialized
        if (SettingsValid && !NanobotBuildAndRepairSystemTerminal.CustomControlsInit && BuildAndRepairSystems.Count > 0) NanobotBuildAndRepairSystemTerminal.InitializeControls();
    }

    /// <summary>
    /// </summary>
    private void Utilities_MessageEntered(string messageText, ref bool sendToOthers)
    {
        if (string.IsNullOrEmpty(messageText)) return;
        var cmd = messageText.ToLower();
        if (cmd.StartsWith(CmdKey))
        {
            if (Mod.Log.ShouldLog(Logging.Level.Verbose)) Mod.Log.Write(Logging.Level.Verbose, "BuildAndRepairSystemMod: Cmd: {0}", messageText);
            var args = cmd.Remove(0, CmdKey.Length).Trim().Split(' ');
            if (args.Length > 0)
            {
                if (Mod.Log.ShouldLog(Logging.Level.Verbose)) Mod.Log.Write(Logging.Level.Verbose, "BuildAndRepairSystemMod: Cmd args[0]: {0}", args[0]);
                switch (args[0].Trim())
                {
                    case CmdLogLevel:
                    case CmdCpsf:
                    case CmdCwsf:
                        MyAPIGateway.Utilities.ShowMessage(CmdKey, "command not allowed");
                        break;

                    case CmdWriteTranslation:
                        if (Mod.Log.ShouldLog(Logging.Level.Verbose)) Mod.Log.Write(Logging.Level.Verbose, "BuildAndRepairSystemMod: CmdWriteTranslation");
                        if (args.Length > 1)
                        {
                            if (Enum.TryParse(args[1], true, out MyLanguagesEnum lang))
                            {
                                LocalizationHelper.ExportDictionary(lang + ".txt", Texts.GetDictionary(lang));
                                MyAPIGateway.Utilities.ShowMessage(CmdKey, string.Format(lang + ".txt writtenwa."));
                            }
                            else
                                MyAPIGateway.Utilities.ShowMessage(CmdKey,
                                                                   $"'{args[1]}' is not a valid language name {string.Join(",", Enum.GetNames(typeof(MyLanguagesEnum)))}");
                        }

                        break;

                    case CmdHelp1:
                    case CmdHelp2:
                    default:
                        MyAPIGateway.Utilities.ShowMissionScreen("NanobotBuildAndRepairSystem", "Help", "", GetHelpText());
                        break;
                }
            }
            else
                MyAPIGateway.Utilities.ShowMissionScreen("NanobotBuildAndRepairSystem", "Help", "", GetHelpText());

            sendToOthers = false;
        }
    }

    private string GetHelpText()
    {
        var text = string.Format(Texts.Cmd_HelpClient.String, Version, CmdHelp1, CmdHelp2,
                                 CmdLogLevel, CmdLogLevel_All, CmdLogLevel_Default,
                                 CmdWriteTranslation, string.Join(",", Enum.GetNames(typeof(MyLanguagesEnum))),
                                 MyAPIGateway.Utilities.GamePaths.UserDataPath + Path.DirectorySeparatorChar + "Storage" + Path.DirectorySeparatorChar + MyAPIGateway.Utilities.GamePaths.ModScopeName);
        if (MyAPIGateway.Session.IsServer) text += string.Format(Texts.Cmd_HelpServer.String, CmdCwsf, CmdCpsf);
        return text;
    }

    /// <summary>
    /// </summary>
    protected override void UnloadData()
    {
        _Init = false;
        try
        {
            if (MyAPIGateway.Utilities != null && MyAPIGateway.Multiplayer != null)
            {
                if (MyAPIGateway.Session.IsServer)
                {
                    MyAPIGateway.Multiplayer.UnregisterSecureMessageHandler(MSGID_MOD_DATAREQUEST, SyncModDataRequestReceived);
                    MyAPIGateway.Multiplayer.UnregisterSecureMessageHandler(MSGID_BLOCK_DATAREQUEST, SyncBlockDataRequestReceived);
                    MyAPIGateway.Multiplayer.UnregisterSecureMessageHandler(MSGID_BLOCK_SETTINGS_FROM_CLIENT, SyncBlockSettingsReceived);
                }
                else
                {
                    MyAPIGateway.Multiplayer.UnregisterSecureMessageHandler(MSGID_MOD_SETTINGS, SyncModSettingsReceived);
                    MyAPIGateway.Multiplayer.UnregisterSecureMessageHandler(MSGID_BLOCK_SETTINGS_FROM_SERVER, SyncBlockSettingsReceived);
                    MyAPIGateway.Multiplayer.UnregisterSecureMessageHandler(MSGID_BLOCK_STATE_FROM_SERVER, SyncBlockStateReceived);
                }
            }

            Mod.Log.Write("BuildAndRepairSystemMod: UnloadData.");
        }
        catch (Exception e)
        {
            Mod.Log.Error("NanobotBuildAndRepairSystemMod.UnloadData: {0}", e.ToString());
        }

        base.UnloadData();
    }

    /// <summary>
    /// </summary>
    public override void UpdateBeforeSimulation()
    {
        try
        {
            if (!_Init)
            {
                if (MyAPIGateway.Session == null) return;
                Init();
            }
            else
            {
                if (MyAPIGateway.Session.IsServer)
                    RebuildSourcesAndTargetsTimer();
                else if (!SettingsValid)
                    if (MyAPIGateway.Session.ElapsedPlayTime.Subtract(_LastSyncModDataRequestSend) >= TimeSpan.FromSeconds(10))
                    {
                        SyncModDataRequestSend();
                        _LastSyncModDataRequestSend = MyAPIGateway.Session.ElapsedPlayTime;
                    }
            }
        }
        catch (Exception e)
        {
            Mod.Log.Error(e);
        }
    }

    /// <summary>
    ///     Damage Handler: Prevent Damage from BuildAndRepairSystem
    /// </summary>
    public void BeforeDamageHandlerNoDamageByBuildAndRepairSystem(object target, ref MyDamageInformation info)
    {
        try
        {
            if (info.Type == MyDamageType.Weld)
                if (target is IMyCharacter)
                {
                    var logicalComponent = BuildAndRepairSystems.GetValueOrDefault(info.AttackerId);
                    if (logicalComponent != null)
                    {
                        var terminalBlock = logicalComponent.Entity as IMyTerminalBlock;
                        if (Mod.Log.ShouldLog(Logging.Level.Communication))
                            Mod.Log.Write(Logging.Level.Communication, "BuildAndRepairSystemMod: Prevent Damage from BuildAndRepairSystem={0} Amount={1}",
                                          terminalBlock != null ? terminalBlock.CustomName : logicalComponent.Entity.DisplayName, info.Amount);
                        info.Amount = 0;
                    }
                }
        }
        catch (Exception e)
        {
            Mod.Log.Error("BuildAndRepairSystemMod: Exception in BeforeDamageHandlerNoDamageByBuildAndRepairSystem: Source={0}, Message={1}", e.Source, e.Message);
        }
    }

    /// <summary>
    ///     Damage Handler: Register friendly damage
    /// </summary>
    public void AfterDamageHandlerNoDamageByBuildAndRepairSystem(object target, MyDamageInformation info)
    {
        try
        {
            if (info.Type == MyDamageType.Grind && info.Amount > 0)
            {
                if (target is IMySlimBlock targetBlock)
                {
                    MyAPIGateway.Entities.TryGetEntityById(info.AttackerId, out var attackerEntity);

                    var attackerId = 0L;

                    if (attackerEntity is IMyShipGrinder shipGrinder)
                        attackerId = shipGrinder.OwnerId;
                    else
                    {
                        if (attackerEntity is IMyEngineerToolBase characterGrinder)
                            attackerId = characterGrinder.OwnerIdentityId;
                    }

                    if (Mod.Log.ShouldLog(Logging.Level.Info))
                        Mod.Log.Write(Logging.Level.Info, "BuildAndRepairSystemMod: AfterDamaged1 {0} from {1} attackerId={2} Amount={3}", Logging.BlockName(target), Logging.BlockName(attackerEntity), attackerId, info.Amount);

                    if (attackerId != 0)
                    {
                        if (Mod.Log.ShouldLog(Logging.Level.Info)) Mod.Log.Write(Logging.Level.Info, "BuildAndRepairSystemMod: Damaged {0} from attackerId={1} Amount={2}", Logging.BlockName(target), attackerId, info.Amount);
                        foreach (var entry in BuildAndRepairSystems)
                        {
                            var relation = entry.Value.Welder.GetUserRelationToOwner(attackerId);
                            if (Mod.Log.ShouldLog(Logging.Level.Info))
                                Mod.Log.Write(Logging.Level.Info, "BuildAndRepairSystemMod: {0} Damaged Check Add FriendlyDamage {1} relation {2}", Logging.BlockName(entry.Value.Welder), Logging.BlockName(targetBlock), relation);
                            if (relation.IsFriendly())
                            {
                                //A 'friendly' damage from grinder -> do not repair (for a while)
                                entry.Value.FriendlyDamage[targetBlock] = MyAPIGateway.Session.ElapsedPlayTime + Settings.FriendlyDamageTimeout;
                                if (Mod.Log.ShouldLog(Logging.Level.Info))
                                    Mod.Log.Write(Logging.Level.Info, "BuildAndRepairSystemMod: {0} Damaged Add FriendlyDamage {0} Timeout {1}", Logging.BlockName(entry.Value.Welder), Logging.BlockName(targetBlock),
                                                  entry.Value.FriendlyDamage[targetBlock]);
                            }
                        }
                    }
                }
            }
        }
        catch (Exception e)
        {
            Mod.Log.Error("BuildAndRepairSystemMod: Exception in BeforeDamageHandlerNoDamageByBuildAndRepairSystem: Source={0}, Message={1}", e.Source, e.Message);
        }
    }

    /// <summary>
    ///     Rebuild the list of targets and inventory sources
    /// </summary>
    protected void RebuildSourcesAndTargetsTimer()
    {
        if (MyAPIGateway.Session.ElapsedPlayTime.Subtract(_LastSourcesAndTargetsUpdateTimer) > SourcesAndTargetsUpdateTimerInterval)
        {
            foreach (var buildAndRepairSystem in BuildAndRepairSystems.Values)
                buildAndRepairSystem.UpdateSourcesAndTargetsTimer();
            _LastSourcesAndTargetsUpdateTimer = MyAPIGateway.Session.ElapsedPlayTime;
        }
    }

    /// <summary>
    /// </summary>
    /// <param name="newAction"></param>
    public static void AddAsyncAction(Action newAction)
    {
        // i dont really care where its executed
        // and i dont want to cause headache for havok if it will remain on some random threads
        Plugin.Torch.Invoke(newAction);
    }

    /// <summary>
    /// </summary>
    private void SyncModCommandReceived(ushort channelId, byte[] bytes, ulong fromId, bool fromServer)
    {
    }

    /// <summary>
    /// </summary>
    private void SyncModDataRequestSend()
    {
        if (MyAPIGateway.Session.IsServer) return;

        var msgSnd = new MsgModDataRequest();
        msgSnd.SteamId = MyAPIGateway.Session.Player != null ? MyAPIGateway.Session.Player.SteamUserId : 0;

        if (Mod.Log.ShouldLog(Logging.Level.Communication)) Mod.Log.Write(Logging.Level.Communication, "BuildAndRepairSystemMod: SyncModDataRequestSend SteamId={0}", msgSnd.SteamId);
        MyAPIGateway.Multiplayer.SendMessageToServer(MSGID_MOD_DATAREQUEST, MyAPIGateway.Utilities.SerializeToBinary(msgSnd));
    }

    /// <summary>
    /// </summary>
    private void SyncModDataRequestReceived(ushort channelId, byte[] bytes, ulong fromId, bool fromServer)
    {
        // var msgRcv = MyAPIGateway.Utilities.SerializeFromBinary<MsgModDataRequest>(bytes);
        if (Mod.Log.ShouldLog(Logging.Level.Communication)) Mod.Log.Write(Logging.Level.Communication, "BuildAndRepairSystemMod: SyncModDataRequestReceived SteamId={0}", fromId);
        SyncModSettingsSend(fromId);
    }

    /// <summary>
    /// </summary>
    private void SyncModSettingsSend(ulong steamId)
    {
        if (!MyAPIGateway.Session.IsServer) return;
        var msgSnd = new MsgModSettings();
        msgSnd.Settings = Settings;
        if (Mod.Log.ShouldLog(Logging.Level.Communication)) Mod.Log.Write(Logging.Level.Communication, "BuildAndRepairSystemMod: SyncModSettingsSend SteamId={0}", steamId);
        if (!MyAPIGateway.Multiplayer.SendMessageTo(MSGID_MOD_SETTINGS, MyAPIGateway.Utilities.SerializeToBinary(msgSnd), steamId))
            if (Mod.Log.ShouldLog(Logging.Level.Error))
                Mod.Log.Write(Logging.Level.Error, "BuildAndRepairSystemMod: SyncModSettingsSend failed");
    }

    /// <summary>
    /// </summary>
    private void SyncModSettingsReceived(ushort channelId, byte[] bytes, ulong fromId, bool fromServer)
    {
        try
        {
            var msgRcv = MyAPIGateway.Utilities.SerializeFromBinary<MsgModSettings>(bytes);
            SyncModSettings.AdjustSettings(msgRcv.Settings);
            Settings = msgRcv.Settings;
            SettingsValid = true;
            if (Mod.Log.ShouldLog(Logging.Level.Communication)) Mod.Log.Write(Logging.Level.Communication, "BuildAndRepairSystemMod: SyncModSettingsReceived");
            SettingsChanged();
        }
        catch (Exception ex)
        {
            Mod.Log.Write(Logging.Level.Error, "BuildAndRepairSystemMod: SyncModSettingsReceived Exception:{0}", ex);
        }
    }

    /// <summary>
    /// </summary>
    public static void SyncBlockDataRequestSend(NanobotBuildAndRepairSystemBlock block)
    {
        if (MyAPIGateway.Session.IsServer) return;

        var msgSnd = new MsgBlockDataRequest();
        if (MyAPIGateway.Session.Player != null)
            msgSnd.SteamId = MyAPIGateway.Session.Player.SteamUserId;
        else
            msgSnd.SteamId = 0;
        msgSnd.EntityId = block.Entity.EntityId;

        if (Mod.Log.ShouldLog(Logging.Level.Communication))
            Mod.Log.Write(Logging.Level.Communication, "BuildAndRepairSystemMod: SyncBlockDataRequestSend SteamId={0} EntityId={1}/{2}", msgSnd.SteamId, msgSnd.EntityId,
                          Logging.BlockName(block.Entity, Logging.BlockNameOptions.None));
        MyAPIGateway.Multiplayer.SendMessageToServer(MSGID_BLOCK_DATAREQUEST, MyAPIGateway.Utilities.SerializeToBinary(msgSnd));
    }

    /// <summary>
    /// </summary>
    private void SyncBlockDataRequestReceived(ushort channelId, byte[] bytes, ulong fromId, bool fromServer)
    {
        var msgRcv = MyAPIGateway.Utilities.SerializeFromBinary<MsgBlockDataRequest>(bytes);

        msgRcv.SteamId = fromId;

        if (BuildAndRepairSystems.TryGetValue(msgRcv.EntityId, out var system))
        {
            if (Mod.Log.ShouldLog(Logging.Level.Communication))
                Mod.Log.Write(Logging.Level.Communication, "BuildAndRepairSystemMod: SyncBlockDataRequestReceived SteamId={0} EntityId={1}/{2}", msgRcv.SteamId, msgRcv.EntityId,
                              Logging.BlockName(system.Entity, Logging.BlockNameOptions.None));
            SyncBlockSettingsSend(msgRcv.SteamId, system);
            SyncBlockStateSend(msgRcv.SteamId, system);
        }
        else
        {
            if (Mod.Log.ShouldLog(Logging.Level.Error)) Mod.Log.Write(Logging.Level.Error, "BuildAndRepairSystemMod: SyncBlockDataRequestReceived for unknown system SteamId{0} EntityId={1}", msgRcv.SteamId, msgRcv.EntityId);
        }
    }

    /// <summary>
    /// </summary>
    public static void SyncBlockSettingsSend(ulong steamId, NanobotBuildAndRepairSystemBlock block)
    {
        var msgSnd = new MsgBlockSettings();
        msgSnd.EntityId = block.Entity.EntityId;
        msgSnd.Settings = block.Settings.GetTransmit();

        var res = false;
        if (MyAPIGateway.Session.IsServer)
        {
            if (steamId == 0)
            {
                if (Mod.Log.ShouldLog(Logging.Level.Communication))
                    Mod.Log.Write(Logging.Level.Communication, "BuildAndRepairSystemMod: SyncBlockSettingsSend To Others EntityId={0}/{1}", block.Entity.EntityId, Logging.BlockName(block.Entity, Logging.BlockNameOptions.None));
                res = MyAPIGateway.Multiplayer.SendMessageToOthers(MSGID_BLOCK_SETTINGS_FROM_SERVER, MyAPIGateway.Utilities.SerializeToBinary(msgSnd));
            }
            else
            {
                if (Mod.Log.ShouldLog(Logging.Level.Communication))
                    Mod.Log.Write(Logging.Level.Communication, "BuildAndRepairSystemMod: SyncBlockSettingsSend To SteamId={2} EntityId={0}/{1}", block.Entity.EntityId, Logging.BlockName(block.Entity, Logging.BlockNameOptions.None),
                                  steamId);
                res = MyAPIGateway.Multiplayer.SendMessageTo(MSGID_BLOCK_SETTINGS_FROM_SERVER, MyAPIGateway.Utilities.SerializeToBinary(msgSnd), steamId);
            }
        }
        else
        {
            if (Mod.Log.ShouldLog(Logging.Level.Communication))
                Mod.Log.Write(Logging.Level.Communication, "BuildAndRepairSystemMod: SyncBlockSettingsSend To Server EntityId={0}/{1} to Server", block.Entity.EntityId,
                              Logging.BlockName(block.Entity, Logging.BlockNameOptions.None));
            res = MyAPIGateway.Multiplayer.SendMessageToServer(MSGID_BLOCK_SETTINGS_FROM_CLIENT, MyAPIGateway.Utilities.SerializeToBinary(msgSnd));
        }

        if (!res && Mod.Log.ShouldLog(Logging.Level.Error)) Mod.Log.Write(Logging.Level.Error, "BuildAndRepairSystemMod: SyncBlockSettingsSend failed", Logging.BlockName(block.Entity, Logging.BlockNameOptions.None));
    }

    /// <summary>
    /// </summary>
    private void SyncBlockSettingsReceived(ushort channelId, byte[] bytes, ulong fromId, bool fromServer)
    {
        try
        {
            var msgRcv = MyAPIGateway.Utilities.SerializeFromBinary<MsgBlockSettings>(bytes);

            if (BuildAndRepairSystems.TryGetValue(msgRcv.EntityId, out var system))
            {
                var fromIdentityId = Sync.Players.TryGetIdentityId(fromId);
                if (!system.Welder.GetUserRelationToOwner(fromIdentityId).IsFriendly())
                    return;
                
                if (Mod.Log.ShouldLog(Logging.Level.Communication))
                    Mod.Log.Write(Logging.Level.Communication, "BuildAndRepairSystemMod: SyncBlockSettingsReceived EntityId={0}/{1}", msgRcv.EntityId, Logging.BlockName(system.Entity, Logging.BlockNameOptions.None));
                system.Settings.AssignReceived(msgRcv.Settings, system.BlockWeldPriority, system.BlockGrindPriority, system.ComponentCollectPriority);
                system.SettingsChanged();
                if (MyAPIGateway.Session.IsServer)
                    SyncBlockSettingsSend(0, system);
            }
            else
            {
                if (Mod.Log.ShouldLog(Logging.Level.Error)) Mod.Log.Write(Logging.Level.Error, "BuildAndRepairSystemMod: SyncBlockSettingsReceived for unknown system EntityId={0}", msgRcv.EntityId);
            }
        }
        catch (Exception ex)
        {
            Mod.Log.Write(Logging.Level.Error, "BuildAndRepairSystemMod: SyncBlockSettingsReceived Exception:{0}", ex);
        }
    }

    /// <summary>
    /// </summary>
    public static void SyncBlockStateSend(ulong steamId, NanobotBuildAndRepairSystemBlock system)
    {
        if (!MyAPIGateway.Session.IsServer) return;
        if (!MyAPIGateway.Multiplayer.MultiplayerActive) return;

        var msgSnd = new MsgBlockState();
        msgSnd.EntityId = system.Entity.EntityId;
        msgSnd.State = system.State.GetTransmit();

        var res = false;
        if (steamId == 0)
        {
            if (Mod.Log.ShouldLog(Logging.Level.Communication))
                Mod.Log.Write(Logging.Level.Communication, "BuildAndRepairSystemMod: SyncBlockStateSend to others EntityId={0}/{1}, State={2}", system.Entity.EntityId, Logging.BlockName(system.Entity, Logging.BlockNameOptions.None),
                              msgSnd.State.ToString());
            res = MyAPIGateway.Multiplayer.SendMessageToOthers(MSGID_BLOCK_STATE_FROM_SERVER, MyAPIGateway.Utilities.SerializeToBinary(msgSnd));
        }
        else
        {
            if (Mod.Log.ShouldLog(Logging.Level.Communication))
                Mod.Log.Write(Logging.Level.Communication, "BuildAndRepairSystemMod: SyncBlockStateSend to SteamId={0} EntityId={1}/{2}, State={3}", steamId, system.Entity.EntityId,
                              Logging.BlockName(system.Entity, Logging.BlockNameOptions.None), msgSnd.State.ToString());
            res = MyAPIGateway.Multiplayer.SendMessageTo(MSGID_BLOCK_STATE_FROM_SERVER, MyAPIGateway.Utilities.SerializeToBinary(msgSnd), steamId);
        }

        if (!res && Mod.Log.ShouldLog(Logging.Level.Error)) Mod.Log.Write(Logging.Level.Error, "BuildAndRepairSystemMod: SyncBlockStateSend Failed");
    }

    private void SyncBlockStateReceived(ushort channelId, byte[] bytes, ulong fromId, bool fromServer)
    {
        try
        {
            var msgRcv = MyAPIGateway.Utilities.SerializeFromBinary<MsgBlockState>(bytes);

            if (BuildAndRepairSystems.TryGetValue(msgRcv.EntityId, out var system))
            {
                if (Mod.Log.ShouldLog(Logging.Level.Communication))
                    Mod.Log.Write(Logging.Level.Communication, "BuildAndRepairSystemMod: SyncBlockStateReceived EntityId={0}/{1}, State={2}", system.Entity.EntityId, Logging.BlockName(system.Entity, Logging.BlockNameOptions.None),
                                  msgRcv.State.ToString());
                system.State.AssignReceived(msgRcv.State);
            }
            else
            {
                if (Mod.Log.ShouldLog(Logging.Level.Error)) Mod.Log.Write(Logging.Level.Error, "BuildAndRepairSystemMod: SyncBlockStateReceived for unknown system EntityId={0}", msgRcv.EntityId);
            }
        }
        catch (Exception ex)
        {
            Mod.Log.Write(Logging.Level.Error, "BuildAndRepairSystemMod: SyncBlockStateReceived Exception:{0}", ex);
        }
    }
}