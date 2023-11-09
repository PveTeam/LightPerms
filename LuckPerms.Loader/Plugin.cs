using System.IO.Compression;
using System.Reflection;
using System.Runtime.CompilerServices;
using NLog;
using Torch;
using Torch.API;
using Torch.API.Managers;
using Torch.API.Plugins;
using Torch.Managers;

namespace LuckPerms.Loader;

public class Plugin : TorchPluginBase
{
    private static readonly ITorchPlugin MainPluginInstance;
    private static readonly ILogger Log = LogManager.GetLogger("LuckPerms.Loader");

    static Plugin()
    {
#pragma warning disable CS0618 // Type or member is obsolete
        var torch = (ITorchServer)TorchBase.Instance;
#pragma warning restore CS0618 // Type or member is obsolete
        var dir = new DirectoryInfo(Path.Combine(torch.InstancePath, "cache", "luckperms.loader"));

        if (dir.Exists)
            dir.Delete(true);
        
        Log.Info($"Extracting cache to {dir}");
        
        using (var pluginStream = typeof(Plugin).Assembly.GetManifestResourceStream("plugin.zip")!)
        using (var archive = new ZipArchive(pluginStream, ZipArchiveMode.Read))
            archive.ExtractToDirectory(dir.FullName);

        Log.Info("Injecting LuckPerms");

        AppDomain.CurrentDomain.AssemblyResolve += (_, args) =>
        {
            var fileName = args.Name[..args.Name.IndexOf(',')];
            var path = Path.Combine(dir.FullName, fileName + ".dll");

            return File.Exists(path) ? Assembly.LoadFile(path) : null;
        };
        
        var mainAssembly = Assembly.LoadFile(Path.Combine(dir.FullName, "LuckPerms.Torch.dll"));

        var pluginType = mainAssembly.GetType("LuckPerms.Torch.Plugin", true)!;
        
        // a hacky way to configure JVM
        RuntimeHelpers.RunClassConstructor(pluginType.TypeHandle);
        
        TorchBase.RegisterAuxAssembly(mainAssembly);

        MainPluginInstance = (ITorchPlugin)Activator.CreateInstance(pluginType)!;

        if (MainPluginInstance is not TorchPluginBase pluginBase) return;
        
        pluginBase.Manifest = PluginManifest.Load(Path.Combine(dir.FullName, "manifest.xml"));
        pluginBase.StoragePath = torch.InstancePath;
    }

    public override void Init(ITorchBase torch)
    {
        var pluginManager = torch.Managers.GetManager<PluginManager>();

        pluginManager._plugins.Remove(Manifest.Guid);
        pluginManager._plugins.Add(Manifest.Guid, MainPluginInstance);
        
        MainPluginInstance.Init(torch);
        
        Log.Info("Injected successfully");
    }
}