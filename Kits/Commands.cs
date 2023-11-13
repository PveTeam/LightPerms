using Sandbox.Game.World;
using Torch.API.Managers;
using Torch.Commands;
using Torch.Commands.Permissions;
using VRage.Game.ModAPI;
namespace Kits;

public class Commands : CommandModule
{
    [Command("kit")]
    [Permission(MyPromoteLevel.None)]
    public void GetKit(string name)
    {
        var manager = Context.Torch.CurrentSession.Managers.GetManager<IKitManager>();
        var player = (MyPlayer)Context.Player;
        
        if (!manager.CanGiveKit(player, name, out var reason))
        {
            Context.Respond(reason, "Error");
            return;
        }
        manager.GiveKit(player, player.Character.GetInventoryBase(), name);
        Context.Respond($"You have got kit {name}");
    } 
}
