namespace BuildAndRepair.Torch;

[Flags]
public enum AutoGrindRelation
{
    NoOwnership = 0x0001,
    Owner = 0x0002,
    FactionShare = 0x0004,
    Neutral = 0x0008,
    Enemies = 0x0010
}