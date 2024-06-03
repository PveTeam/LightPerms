using VRage.Game;

namespace BuildAndRepair.Torch;

public class NanobotBuildAndRepairSystemComponentPriorityHandling : PriorityHandling<PrioItem, MyDefinitionId>
{
    public NanobotBuildAndRepairSystemComponentPriorityHandling()
    {
        foreach (var item in Enum.GetValues(typeof(ComponentClass)))
            Add(new(new((int)item, item.ToString()), true, true));
    }

    public override int GetItemKey(MyDefinitionId a, bool real)
    {
        if (a.TypeId == typeof(MyObjectBuilder_Ingot))
        {
            if (a.SubtypeName == "Stone") return (int)ComponentClass.Gravel;
            return (int)ComponentClass.Ingot;
        }

        if (a.TypeId == typeof(MyObjectBuilder_Ore))
        {
            if (a.SubtypeName == "Stone") return (int)ComponentClass.Stone;
            return (int)ComponentClass.Ore;
        }

        return (int)ComponentClass.Material;
    }

    public override string GetItemAlias(MyDefinitionId a, bool real)
    {
        var key = GetItemKey(a, real);
        return ((ComponentClass)key).ToString();
    }
}