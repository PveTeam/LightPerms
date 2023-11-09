using java.lang;
using me.lucko.luckperms.common.config.generic;
using me.lucko.luckperms.common.config.generic.key;

namespace LuckPerms.Torch.Extensions;

public static class KeyedConfigurationExtensions
{
    public static bool GetBoolean(this KeyedConfiguration config, ConfigKey key, bool defaultValue = default)
    {
        var value = config.get(key);

        return value switch
        {
            bool boolValue => boolValue,
            Boolean booleanValue => booleanValue.booleanValue(),
            _ => defaultValue
        };
    }
}