using Torch;
using Torch.Views;

namespace LightPerms;

public class Config : ViewModel
{
    [Display(Name = "Default Group Name", Description = "All new players will join this group.")]
    public string DefaultGroupName { get; set; } = "player";
}
