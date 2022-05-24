using System.Text.RegularExpressions;
using Torch.API.Managers;
using Torch.Commands;
using Torch.Commands.Permissions;
using VRage.Game.ModAPI;
namespace LightPerms.Discord;

[Category("lp")]
public class LpCommands : CommandModule
{
    private static readonly Regex DiscordTagRegex = new(@"^.{3,32}#[0-9]{4}$");
    
    [Command("link", "Link you with your discord account. (type `/lp-link` on server discord after)")]
    [Permission(MyPromoteLevel.None)]
    public void Link(string discordUsername)
    {
        if (!DiscordTagRegex.IsMatch(discordUsername))
        {
            Context.Respond("Please enter a valid discord username! (like abcde#1234)");
            return;
        }

        Context.Torch.CurrentSession.Managers.GetManager<DiscordManager>().WaitingUsers[discordUsername] = Context.Player.SteamUserId;
        Context.Respond("Type `/lp-link` on server discord");
    }
}
