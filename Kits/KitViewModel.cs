using System.Collections.ObjectModel;
using System.Xml.Serialization;
using Kits.Views;
using Torch;
using Torch.Views;
using VRage.Game.ModAPI;
using VRage.ObjectBuilders;

namespace Kits;

public class KitViewModel : ViewModel
{
    [Display(Name = "Name", GroupName = "General")]
    [XmlAttribute]
    public string Name { get; set; } = "unnamed";
    [Display(Name = "Cost", GroupName = "Usage", Description = "Credits cost to use this kit")]
    public long UseCost { get; set; } = 0;
    [Display(Name = "Cooldown Minutes", GroupName = "Usage", Description = "Cooldown to use this kit per player in minutes")]
    public ulong UseCooldownMinutes { get; set; } = 0;
    [Display(Name = "Required Promote Level", GroupName = "Conditions", Description = "Minimal Promote Level to use this kit")]
    public MyPromoteLevel RequiredPromoteLevel { get; set; } = MyPromoteLevel.None;
    [Display(Name = "Lp Permission", GroupName = "Conditions", Description = "Luck Perms permission to use this kit (leave empty to disable, example: kits.vip)")]
    public string LpPermission { get; set; } = "";
    [Display(Name = "Respawn Pod Wildcards", GroupName = "Usage", Description = "Respawn pod name wildcard to filter usage of kit, leave empty to disable")]
    public ObservableCollection<string> RespawnPodWildcards { get; set; } = [];
    [Display(Name = "Items", GroupName = "General", EditorType = typeof(EditButton))]
    [XmlArrayItem("Item")]
    public ObservableCollection<KitItemViewModel> Items { get; set; } = [];

    public override string ToString()
    {
        return Name;
    }
}
public class KitItemViewModel : ViewModel
{
    [Display(Name = "Id", EditorType = typeof(DefinitionIdEditor), Description = "TypeId/SubtypeId. Only items are allowed. for e.g Component/SteelPlate, Ore/Stone, PhysicalGunObject/RapidFireAutomaticRifleItem")]
    public DefinitionId Id { get; set; } = new();
    [Display(Name = "Probability", Description = "Probability of the item. 1 is 100%, 0 is 0%")]
    public float Probability { get; set; } = 1;
    [Display(Name = "Amount")]
    public float Amount { get; set; } = 0;
    
    [XmlIgnore]
    public string Name => Id.ToString();

    public override string ToString()
    {
        return Id.ToString();
    }
}

public class DefinitionId : ViewModel
{
    [XmlAttribute]
    public string TypeId { get; set; } = "type";
    [XmlAttribute]
    public string SubtypeId { get; set; } = "subtype";

    public override string ToString() => $"{TypeId}/{SubtypeId}";
    public static implicit operator SerializableDefinitionId(DefinitionId id) => new(MyObjectBuilderType.ParseBackwardsCompatible(id.TypeId), id.SubtypeId);
}
