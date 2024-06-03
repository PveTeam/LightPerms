using System.Xml.Serialization;
using ProtoBuf;

namespace BuildAndRepair.Torch;

/// <summary>
///     The settings for Mod
/// </summary>
[ProtoContract(SkipConstructor = true, UseProtoMembersOnly = true)]
public class SyncModSettings
{
    private const int CurrentSettingsVersion = 6;

    public SyncModSettings()
    {
        DisableLocalization = false;
        LogLevel = Logging.Level.Error; //Default
        MaxBackgroundTasks = NanobotBuildAndRepairSystemMod.MaxBackgroundTasks_Default;
        TargetsUpdateInterval = TimeSpan.FromSeconds(10);
        SourcesUpdateInterval = TimeSpan.FromSeconds(60);
        FriendlyDamageTimeout = TimeSpan.FromSeconds(60);
        FriendlyDamageCleanup = TimeSpan.FromSeconds(10);
        Range = NanobotBuildAndRepairSystemBlock.WELDER_RANGE_DEFAULT_IN_M;
        MaximumOffset = NanobotBuildAndRepairSystemBlock.WELDER_OFFSET_MAX_DEFAULT_IN_M;
        MaximumRequiredElectricPowerStandby = NanobotBuildAndRepairSystemBlock.WELDER_REQUIRED_ELECTRIC_POWER_STANDBY_DEFAULT;
        MaximumRequiredElectricPowerTransport = NanobotBuildAndRepairSystemBlock.WELDER_REQUIRED_ELECTRIC_POWER_TRANSPORT_DEFAULT;
        Welder = new();
    }

    [ProtoMember(2000)] [XmlElement]
    public int Version { get; set; } = CurrentSettingsVersion;

    [XmlElement]
    public bool DisableLocalization { get; set; }

    [ProtoMember(1)] [XmlElement]
    public Logging.Level LogLevel { get; set; }

    [XmlIgnore]
    public TimeSpan SourcesUpdateInterval { get; set; }

    [XmlIgnore]
    public TimeSpan TargetsUpdateInterval { get; set; }

    [XmlIgnore]
    public TimeSpan FriendlyDamageTimeout { get; set; }

    [XmlIgnore]
    public TimeSpan FriendlyDamageCleanup { get; set; }

    [ProtoMember(2)] [XmlElement]
    public int Range { get; set; }

    [ProtoMember(3)] [XmlElement]
    public long SourcesAndTargetsUpdateIntervalTicks
    {
        get => TargetsUpdateInterval.Ticks;
        set
        {
            TargetsUpdateInterval = new(value);
            SourcesUpdateInterval = new(value * 6);
        }
    }

    [ProtoMember(4)] [XmlElement]
    public long FriendlyDamageTimeoutTicks
    {
        get => FriendlyDamageTimeout.Ticks;
        set => FriendlyDamageTimeout = new(value);
    }

    [ProtoMember(5)] [XmlElement]
    public long FriendlyDamageCleanupTicks
    {
        get => FriendlyDamageCleanup.Ticks;
        set => FriendlyDamageCleanup = new(value);
    }

    [ProtoMember(8)] [XmlElement]
    public float MaximumRequiredElectricPowerTransport { get; set; }

    [ProtoMember(9)] [XmlElement]
    public float MaximumRequiredElectricPowerStandby { get; set; }

    [ProtoMember(10)] [XmlElement]
    public SyncModSettingsWelder Welder { get; set; }

    [ProtoMember(20)] [XmlElement]
    public int MaxBackgroundTasks { get; set; }

    [ProtoMember(21)] [XmlElement]
    public int MaximumOffset { get; set; }

    public static bool AdjustSettings(SyncModSettings settings)
    {
        if (settings.Version >= CurrentSettingsVersion) return false;

        Mod.Log.Write("NanobotBuildAndRepairSystemSettings: Settings have old version: {0} update to {1}", settings.Version, CurrentSettingsVersion);

        if (settings.Version <= 0) settings.LogLevel = Logging.Level.Error;
        if (settings.Version <= 4 && settings.Welder.AllowedSearchModes == 0) settings.Welder.AllowedSearchModes = SearchModes.Grids | SearchModes.BoundingBox;
        if (settings.Version <= 4 && settings.Welder.AllowedWorkModes == 0)
            settings.Welder.AllowedWorkModes = WorkModes.WeldBeforeGrind | WorkModes.GrindBeforeWeld | WorkModes.GrindIfWeldGetStuck | WorkModes.WeldOnly | WorkModes.GrindOnly;
        if (settings.Version <= 4 && settings.Welder.WeldingMultiplier == 0) settings.Welder.WeldingMultiplier = 1;
        if (settings.Version <= 4 && settings.Welder.GrindingMultiplier == 0) settings.Welder.GrindingMultiplier = 1;
        if (settings.Version <= 5 && settings.Welder.AllowedGrindJanitorRelations == 0) settings.Welder.AllowedGrindJanitorRelations = AutoGrindRelation.NoOwnership | AutoGrindRelation.Enemies | AutoGrindRelation.Neutral;

        settings.Version = CurrentSettingsVersion;
        return true;
    }
}