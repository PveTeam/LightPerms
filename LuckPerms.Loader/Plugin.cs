using System.IO.Compression;
using System.Reflection;
using System.Runtime.CompilerServices;
using NLog;
using Torch;
using Torch.API;
using Torch.API.Managers;
using Torch.API.Plugins;
using Torch.Collections;
using Torch.Managers;
using Torch.Utils;

namespace LuckPerms.Loader;

public class Plugin : TorchPluginBase
{
    private static readonly ITorchPlugin MainPluginInstance;
    private static readonly ILogger Log = LogManager.GetLogger("Loader");

    static Plugin()
    {
        string assemblyName;
        using (var infoStream = typeof(Plugin).Assembly.GetManifestResourceStream("name.txt")!)
        using (var infoStreamReader = new StreamReader(infoStream))
            assemblyName = infoStreamReader.ReadLine()!.Trim();
        
#pragma warning disable CS0618 // Type or member is obsolete
        var torch = (ITorchServer)TorchBase.Instance;
#pragma warning restore CS0618 // Type or member is obsolete
        var dir = new DirectoryInfo(Path.Combine(torch.InstancePath, "cache", assemblyName));
        
        void ExtractCache()
        {
            using var currentHashStream = typeof(Plugin).Assembly.GetManifestResourceStream("plugin.zip.sha256")!;
            var currentHash = currentHashStream.ReadToEnd();
            
            var hashPath = Path.Combine(dir.FullName, "plugin.zip.sha256");
            if (dir.Exists)
            {
                if (File.Exists(hashPath))
                {
                    Log.Info("Checking cache");

                    var hash = File.ReadAllBytes(hashPath);

                    if (hash.SequenceEqual(currentHash)) return;
                }

                dir.Delete(true);
            }
        
            Log.Info($"Extracting cache to {dir}");

            using var pluginStream = typeof(Plugin).Assembly.GetManifestResourceStream("plugin.zip")!;
            using var archive = new ZipArchive(pluginStream, ZipArchiveMode.Read);
            
            archive.ExtractToDirectory(dir.FullName);
            File.WriteAllBytes(hashPath, currentHash);
        }

        ExtractCache();

        Log.Info($"Injecting {assemblyName}");

        AppDomain.CurrentDomain.AssemblyResolve += (_, args) =>
        {
            var fileName = args.Name[..args.Name.IndexOf(',')];

            if (AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(b =>
                    b.GetName().Name?.Equals(fileName, StringComparison.OrdinalIgnoreCase) is true) is { } assembly)
                return assembly;
            
            var path = Path.Combine(dir.FullName, fileName + ".dll");

            return File.Exists(path) ? Assembly.LoadFile(path) : null;
        };
        
        var mainAssembly = Assembly.LoadFile(Path.Combine(dir.FullName, $"{assemblyName}.dll"));

        var pluginType = mainAssembly.GetType($"{assemblyName}.Plugin", true)!;
        
        // a hacky way to configure the plugin
        RuntimeHelpers.RunClassConstructor(pluginType.TypeHandle);
        
        typeof(TorchBase).GetMethod("RegisterAuxAssembly", BindingFlags.NonPublic | BindingFlags.Static)!.Invoke(null, new object[] { mainAssembly });

        MainPluginInstance = (ITorchPlugin)Activator.CreateInstance(pluginType)!;
    }

    public override void Init(ITorchBase torch)
    {
        if (MainPluginInstance is TorchPluginBase pluginBase)
        {
            typeof(TorchPluginBase).GetProperty(nameof(Manifest))!.SetValue(pluginBase, Manifest);
            typeof(TorchPluginBase).GetProperty(nameof(StoragePath))!.SetValue(pluginBase, StoragePath);
        }
        
        var pluginManager = torch.Managers.GetManager<PluginManager>();
        var plugins =
            (MtObservableSortedDictionary<Guid, ITorchPlugin>)typeof(PluginManager).GetField("_plugins",
                BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(pluginManager);
        
        plugins.Remove(Manifest.Guid);
        plugins.Add(Manifest.Guid, MainPluginInstance);

        MainPluginInstance.Init(torch);

        Log.Info("Injected successfully");
    }
}