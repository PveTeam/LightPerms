using System.Text;
using Torch.API.Managers;
using Torch.Commands;
using Torch.Mod;
using Torch.Mod.Messages;
namespace LightPerms.TorchCommands;

[Category("lp")]
public class Commands : CommandModule
{
    [Command("get commands", "Get all command permissions")]
    public void GetCommands()
    {
        var sb = new StringBuilder();

        if (Context.Player is null)
            sb.AppendLine("Available commands:");
        
        foreach (var commandNode in Context.Torch.CurrentSession.Managers.GetManager<CommandManager>().Commands.WalkTree()
                     .Where(b => b.IsCommand))
        {
            sb.Append(" ".PadRight(4)).AppendLine(commandNode.Command.GetPermissionString());
        }
        
        if (Context.Player is null)
            Context.Respond(sb.ToString());
        else
            ModCommunication.SendMessageTo(new DialogMessage("Light Perms", "Available commands:", sb.ToString()), Context.Player.SteamUserId);
    }
}
