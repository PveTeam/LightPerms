using System.IO;
using Maintenance.Managers;
using Torch;
using Torch.API;
using Torch.API.Managers;
using Torch.API.Session;

namespace Maintenance;

public class Plugin : TorchPluginBase
{
    public override void Init(ITorchBase torch)
    {
        var storagePath = Directory.CreateDirectory(Path.Combine(StoragePath, "maintenance")).FullName;
        
        torch.Managers.AddManager(new ConfigManager(storagePath));
        torch.Managers.AddManager(new LanguageManager(storagePath));
        torch.Managers.GetManager<ITorchSessionManager>().AddFactory(s => new MaintenanceManager(s.Torch));
        torch.Managers.GetManager<ITorchSessionManager>().AddFactory(_ => new MaintenanceScheduleManager(storagePath));
    }
}