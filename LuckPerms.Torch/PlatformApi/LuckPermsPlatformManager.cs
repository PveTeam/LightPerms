using System.IO;
using java.lang;
using LuckPerms.Torch.Impl;
using NLog;
using Torch;
using Torch.API;
using Torch.API.Managers;
using Exception = System.Exception;

namespace LuckPerms.Torch.PlatformApi;

public class LuckPermsPlatformManager : IManager
{
    private readonly ILogger _log = LogManager.GetCurrentClassLogger();
    private readonly LpTorchBootstrap _bootstrap;
    
    public LuckPermsPlatformManager(TorchPluginBase plugin, ITorchServer server, ILogger log)
    {
        _bootstrap = new(server, plugin, log, Path.Combine(plugin.StoragePath, "luckperms"));
        
        try
        {
            log.Info("Initializing LuckPerms");
            _bootstrap.Plugin.load();
        }
        catch (Exception e)
        {
            log.Fatal(e);
            throw;
        }
        finally
        {
            _bootstrap.LoadLatch.countDown();
        }
    }
    
    public void Attach()
    {
        try
        {
            _log.Info("Loading LuckPerms");
            Thread.currentThread().setContextClassLoader(LpDependencyManager.CurrentClassLoader);
            _bootstrap.Plugin.enable();
        }
        catch (Exception e)
        {
            _log.Fatal(e);
            throw;
        }
        finally
        {
            _bootstrap.EnableLatch.countDown();
        }
    }

    public void Detach()
    {
        _log.Info("Unloading LuckPerms");
        _bootstrap.Plugin.disable();
    }
}