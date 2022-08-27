using System.IO;
using System.Windows.Controls;
using heh;
using heh.Utils;
using Torch;
using Torch.API;
using Torch.API.Managers;
using Torch.API.Plugins;
using Torch.API.Session;
using Torch.Views;

namespace LightPerms;

public class Plugin : TorchPluginBase, IWpfPlugin
{
    private ProperPersistent<Config> _config = null!;

    public override void Init(ITorchBase torch)
    {
        base.Init(torch);
        _config = new(Path.Combine(StoragePath, "LightPerms.cfg"));
        Torch.Managers.AddManager(DbManager.Static);
        Torch.Managers.AddManager(new PermissionsManager(Torch));
        Torch.Managers.GetManager<ITorchSessionManager>().AddFactory(s => new MultiplayerMembersManager(s.Torch));
    }

    public UserControl GetControl() => new PropertyGrid
    {
        Margin = new(3),
        DataContext = _config.Data
    };
}
