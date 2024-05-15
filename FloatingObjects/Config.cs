using Torch;
using Torch.Views;

namespace FloatingObjects;

public class Config : ViewModel
{
    [Display(Name = "Enabled", Description = "Enables functional of plugin.", GroupName = "Plugin")]
    public bool Enabled { get; set; }

    [Display(Name = "Disable Ejection",
        Description =
            "Disables ejection of items into the world by connector/ejector. Now if throw out is enabled, all items wil go to the void!",
        GroupName = "Floating Objects")]
    public bool DisableEjection { get; set; }

    [Display(Name = "Auto Ore Pickup",
        Description = "Now all mined ore by hand drill will be immediately directed to player's inventory.",
        GroupName = "Floating Objects")]
    public bool AutoOrePickup { get; set; }

    [Display(Name = "Stack Dropped Items",
        Description = "Stack all items dropped from destroyed cargo or any block with inventory. (Would work only if temp/loot container spawn is disabled in world settings)",
        GroupName = "Floating Objects")]
    public bool StackDropItems { get; set; }

    [Display(Name = "Disable Destroyed Ammo Detonation",
        Description = "Detonate explosives/ammo when containing cargo or dropped item is destroyed.",
        GroupName = "Floating Objects")] 
    public bool DisableAmmoDetonation { get; set; }
}