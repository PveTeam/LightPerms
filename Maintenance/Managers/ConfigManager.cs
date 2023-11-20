using System.IO;
using Microsoft.Extensions.Configuration;
using Torch.API.Managers;

namespace Maintenance.Managers;

public class ConfigManager(string storagePath) : IManager
{
    private IConfigurationRoot? _configurationRoot;
    
    public IConfiguration Configuration => _configurationRoot ?? throw new InvalidOperationException("Manager is not attached");
    
    public void Attach()
    {
        var configPath = Path.Combine(storagePath, "config.yml");
        
        if (!File.Exists(configPath))
            ExtractDefault(configPath);

        _configurationRoot = new ConfigurationBuilder()
            .AddYamlFile(configPath, false, true)
            .Build();
    }

    private void ExtractDefault(string path)
    {
        var name = path[storagePath.Length..].TrimStart(Path.GetInvalidFileNameChars()).Replace('\\', '.');
        
        using var stream = typeof(ConfigManager).Assembly.GetManifestResourceStream(name)!;
        using var file = File.Create(path);
        
        stream.CopyTo(file);
    }

    public void Detach()
    {
    }
}