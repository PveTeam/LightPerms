using Sandbox.Game.Entities;
using Sandbox.Game.World;

namespace ZLimits.Handlers;

public class PerGridHandler : ILimitsHandler
{
    public bool ShouldCountForPlayer => false;

    public void IncreaseBlocksBuilt(MyIdentity identity, string type, MyCubeGrid? grid, LimitsChangeToken token)
    {
    }

    public void DecreaseBlocksBuilt(MyIdentity identity, string type, MyCubeGrid? grid, LimitsChangeToken token)
    {
    }
}