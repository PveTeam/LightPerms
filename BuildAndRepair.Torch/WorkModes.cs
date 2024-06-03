namespace BuildAndRepair.Torch;

[Flags]
public enum WorkModes
{
    /// <summary>
    ///     Grind only if nothing to weld
    /// </summary>
    WeldBeforeGrind = 0x0001,

    /// <summary>
    ///     Weld onyl if nothing to grind
    /// </summary>
    GrindBeforeWeld = 0x0002,

    /// <summary>
    ///     Grind only if nothing to weld or
    ///     build waiting for missing items
    /// </summary>
    GrindIfWeldGetStuck = 0x0004,

    /// <summary>
    ///     Only welding is allowed
    /// </summary>
    WeldOnly = 0x0008,

    /// <summary>
    ///     Only grinding is allowed
    /// </summary>
    GrindOnly = 0x0010
}