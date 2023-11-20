using System.IO;
using Microsoft.Extensions.Configuration;
using SmartFormat;
using SmartFormat.Core.Settings;
using Torch.API.Managers;
using Torch.Managers;

namespace Maintenance.Managers;

public class LanguageManager(string storagePath) : IManager
{
    private readonly string[] _langs = { "en" };
    
    public IConfiguration Language => _configurationRoot ?? throw new InvalidOperationException("Manager is not attached");
    
    private IConfigurationRoot? _configurationRoot;
    
    [Manager.Dependency]
    private readonly ConfigManager _configManager = null!;

    public SmartFormatter Formatter { get; } = Smart.CreateDefaultSmartFormat(new()
    {
        CaseSensitivity = CaseSensitivityType.CaseInsensitive
    });

    public string this[string key] => Language[key] ?? throw new KeyNotFoundException(key);
    
    public void Attach()
    {
        var langDirectory = Path.Combine(storagePath, "translations");
        
        if (!Directory.Exists(langDirectory))
            Directory.CreateDirectory(langDirectory);
        
        foreach (var lang in _langs)
        {
            var path = Path.Combine(langDirectory, $"{lang}.yml");
            if (!File.Exists(path))
                ExtractTranslation(path);
        }
        
        _configurationRoot = new ConfigurationBuilder()
            .AddYamlFile(Path.Combine(langDirectory, $"{_configManager.Configuration.GetValue<string>(ConfigKeys.Language)!}.yml"), false, true)
            .Build();
    }

    public string Format(string key, object obj) => Formatter.Format(this[key], obj);

    private void ExtractTranslation(string path)
    {
        var lang = path[storagePath.Length..].TrimStart(Path.GetInvalidFileNameChars()).Replace('\\', '.');
        
        using var stream = typeof(LanguageManager).Assembly.GetManifestResourceStream(lang)!;
        using var file = File.Create(path);
        
        stream.CopyTo(file);
    }

    public void Detach()
    {
    }
}