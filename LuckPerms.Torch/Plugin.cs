using System;
using System.IO;
using java.lang;
using java.util;
using LuckPerms.Torch.Impl;
using NLog;
using Sandbox;
using Torch;
using Torch.API;
using Exception = System.Exception;
using Object = java.lang.Object;

namespace LuckPerms.Torch;

public class Plugin : TorchPluginBase
{
    static Plugin()
    {
        // for some fucking reason if jvm is getting initialized in some other place - it throws
        // happens only on framework and most likely related to its retarded types initialization rules
        _ = new Object(); 
        Locale.setDefault(Locale.ENGLISH);
    }
    
    public static readonly ILogger Log = LogManager.GetLogger("LuckPerms");
    private LpTorchBootstrap? _bootstrap;

    public override void Init(ITorchBase torch)
    {
        base.Init(torch);
        Torch.GameStateChanged += TorchOnGameStateChanged;
        _bootstrap = new((ITorchServer)Torch, this, Log, Path.Combine(StoragePath, "luckperms"));
        
        try
        {
            Log.Info("Initializing LuckPerms");
            _bootstrap.Plugin.load();
        }
        catch (Exception e)
        {
            Log.Fatal(e);
            throw;
        }
        finally
        {
            _bootstrap.LoadLatch.countDown();
        }
    }

    private void TorchOnGameStateChanged(MySandboxGame game, TorchGameState newState)
    {
        if (_bootstrap is null)
            throw new InvalidOperationException("Plugin is not initialized");
        
        switch (newState)
        {
            case TorchGameState.Loading:
                try
                {
                    Log.Info("Loading LuckPerms");
                    Thread.currentThread().setContextClassLoader(LpDependencyManager.CurrentClassLoader);
                    _bootstrap.Plugin.enable();
                }
                catch (Exception e)
                {
                    Log.Fatal(e);
                    throw;
                }
                finally
                {
                    _bootstrap.EnableLatch.countDown();
                }
                break;
            case TorchGameState.Unloading:
                Log.Info("Unloading LuckPerms");
                _bootstrap.Plugin.disable();
                break;
        }
    }
}