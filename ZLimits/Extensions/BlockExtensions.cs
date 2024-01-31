using Sandbox.Game.Entities.Cube;

namespace ZLimits.Extensions;

public static class BlockExtensions
{
    public static int GetPcu(this MySlimBlock block)
    {
        return block.IsFunctional ? block.BlockDefinition.PCU : 1;
    }
}