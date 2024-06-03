using System.IO;
using BuildAndRepair.Torch.Managers;
using NLog;
using Torch;
using Torch.API;
using Torch.API.Managers;
using Torch.Server.Managers;
using Torch.Server.ViewModels;
using Torch.Utils;

namespace BuildAndRepair.Torch;

public class Plugin : TorchPluginBase
{
    public const ulong ModId = 2111073562;
    
    internal static readonly ILogger Log = LogManager.GetCurrentClassLogger();

    // we need this because of the way mod classes are instantiated
    internal static ITorchBase Torch = null!;
    
    public override void Init(ITorchBase torch)
    {
        var storagePath = Directory.CreateDirectory(Path.Combine(StoragePath, "buildandrepair")).FullName;

        torch.Managers.AddManager(new ConfigManager(storagePath));
        
        Torch = torch;
        
        torch.Managers.GetManager<InstanceManager>().InstanceLoaded += OnInstanceLoaded;

        void OnInstanceLoaded(ConfigDedicatedViewModel model)
        {
            if (model.Mods.All(b => b.PublishedFileId != ModId))
                model.Mods.Add(new(ModItemUtils.Create(ModId)));
        }
    }
}