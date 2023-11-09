using java.lang;
using java.util;
using LuckPerms.Torch.Extensions;
using me.lucko.luckperms.common.cacheddata;
using me.lucko.luckperms.common.calculator;
using me.lucko.luckperms.common.calculator.processor;
using me.lucko.luckperms.common.config;
using net.luckperms.api.query;

namespace LuckPerms.Torch.Impl.Calculator;

public class LpCalculatorFactory(LpTorchPlugin plugin) : CalculatorFactory
{
    public PermissionCalculator build(QueryOptions qo, CacheMetadata cm)
    {
        List processors = new ArrayList(5);

        processors.add(new DirectProcessor());

        if (plugin.getConfiguration().GetBoolean(ConfigKeys.APPLYING_REGEX)) 
            processors.add(new RegexProcessor());

        if (plugin.getConfiguration().GetBoolean(ConfigKeys.APPLYING_WILDCARDS)) 
            processors.add(new WildcardProcessor());

        if (plugin.getConfiguration().GetBoolean(ConfigKeys.APPLYING_WILDCARDS_SPONGE))
            processors.add(new SpongeWildcardProcessor());
        
        if (((Boolean)qo.option(LpContextManager.ConsoleOption).orElse(Boolean.FALSE)).booleanValue())
            processors.add(ConsoleProcessor.Instance); // TODO add config option to disable bypassing checks for console

        return new(plugin, cm, processors);
    }
}