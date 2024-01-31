using Sandbox.Game.Entities;
using Sandbox.Game.World;

namespace ZLimits;

public interface ILimitsHandler
{
    public bool ShouldCountForPlayer { get; }
    public void IncreaseBlocksBuilt(MyIdentity? identity, string type, MyCubeGrid? grid, LimitsChangeToken token);
    public void DecreaseBlocksBuilt(MyIdentity? identity, string type, MyCubeGrid? grid, LimitsChangeToken token);
}