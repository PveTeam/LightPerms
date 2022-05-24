using System.Collections.ObjectModel;
using Torch;
using Torch.Views;

namespace LightPerms.Discord;

public class Config : ViewModel
{
    [Display(Name = "Token", Description = "Discord bot token")]
    public string Token { get; set; } = "bot-token";
    [Display(Name = "Guild Id", Description = "Id of the guild to work with")]
    public ulong GuildId { get; set; }
    [Display(Name = "Role Configs")]
    public ObservableCollection<DiscordRoleConfig> RoleConfigs { get; set; } = new();
}

public class DiscordRoleConfig : ViewModel
{
    [Display(Name = "Role Id", Description = "Id of the discord role to work with")]
    public ulong RoleId { get; set; }
    [Display(Name = "Group Name", Description = "Name of the group to work with (read guide on LightPerms plugin page to create a new group)")]
    public string GroupName { get; set; } = "role-group";

    public override string ToString() => GroupName;
}
