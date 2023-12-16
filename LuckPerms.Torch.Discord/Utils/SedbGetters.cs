using System.Reflection;
using Torch.API.Managers;
using Torch.API.Plugins;

namespace LuckPerms.Torch.Discord.Utils;

public static class SedbGetters
{
    private static readonly Guid SedbId = new("3cd3ba7f-c47c-4efe-8cf1-bd3f618f5b9c");

    public static bool IsSedbInstalled(this IPluginManager pluginManager) => pluginManager.Plugins.ContainsKey(SedbId);

    public static string? GetSedbToken(this IPluginManager pluginManager)
    {
        var instance = GetConfigProp(pluginManager, out var configProp);
        var tokenProp = configProp.PropertyType.GetProperty("BotToken") ?? throw new InvalidOperationException("BotToken property not found");
        
        return tokenProp.GetValue(configProp.GetValue(instance)) as string;
    }

    private static ITorchPlugin GetConfigProp(IPluginManager pluginManager, out PropertyInfo configProp)
    {
        var instance = pluginManager.Plugins[SedbId];
        configProp = instance.GetType().GetProperty("Config") ?? throw new InvalidOperationException("Config property not found");
        return instance;
    }
}