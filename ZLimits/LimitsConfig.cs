using Torch.Views;

namespace ZLimits;

public class LimitsConfig
{
    public LimitGroupInfo[] LimitGroups { get; set; } = [];
    [Display(Name = "Nexus Sync", Description = "Sync limits between server using Nexus plugin (if installed).")]
    public bool NexusSync { get; set; }
    public bool LimitGridsPcu { get; set; }
    public int LargeGridPcu { get; set; } = 16000;
    public int SmallGridPcu { get; set; } = 6000;
}