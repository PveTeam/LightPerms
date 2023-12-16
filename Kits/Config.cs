using System.Collections.ObjectModel;
using System.Xml.Schema;
using System.Xml.Serialization;
using Kits.Views;
using Torch;
using Torch.Views;

namespace Kits;

[XmlRoot]
public class Config : ViewModel
{
    [Display(Name = "Kits", EditorType = typeof(EditButton))]
    [XmlArrayItem("Kit")]
    public ObservableCollection<KitViewModel> Kits { get; set; } = new();

    [XmlAttribute(Form = XmlSchemaForm.Qualified, Namespace = "http://www.w3.org/2001/XMLSchema-instance")]
    // ReSharper disable once InconsistentNaming
    public string noNamespaceSchemaLocation = "Kits.v1.1.2.xsd";
}
