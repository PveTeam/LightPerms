using System.IO;
using System.Windows.Controls;
using heh.Utils;
using Torch;
using Torch.API;
using Torch.API.Managers;
using Torch.API.Plugins;
using Torch.API.Session;
using Torch.Views;

namespace LightPerms.Discord;

public class Plugin : TorchPluginBase, IWpfPlugin
{
    private ProperPersistent<Config> _config = null!;

    public override void Init(ITorchBase torch)
    {
        base.Init(torch);
        _config = new(Path.Combine(StoragePath, "LightPerms.Discord.cfg"));

        Torch.Managers.GetManager<ITorchSessionManager>().AddFactory(s => new DiscordManager(s.Torch, _config.Data));
    }

    public UserControl GetControl() => new PropertyGrid
    {
        Margin = new(3),
        DataContext = _config.Data
    };
}
