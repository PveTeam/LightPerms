using NLog;
using Sandbox.Game.World;
using Torch.API.Managers;
using Torch.Commands;
using Torch.Commands.Permissions;
using VRage.Game.ModAPI;
namespace Kits;

public class Commands : CommandModule
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();
    
    [Command("kit")]
    [Permission(MyPromoteLevel.None)]
    public void GetKit(string? name = null)
    {
        var manager = Context.Torch.CurrentSession.Managers.GetManager<IKitManager>();
        
        if (name is null or [])
        {
            RespondKitList(manager, Context.Player as MyPlayer);
            return;
        }

        if (Context.Player is not MyPlayer player)
        {
            Context.Respond("You are not in game", "Error");
            return;
        }
        
        try
        {
            if (!manager.CanGiveKit(player, name, out var reason))
            {
                Context.Respond(reason, "Error");
                return;
            }

            manager.GiveKit(player, player.Character.GetInventoryBase(), name);
            Context.Respond($"You have got kit {name}");
        }
        catch (KitNotFoundException e)
        {
            Context.Respond(e.Message, "Error");
            Log.Error(e, "When processing command from {0}", Context.Player.DisplayName);
        }
    }
    
    private void RespondKitList(IKitManager manager, MyPlayer? player)
    {
        if (player is null)
        {
            Context.Respond($"Kits: {string.Join(" ", manager.ListKits())}");
            return;
        }
        
        Context.Respond($"Kits: {string.Join(" ", manager.ListKits(player).Select(b => b.reason is null ? b.kit.Name : $"{b.kit.Name} ({b.reason})"))}");
    }
}
