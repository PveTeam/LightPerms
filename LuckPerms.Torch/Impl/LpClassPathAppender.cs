using System.Reflection;
using java.nio.file;
using me.lucko.luckperms.common.plugin.classpath;

namespace LuckPerms.Torch.Impl;

public class LpClassPathAppender : ClassPathAppender
{
    public void Dispose()
    {
    }

    public void addJarToClasspath(Path p)
    {
        if (p.endsWith(".dll"))
            Assembly.Load(p.toString());
    }

    public void close()
    {
    }
}