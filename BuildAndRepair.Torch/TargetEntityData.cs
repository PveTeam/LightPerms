using VRage.ModAPI;

namespace BuildAndRepair.Torch;

public class TargetEntityData
{
    public TargetEntityData(IMyEntity entity, double distance)
    {
        Entity = entity;
        Distance = distance;
        Ignore = false;
    }

    public IMyEntity Entity { get; internal set; }
    public double Distance { get; internal set; }
    public bool Ignore { get; set; }
}