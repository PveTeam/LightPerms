using System;
using System.IO;
using java.lang;
using java.util;
using LuckPerms.Torch.Impl;
using LuckPerms.Torch.PlatformApi;
using NLog;
using Sandbox;
using Torch;
using Torch.API;
using Torch.API.Managers;
using Torch.API.Session;
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
    

    public override void Init(ITorchBase torch)
    {
        base.Init(torch);

        var platformManager = new LuckPermsPlatformManager(this, (ITorchServer)torch, Log);

        Torch.Managers.GetManager<ITorchSessionManager>().AddFactory(_ => platformManager);
    }
}