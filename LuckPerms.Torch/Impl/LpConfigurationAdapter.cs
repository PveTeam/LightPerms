using java.nio.file;
using me.lucko.luckperms.common.config.generic.adapter;
using me.lucko.luckperms.common.plugin;
using ninja.leaping.configurate.loader;
using ninja.leaping.configurate.yaml;

namespace LuckPerms.Torch.Impl;

public class LpConfigurationAdapter(LuckPermsPlugin plugin, Path path) : ConfigurateConfigAdapter(plugin, path)
{
    protected override ConfigurationLoader createLoader(Path p)
    {
        return YAMLConfigurationLoader.builder().setPath(p).build();
    }
}