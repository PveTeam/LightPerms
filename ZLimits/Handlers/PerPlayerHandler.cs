using Sandbox.Game.Entities;
using Sandbox.Game.World;

namespace ZLimits.Handlers;

public class PerPlayerHandler : ILimitsHandler
{
    public bool ShouldCountForPlayer => true;

    public void IncreaseBlocksBuilt(MyIdentity identity, string type, MyCubeGrid? grid, LimitsChangeToken token)
    {
    }

    public void DecreaseBlocksBuilt(MyIdentity identity, string type, MyCubeGrid? grid, LimitsChangeToken token)
    {
    }
}