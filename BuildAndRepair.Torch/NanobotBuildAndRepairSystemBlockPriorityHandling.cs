using Sandbox.ModAPI;
using SpaceEngineers.Game.ModAPI;
using VRage.Game.ModAPI;

namespace BuildAndRepair.Torch;

public class NanobotBuildAndRepairSystemBlockPriorityHandling : PriorityHandling<PrioItem, IMySlimBlock>
{
    public NanobotBuildAndRepairSystemBlockPriorityHandling()
    {
        foreach (var item in Enum.GetValues(typeof(BlockClass)))
            Add(new(new((int)item, item.ToString()), true, true));
    }

    public override int GetItemKey(IMySlimBlock a, bool real)
    {
        var block = a.FatBlock;
        if (block == null) return (int)BlockClass.ArmorBlock;
        var functionalBlock = block as IMyFunctionalBlock;
        if (!real && functionalBlock != null && !functionalBlock.Enabled) return (int)BlockClass.ArmorBlock; //Switched off -> handle as structural block (if logical class is asked)

        if (block is IMyShipWelder && block.BlockDefinition.SubtypeName.Contains("NanobotBuildAndRepairSystem")) return (int)BlockClass.AutoRepairSystem;
        if (block is IMyShipController) return (int)BlockClass.ShipController;
        if (block is IMyThrust || block is IMyWheel || block is IMyMotorRotor) return (int)BlockClass.Thruster;
        if (block is IMyGyro) return (int)BlockClass.Gyroscope;
        if (block is IMyCargoContainer) return (int)BlockClass.CargoContainer;
        if (block is IMyConveyor || a.FatBlock is IMyConveyorSorter || a.FatBlock is IMyConveyorTube) return (int)BlockClass.Conveyor;
        if (block is IMyUserControllableGun) return (int)BlockClass.ControllableGun;
        if (block is IMyWarhead) return (int)BlockClass.ControllableGun;
        if (block is IMyPowerProducer) return (int)BlockClass.PowerBlock;
        if (block is IMyProgrammableBlock) return (int)BlockClass.ProgrammableBlock;
        if (block is IMyTimerBlock) return (int)BlockClass.ProgrammableBlock;
        if (block is IMyProjector) return (int)BlockClass.Projector;
        if (block is IMyDoor) return (int)BlockClass.Door;
        if (block is IMyProductionBlock) return (int)BlockClass.ProductionBlock;
        if (functionalBlock != null) return (int)BlockClass.FunctionalBlock;

        return (int)BlockClass.ArmorBlock;
    }

    public override string GetItemAlias(IMySlimBlock a, bool real)
    {
        var key = GetItemKey(a, real);
        return ((BlockClass)key).ToString();
    }
}