using System.Text;
using Sandbox.Definitions;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Character;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.World;
using Torch.API.Managers;
using Torch.Commands;
using Torch.Mod;
using Torch.Mod.Messages;
using VRage.Game;
using VRage.Game.ModAPI;
using ZLimits.Managers;

namespace ZLimits;

public class Commands : CommandModule
{
    private LimitsManager? _rLimits;
    private LimitsManager Manager => _rLimits ??= Context.Torch.Managers.GetManager<LimitsManager>();

    [Command("limits", "list all your limits (except grids)")]
    public void ListLimits()
    {
        if (Context.Player is null)
        {
            Context.Respond("Only in-game player allowed");
            return;
        }

        PrintLimits(Sync.Players.TryGetIdentity(Context.Player.IdentityId).BlockLimits, true);
    }
        
    [Command("limits grid", "list limits for current grid (you should look at block on grid with welder/grinder)")]
    public void ListGridLimits(string name = null!)
    {
        if (Context.Player is null)
        {
            Context.Respond("Only in-game player allowed");
            return;
        }

        MyCubeGrid? grid = null;
        if (string.IsNullOrEmpty(name) && !MyEntities.TryGetEntityById(((MyCharacter) Context.Player.Character).AimedGrid, out grid))
        {
            Context.Respond("Cannot find aimed grid! try looking with welder/grinder.", "Limits");
            return;
        }

        grid ??= MyEntities.GetEntities().OfType<MyCubeGrid>().FirstOrDefault(b =>
            b.DisplayName.Equals(name, StringComparison.InvariantCultureIgnoreCase));
            
        if (grid is null)
        {
            Context.Respond("Cannot find grid!", "Limits");
            return;
        }

        var owner = grid.BigOwners.FirstOrDefault();
        
        if (Context.Player.PromoteLevel < MyPromoteLevel.Admin && owner != 0 && !Context.Player.GetRelationTo(owner).IsFriendly())
        {
            Context.Respond("Cannot look at enemy limits!");
            return;
        }
        
        PrintLimits(LimitsManager.GridBlockLimitsMap.TryGetValue(grid, out var gridLimits) ? gridLimits : MyBlockLimits.Empty, false);
    }

    private void PrintLimits(MyBlockLimits limits, bool shouldCountPlayer)
    {
        var sb = new StringBuilder();
        foreach (var limitGroup in shouldCountPlayer ? Manager.SessionLimits : Manager.Config.LimitGroups.Where(b => b.Handler.Contains("Grid")))
        {
            sb.Append(string.Empty.PadRight(5, '-'));
            sb.Append(' ').Append(limitGroup.Name).Append(' ');
            sb.AppendLine(string.Empty.PadRight(5, '-'));
            sb.AppendLine(
                $"Blocks: {string.Join(" ", limitGroup.Items.Select(b => $"[{MyDefinitionManager.Static.TryGetDefinitionGroup(b).Any.DisplayNameText}]"))}");
            sb.AppendLine($"Type: {limitGroup.Handler}");
            sb.AppendLine($"Max: {limitGroup.Max}");
            sb.AppendLine(
                $"Current: {limits.BlockTypeBuilt.GetValueOrDefault(limitGroup.Items[0], null)?.BlocksBuilt ?? 0}");
        }

        ModCommunication.SendMessageTo(new DialogMessage("Limits", "Your block limits", sb.ToString()), Context.Player.SteamUserId);
    }
}