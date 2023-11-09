using System;
using me.lucko.luckperms.common.plugin.logging;
using NLog;

namespace LuckPerms.Torch.Impl;

public class NLogPluginLogger(ILogger logger) : PluginLogger
{
    public void warn(string str) => logger.Warn(str);

    public void warn(string str, Exception t) => logger.Warn(t, str);

    public void info(string str) => logger.Info(str);

    public void severe(string str) => logger.Error(str);

    public void severe(string str, Exception t) => logger.Error(t, str);
}