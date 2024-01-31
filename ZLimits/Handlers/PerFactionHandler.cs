using Sandbox.Game.Entities;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.World;

namespace ZLimits.Handlers;

public class PerFactionHandler : ILimitsHandler
{
    public bool ShouldCountForPlayer => true;

    public void IncreaseBlocksBuilt(MyIdentity? identity, string type, MyCubeGrid? grid, LimitsChangeToken token)
    {
        var faction = MySession.Static.Factions.GetPlayerFaction(identity!.IdentityId);

        if (faction == null) return;
        
        foreach (var factionMember in faction.Members.Keys.Where(b => b != identity.IdentityId))
        {
            token.IncreaseBlocksBuilt(Sync.Players.TryGetIdentity(factionMember), type);
        }
    }

    public void DecreaseBlocksBuilt(MyIdentity? identity, string type, MyCubeGrid? grid, LimitsChangeToken token)
    {
        var faction = MySession.Static.Factions.GetPlayerFaction(identity!.IdentityId);

        if (faction == null) return;
        
        foreach (var factionMember in faction.Members.Keys.Where(b => b != identity.IdentityId))
        {
            token.DecreaseBlocksBuilt(Sync.Players.TryGetIdentity(factionMember), type);
        }
    }
}