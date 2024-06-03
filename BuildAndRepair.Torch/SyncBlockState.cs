using ProtoBuf;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using VRageMath;

namespace BuildAndRepair.Torch;

/// <summary>
///     Current State of block
/// </summary>
[ProtoContract(SkipConstructor = true, UseProtoMembersOnly = true)]
public class SyncBlockState
{
    public const int MaxSyncItems = 20;
    private VRage.Game.ModAPI.IMySlimBlock _CurrentGrindingBlock;
    private bool _CurrentTransportIsPick;
    private TimeSpan _CurrentTransportStartTime = TimeSpan.Zero;

    private Vector3D? _CurrentTransportTarget;
    private TimeSpan _CurrentTransportTime = TimeSpan.Zero;
    private VRage.Game.ModAPI.IMySlimBlock _CurrentWeldingBlock;
    private bool _Grinding;
    private bool _InventoryFull;
    private Vector3D? _LastTransportTarget;
    private bool _LimitsExceeded;
    private List<SyncComponents> _MissingComponentsSync;
    private bool _NeedGrinding;
    private bool _NeedWelding;
    private List<SyncTargetEntityData> _PossibleFloatingTargetsSync;
    private List<SyncTargetEntityData> _PossibleGrindTargetsSync;
    private List<SyncTargetEntityData> _PossibleWeldTargetsSync;
    private bool _Ready;
    private bool _Transporting;
    private bool _Welding;

    public SyncBlockState()
    {
        MissingComponents = new();
        PossibleWeldTargets = [];
        PossibleGrindTargets = [];
        PossibleFloatingTargets = [];
    }

    public bool Changed { get; private set; }

    [ProtoMember(1)]
    public bool Ready
    {
        get => _Ready;
        set
        {
            if (value != _Ready)
            {
                _Ready = value;
                Changed = true;
            }
        }
    }

    [ProtoMember(2)]
    public bool Welding
    {
        get => _Welding;
        set
        {
            if (value != _Welding)
            {
                _Welding = value;
                Changed = true;
            }
        }
    }

    [ProtoMember(3)]
    public bool NeedWelding
    {
        get => _NeedWelding;
        set
        {
            if (value != _NeedWelding)
            {
                _NeedWelding = value;
                Changed = true;
            }
        }
    }

    [ProtoMember(4)]
    public bool Grinding
    {
        get => _Grinding;
        set
        {
            if (value != _Grinding)
            {
                _Grinding = value;
                Changed = true;
            }
        }
    }

    [ProtoMember(5)]
    public bool NeedGrinding
    {
        get => _NeedGrinding;
        set
        {
            if (value != _NeedGrinding)
            {
                _NeedGrinding = value;
                Changed = true;
            }
        }
    }

    [ProtoMember(6)]
    public bool Transporting
    {
        get => _Transporting;
        set
        {
            if (value != _Transporting)
            {
                _Transporting = value;
                Changed = true;
            }
        }
    }

    [ProtoMember(7)]
    public TimeSpan LastTransmitted { get; set; }

    public VRage.Game.ModAPI.IMySlimBlock CurrentWeldingBlock
    {
        get => _CurrentWeldingBlock;
        set
        {
            if (value != _CurrentWeldingBlock)
            {
                _CurrentWeldingBlock = value;
                Changed = true;
            }
        }
    }

    [ProtoMember(10)]
    public SyncEntityId CurrentWeldingBlockSync
    {
        get => SyncEntityId.GetSyncId(_CurrentWeldingBlock);
        set => CurrentWeldingBlock = SyncEntityId.GetItemAsSlimBlock(value);
    }

    public VRage.Game.ModAPI.IMySlimBlock CurrentGrindingBlock
    {
        get => _CurrentGrindingBlock;
        set
        {
            if (value != _CurrentGrindingBlock)
            {
                _CurrentGrindingBlock = value;
                Changed = true;
            }
        }
    }

    [ProtoMember(15)]
    public SyncEntityId CurrentGrindingBlockSync
    {
        get => SyncEntityId.GetSyncId(_CurrentGrindingBlock);
        set => CurrentGrindingBlock = SyncEntityId.GetItemAsSlimBlock(value);
    }

    [ProtoMember(16)]
    public Vector3D? CurrentTransportTarget
    {
        get => _CurrentTransportTarget;
        set
        {
            if (value != _CurrentTransportTarget)
            {
                _CurrentTransportTarget = value;
                Changed = true;
            }
        }
    }

    [ProtoMember(17)]
    public Vector3D? LastTransportTarget
    {
        get => _LastTransportTarget;
        set
        {
            if (value != _LastTransportTarget)
            {
                _LastTransportTarget = value;
                Changed = true;
            }
        }
    }

    [ProtoMember(18)]
    public bool CurrentTransportIsPick
    {
        get => _CurrentTransportIsPick;
        set
        {
            if (value != _CurrentTransportIsPick)
            {
                _CurrentTransportIsPick = value;
                Changed = true;
            }
        }
    }

    [ProtoMember(19)]
    public TimeSpan CurrentTransportTime
    {
        get => _CurrentTransportTime;
        set
        {
            if (value != _CurrentTransportTime)
            {
                _CurrentTransportTime = value;
                Changed = true;
            }
        }
    }

    [ProtoMember(20)]
    public TimeSpan CurrentTransportStartTime
    {
        get => _CurrentTransportStartTime;
        set
        {
            if (value != _CurrentTransportStartTime)
            {
                _CurrentTransportStartTime = value;
                Changed = true;
            }
        }
    }

    public DefinitionIdHashDictionary MissingComponents { get; }

    [ProtoMember(21)]
    public List<SyncComponents> MissingComponentsSync
    {
        get
        {
            if (_MissingComponentsSync == null)
            {
                if (MissingComponents != null) _MissingComponentsSync = MissingComponents.GetSyncList();
                else _MissingComponentsSync = [];
            }

            return _MissingComponentsSync;
        }
    }

    [ProtoMember(22)]
    public bool InventoryFull
    {
        get => _InventoryFull;
        set
        {
            if (value != _InventoryFull)
            {
                _InventoryFull = value;
                Changed = true;
            }
        }
    }

    [ProtoMember(23)]
    public bool LimitsExceeded
    {
        get => _LimitsExceeded;
        set
        {
            if (value != _LimitsExceeded)
            {
                _LimitsExceeded = value;
                Changed = true;
            }
        }
    }

    public TargetBlockDataHashList PossibleWeldTargets { get; }

    [ProtoMember(30)]
    public List<SyncTargetEntityData> PossibleWeldTargetsSync
    {
        get
        {
            if (_PossibleWeldTargetsSync == null)
            {
                if (PossibleWeldTargets != null) _PossibleWeldTargetsSync = PossibleWeldTargets.GetSyncList();
                else _PossibleWeldTargetsSync = [];
            }

            return _PossibleWeldTargetsSync;
        }
    }

    public TargetBlockDataHashList PossibleGrindTargets { get; }

    [ProtoMember(35)]
    public List<SyncTargetEntityData> PossibleGrindTargetsSync
    {
        get
        {
            if (_PossibleGrindTargetsSync == null)
            {
                if (PossibleGrindTargets != null) _PossibleGrindTargetsSync = PossibleGrindTargets.GetSyncList();
                else _PossibleGrindTargetsSync = [];
            }

            return _PossibleGrindTargetsSync;
        }
    }

    public TargetEntityDataHashList PossibleFloatingTargets { get; }

    [ProtoMember(36)]
    public List<SyncTargetEntityData> PossibleFloatingTargetsSync
    {
        get
        {
            if (_PossibleFloatingTargetsSync == null)
            {
                if (PossibleFloatingTargets != null) _PossibleFloatingTargetsSync = PossibleFloatingTargets.GetSyncList();
                else _PossibleFloatingTargetsSync = [];
            }

            return _PossibleFloatingTargetsSync;
        }
    }

    public override string ToString()
    {
        return
            $"Ready={Ready}, Welding={Welding}/{NeedWelding}, Grinding={Grinding}/{NeedGrinding}, MissingComponentsCount={MissingComponentsSync?.Count ?? -1}, PossibleWeldTargetsCount={PossibleWeldTargetsSync?.Count ?? -1}, PossibleGrindTargetsCount={PossibleGrindTargetsSync?.Count ?? -1}, PossibleFloatingTargetsCount={PossibleFloatingTargetsSync?.Count ?? -1}, CurrentWeldingBlock={Logging.BlockName(CurrentWeldingBlock, Logging.BlockNameOptions.None)}, CurrentGrindingBlock={Logging.BlockName(CurrentGrindingBlock, Logging.BlockNameOptions.None)}, CurrentTransportTarget={CurrentTransportTarget}";
    }

    internal void HasChanged()
    {
        Changed = true;
    }

    internal bool IsTransmitNeeded()
    {
        return Changed && MyAPIGateway.Session.ElapsedPlayTime.Subtract(LastTransmitted).TotalSeconds >= 2;
    }

    internal SyncBlockState GetTransmit()
    {
        _MissingComponentsSync = null;
        _PossibleWeldTargetsSync = null;
        _PossibleGrindTargetsSync = null;
        _PossibleFloatingTargetsSync = null;
        LastTransmitted = MyAPIGateway.Session.ElapsedPlayTime;
        Changed = false;
        return this;
    }

    internal void AssignReceived(SyncBlockState newState)
    {
        _Ready = newState.Ready;
        _Welding = newState.Welding;
        _NeedWelding = newState.NeedWelding;
        _Grinding = newState.Grinding;
        _NeedGrinding = newState.NeedGrinding;
        _InventoryFull = newState.InventoryFull;
        _LimitsExceeded = newState.LimitsExceeded;
        _CurrentTransportStartTime = MyAPIGateway.Session.ElapsedPlayTime - (newState.LastTransmitted - newState.CurrentTransportStartTime);
        _CurrentTransportTime = newState.CurrentTransportTime;

        _CurrentWeldingBlock = SyncEntityId.GetItemAsSlimBlock(newState.CurrentWeldingBlockSync);
        _CurrentGrindingBlock = SyncEntityId.GetItemAsSlimBlock(newState.CurrentGrindingBlockSync);
        _CurrentTransportTarget = newState.CurrentTransportTarget;
        _CurrentTransportIsPick = newState.CurrentTransportIsPick;

        MissingComponents.Clear();
        var missingComponentsSync = newState.MissingComponentsSync;
        if (missingComponentsSync != null)
            foreach (var item in missingComponentsSync)
                MissingComponents.Add(item.Component, item.Amount);

        PossibleWeldTargets.Clear();
        var possibleWeldTargetsSync = newState.PossibleWeldTargetsSync;
        if (possibleWeldTargetsSync != null)
            foreach (var item in possibleWeldTargetsSync)
                PossibleWeldTargets.Add(new(SyncEntityId.GetItemAsSlimBlock(item.Entity), item.Distance, 0));

        PossibleGrindTargets.Clear();
        var possibleGrindTargetsSync = newState.PossibleGrindTargetsSync;
        if (possibleGrindTargetsSync != null)
            foreach (var item in possibleGrindTargetsSync)
                PossibleGrindTargets.Add(new(SyncEntityId.GetItemAsSlimBlock(item.Entity), item.Distance, 0));

        PossibleFloatingTargets.Clear();
        var possibleFloatingTargetsSync = newState.PossibleFloatingTargetsSync;
        if (possibleFloatingTargetsSync != null)
            foreach (var item in possibleFloatingTargetsSync)
                PossibleFloatingTargets.Add(new(SyncEntityId.GetItemAs<MyFloatingObject>(item.Entity), item.Distance));

        Changed = true;
    }

    internal void ResetChanged()
    {
        Changed = false;
    }
}