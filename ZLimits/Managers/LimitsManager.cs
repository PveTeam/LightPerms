using System.Collections.Concurrent;
using System.Reflection;
using System.Reflection.Emit;
using Microsoft.Extensions.Configuration;
using Sandbox.Game;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.Localization;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.World;
using Torch.API;
using Torch.API.Session;
using Torch.Managers;
using Torch.Managers.PatchManager;
using Torch.Managers.PatchManager.MSIL;
using Torch.Mod;
using Torch.Mod.Messages;
using Torch.Utils;
using VRage;
using VRage.Audio;
using VRage.Game;
using VRage.Network;
using ZLimits.Extensions;
using ZLimits.Handlers;
using ZLimits.Punishers;

namespace ZLimits.Managers;

public class LimitsManager : Manager
{
    [ReflectedMethodInfo(typeof(MyBlockLimits), nameof(MyBlockLimits.IncreaseBlocksBuilt))]
    private static readonly MethodInfo IncreaseBlocksBuiltMethod = null!;
    
    [ReflectedMethodInfo(typeof(MyBlockLimits), nameof(MyBlockLimits.DecreaseBlocksBuilt))]
    private static readonly MethodInfo DecreaseBlocksBuiltMethod = null!;

    [ReflectedMethodInfo(typeof(LimitsManager), nameof(IncreaseBlocksBuiltTranspiler))]
    private static readonly MethodInfo IncreaseBlocksBuiltTranspilerMethod = null!;
    
    [ReflectedMethodInfo(typeof(LimitsManager), nameof(DecreaseBlocksBuiltTranspiler))]
    private static readonly MethodInfo DecreaseBlocksBuiltTranspilerMethod = null!;
        
    internal static readonly ConcurrentDictionary<MyIdentity, MyBlockLimits> BlockLimitsMap = new();
    internal static readonly ConcurrentDictionary<MyCubeGrid, MyBlockLimits> GridBlockLimitsMap = new();
    private static readonly ConcurrentDictionary<string, ILimitsHandler> Handlers = new();
    private static readonly ConcurrentDictionary<string, ILimitsPunisher> Punishers = new();
    public static LimitsManager Instance = null!;

    [Dependency] private readonly PatchManager _patchManager = null!;
    [Dependency] private readonly ConfigManager _configManager = null!;
    [Dependency] private readonly ITorchSessionManager _sessionManager = null!;

    private readonly List<LimitGroupInfo> _sessionLimits = [];
    
    public IReadOnlyCollection<LimitGroupInfo> SessionLimits => _sessionLimits;
    
    public LimitsConfig Config { get; set; } = null!;

    public LimitsManager(ITorchBase torch) : base(torch)
    {
        Instance = this;
        TryRegisterHandler<PerPlayerHandler>("PerPlayer");
        TryRegisterHandler<PerFactionHandler>("PerFaction");
        TryRegisterHandler<PerGridHandler>("PerGrid");
        TryRegisterPunisher<DisableBlockPunisher>("DisableBlock");
    }

    public override void Attach()
    {
        Config = _configManager.Configuration.Get<LimitsConfig>()!;
        
        _sessionManager.SessionStateChanged += SessionManagerOnSessionStateChanged;

        // TODO bring back nexus stuff
        // if (!Config.NexusSync || !NexusApi.IsRunningNexus()) return;
        // NexusApi.RegisterMessageHandler<LimitChangeMessage>(NexusSyncHandler);
        // Log.Info("Nexus Sync initialized");
        
        // MyCubeGrids.BlockBuilt += static(grid, block) =>
        // {
        //     if (!_gridBlockLimitsMap.TryGetValue(grid, out var limits))
        //         return;
        //     limits.IncreaseBlocksBuilt(block.BlockDefinition.BlockPairName, block.GetPcu(), grid);
        // };
        MyCubeGrids.BlockDestroyed += static(grid, block) =>
        {
            if (GridBlockLimitsMap.TryGetValue(grid, out var limits) &&
                limits.BlockTypeBuilt.ContainsKey(block.BlockDefinition.BlockPairName))
                limits.DecreaseBlocksBuilt(block.BlockDefinition.BlockPairName, block.GetPcu(), grid);
        };
    }

    private void SessionManagerOnSessionStateChanged(ITorchSession session, TorchSessionState newState)
    {
        if (newState == TorchSessionState.Loading)
            ApplyHooks();
    }


    private static void NexusSyncHandler(LimitChangeMessage message)
    {
#pragma warning disable 8620
        var limits = BlockLimitsMap.GetValueOrDefault(Sync.Players.TryGetIdentity(message.IdentityId), null);
#pragma warning restore 8620
            
        if (limits is null)
            return;
            
        if (message.IsIncrease)
        {
            IncreaseBlockBuiltHandler(limits, message.BlockType, null!);
        }
        else
        {
            DecreaseBlockBuiltHandler(limits, message.BlockType, null!);
        }
    }

    internal static void SendSyncMessage(bool isIncrease, MyIdentity identity, string type)
    {
        // NexusApi.SendMessageTo(new LimitChangeMessage
        // {
        //     IsIncrease = isIncrease,
        //     IdentityId = identity.IdentityId,
        //     BlockType = type
        // });
    }

    private void ApplyHooks()
    {
        var context = _patchManager.AcquireContext();
        context.GetPattern(IncreaseBlocksBuiltMethod)
            .Transpilers.Add(IncreaseBlocksBuiltTranspilerMethod);
        context.GetPattern(DecreaseBlocksBuiltMethod)
            .Transpilers.Add(DecreaseBlocksBuiltTranspilerMethod);
        _patchManager.Commit();
    }

    internal void LoadSessionLimits(MyObjectBuilder_Checkpoint checkpoint)
    {
        checkpoint.Settings.BlockLimitsEnabled = MyBlockLimitsEnabledEnum.PER_PLAYER;
        checkpoint.Settings.BlockTypeLimits = new();
        foreach (var limitGroup in Config.LimitGroups.Where(limitGroup => Handlers[limitGroup.Handler].ShouldCountForPlayer))
        {
            _sessionLimits.Add(limitGroup);
            foreach (var limit in limitGroup.Items)
            {
                checkpoint.Settings.BlockTypeLimits.Dictionary.Add(limit, limitGroup.Max);
            }
        }
    }

    private static IEnumerable<MsilInstruction> IncreaseBlocksBuiltTranspiler(IEnumerable<MsilInstruction> ins)
    {
        var found = false;
        foreach (var instruction in ins)
        {
            if (!found && instruction.OpCode == OpCodes.Ldloc_0)
            {
                found = true;
                var label = new MsilLabel();
                yield return new(OpCodes.Ldarg_3);
                yield return new MsilInstruction(OpCodes.Brfalse).InlineTarget(label);
                yield return new(OpCodes.Ldarg_0);
                yield return new(OpCodes.Ldarg_1);
                yield return new(OpCodes.Ldarg_3);
                yield return new MsilInstruction(OpCodes.Call).InlineValue(
                    new Action<MyBlockLimits, string, MyCubeGrid>(IncreaseBlockBuiltHandler).Method);
                yield return instruction.LabelWith(label);
                continue;
            }
            yield return instruction;
        }
    }
    
    private static IEnumerable<MsilInstruction> DecreaseBlocksBuiltTranspiler(IEnumerable<MsilInstruction> ins)
    {
        var found = false;
            
        foreach (var instruction in ins)
        {
            if (!found && instruction.OpCode == OpCodes.Ldloc_0)
            {
                found = true;
                var label = new MsilLabel();
                yield return new(OpCodes.Ldarg_3);
                yield return new MsilInstruction(OpCodes.Brfalse).InlineTarget(label);
                yield return new(OpCodes.Ldarg_0);
                yield return new(OpCodes.Ldarg_1);
                yield return new(OpCodes.Ldarg_3);
                yield return new MsilInstruction(OpCodes.Call).InlineValue(
                    new Action<MyBlockLimits, string, MyCubeGrid>(DecreaseBlockBuiltHandler).Method);
                yield return instruction.LabelWith(label);
                continue;
            }
            yield return instruction;
        }
    }

    private static void IncreaseBlockBuiltHandler(MyBlockLimits blockLimits, string blockType, MyCubeGrid cubeGrid)
    {
        var identity = BlockLimitsMap.FirstOrDefault(b => b.Value == blockLimits).Key;
        if (identity is null && !GridBlockLimitsMap.TryGetValue(cubeGrid, out blockLimits)) return;
            
        var limitGroupInfo = Instance.GetLimitsGroup(blockType, identity is not null ? "PerPlayer" : "PerGrid");
        if (limitGroupInfo is null)
            return;
        LimitsChangeToken token = new(blockLimits, identity, Instance.Config.NexusSync);
                
        var limitsHandler = Handlers[limitGroupInfo.Handler];
                
        limitsHandler.IncreaseBlocksBuilt(identity, blockType, cubeGrid, token);
        foreach (var s in limitGroupInfo.Items.Where(b => b != blockType))
        {
            token.IncreaseBlocksBuilt(s);
            limitsHandler.IncreaseBlocksBuilt(identity, blockType, cubeGrid, token);
        }
    }
        
    private static void DecreaseBlockBuiltHandler(MyBlockLimits blockLimits, string blockType, MyCubeGrid cubeGrid)
    {
        var identity = BlockLimitsMap.FirstOrDefault(b => b.Value == blockLimits).Key;
        if (identity is null && !GridBlockLimitsMap.TryGetValue(cubeGrid, out blockLimits)) return;
            
        var limitGroupInfo = Instance.GetLimitsGroup(blockType, identity is { } ? "PerPlayer" : "PerGrid");
        if (limitGroupInfo is null)
            return;
            
        LimitsChangeToken token = new(blockLimits, identity, Instance.Config.NexusSync);
                
        var limitsHandler = Handlers[limitGroupInfo.Handler];
                
        limitsHandler.DecreaseBlocksBuilt(identity, blockType, cubeGrid, token);
        foreach (var s in limitGroupInfo.Items.Where(b => b != blockType))
        {
            token.DecreaseBlocksBuilt(s);
            limitsHandler.DecreaseBlocksBuilt(identity, blockType, cubeGrid, token);
        }
    }

    public LimitGroupInfo? GetLimitsGroup(string pairName, string handlerId)
    {
        return Config.LimitGroups.SingleOrDefault(b => b.Handler == handlerId && b.Items.Contains(pairName));
    }

    public IEnumerable<LimitGroupInfo> GetLimitsGroups(string pairName)
    {
        return Config.LimitGroups.Where(b => b.Items.Contains(pairName));
    }

    private static MyBlockLimits CreateBlockLimits(MyCubeGrid grid)
    {
        var limits = new MyBlockLimits(0, 0);
        limits.BlockTypeBuilt.Clear();
        foreach (var s in Instance.Config.LimitGroups.Where(b => b.Handler.Contains("Grid")).SelectMany(b => b.Items))
        {
            limits.BlockTypeBuilt.GetOrAdd(s, static b => new() {BlockPairName = b});
        }

        return limits;
    }
    
    internal MyBlockLimits GetBlockLimits(MyCubeGrid grid)
    {
        return GridBlockLimitsMap.GetOrAdd(grid, CreateBlockLimits);
    }

    internal static void NotifyLimitsReached(long? identityId = null)
    {
        //TODO client language support 
        ModCommunication.SendMessageTo(new NotificationMessage(MyTexts.GetString(MySpaceTexts.NotificationLimitsPerBlockType), 5000, MyFontEnum.Red), MyEventContext.Current.Sender.Value);
        
        identityId ??= Sync.Players.TryGetIdentityId(MyEventContext.Current.Sender.Value);
        MyVisualScriptLogicProvider.PlayHudSound(MyGuiSounds.HudUnable, identityId.Value);
    }

    public bool TryRegisterHandler<THandler>(string id) where THandler : class, ILimitsHandler, new()
    {
        return Handlers.TryAdd(id, new THandler());
    }
    public bool TryRegisterPunisher<TPunisher>(string id) where TPunisher : class, ILimitsPunisher, new()
    {
        return Punishers.TryAdd(id, new TPunisher());
    }
}