using Sandbox.Game.World;
using ZLimits.Managers;

namespace ZLimits;

public readonly ref struct LimitsChangeToken(MyBlockLimits ownerLimits, MyIdentity? changeOwner, bool isNexusSupported)
{
    public void IncreaseBlocksBuilt(string type)
    {
        IncreaseBlocksBuilt(ownerLimits, type, changeOwner);
    }
    public void DecreaseBlocksBuilt(string type)
    {
        DecreaseBlocksBuilt(ownerLimits, type, changeOwner);
    }
        
    public void IncreaseBlocksBuilt(MyIdentity identity, string type)
    {
        IncreaseBlocksBuilt(identity.BlockLimits, type, identity);
    }
    public void DecreaseBlocksBuilt(MyIdentity identity, string type)
    {
        DecreaseBlocksBuilt(identity.BlockLimits, type, identity);
    }

    private void DecreaseBlocksBuilt(MyBlockLimits limits, string type, MyIdentity? identity)
    {
        var data = limits.BlockTypeBuilt.GetOrAdd(type, static b => new() {BlockPairName = b});;
        
        if (data.BlocksBuilt == 0) 
            return;
        
        Interlocked.Decrement(ref data.BlocksBuilt);
        data.Dirty = 1;
        
        if (isNexusSupported && identity is not null)
        {
            LimitsManager.SendSyncMessage(false, identity, type);
        }
    }
    private void IncreaseBlocksBuilt(MyBlockLimits limits, string type, MyIdentity? identity)
    {
        var data = limits.BlockTypeBuilt.GetOrAdd(type, static b => new() {BlockPairName = b});;
        
        Interlocked.Increment(ref data.BlocksBuilt);
        data.Dirty = 1;
        
        if (isNexusSupported && identity is not null)
        {
            LimitsManager.SendSyncMessage(true, identity, type);
        }
    }
}