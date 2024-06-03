namespace BuildAndRepair.Torch;

public class TargetBlockData : TargetEntityData
{
    [Flags]
    public enum AttributeFlags
    {
        Projected = 0x0001,
        Autogrind = 0x0100
    }

    public TargetBlockData(VRage.Game.ModAPI.IMySlimBlock block, double distance, AttributeFlags attributes) : base(block?.FatBlock, distance)
    {
        Block = block;
        Attributes = attributes;
    }

    public VRage.Game.ModAPI.IMySlimBlock Block { get; internal set; }
    public AttributeFlags Attributes { get; internal set; }
}