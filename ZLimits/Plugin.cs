using System.IO;
using Torch;
using Torch.API;
using Torch.API.Managers;
using Torch.API.Session;
using ZLimits.Managers;

namespace ZLimits;

public class Plugin : TorchPluginBase
{
    public override void Init(ITorchBase torch)
    {
        var storagePath = Directory.CreateDirectory(Path.Combine(StoragePath, "zlimits")).FullName;

        torch.Managers.AddManager(new ConfigManager(storagePath));
        torch.Managers.AddManager(new LimitsManager(torch));
        
        var sessionManager = torch.Managers.GetManager<ITorchSessionManager>();

        sessionManager.AddFactory(_ => new ProjectionPatchManager());
    }
}