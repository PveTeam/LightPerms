using Sandbox.Game.Entities;
using Sandbox.Game.World;

namespace ZLimits;

public interface ILimitsPunisher
{
    public void SendWarning(MyIdentity identity, string type, uint current, uint max);
    public void ApplyPunishment(MyIdentity identity, MyCubeBlock[] overLimitedBlocks);
}