using System.IO;
using System.Windows.Controls;
using heh;
using heh.Utils;
using Kits.Views;
using Torch;
using Torch.API;
using Torch.API.Managers;
using Torch.API.Plugins;
using Torch.API.Session;
using Torch.Views;

namespace Kits;

public class Plugin : TorchPluginBase, IWpfPlugin
{
    private ProperPersistent<Config> _config = null!;

    public override void Init(ITorchBase torch)
    {
        base.Init(torch);
        CheckConfigSchema();
        _config = new(Path.Combine(StoragePath, "Kits.xml"));
        
        Torch.Managers.AddManager(DbManager.Static);
        Torch.Managers.GetManager<ITorchSessionManager>().AddFactory(s => new KitManager(s.Torch, _config.Data));
    }

    private void CheckConfigSchema()
    {
        var files = Directory.EnumerateFiles(StoragePath, "Kits.*.xsd").ToList();
        if (files.Any() && files[0].Substring(files[0].IndexOf('.') + 1, Manifest.Version.Length) == Manifest.Version)
            return;
        
        foreach (var file in files)
        {
            File.Delete(file);
        }
        
        using var resource = typeof(Plugin).Assembly.GetManifestResourceStream("Kits.schema.xsd");
        using var stream = File.Create(Path.Combine(StoragePath, $"Kits.{Manifest.Version}.xsd"));
        resource?.CopyTo(stream);
    }

    public UserControl GetControl() => new PropertyGrid
    {
        Margin = new(3),
        DataContext = _config.Data
    };
}
