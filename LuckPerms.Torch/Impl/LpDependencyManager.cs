using ikvm.runtime;
using java.lang;
using java.util;
using me.lucko.luckperms.common.dependencies;

namespace LuckPerms.Torch.Impl;

public class LpDependencyManager : DependencyManager
{
    public static readonly ClassLoader CurrentClassLoader = new AppDomainAssemblyClassLoader(typeof(LpDependencyManager).Assembly);
    
    public void Dispose()
    {
    }

    public void loadDependencies(Set s)
    {
    }

    public ClassLoader obtainClassLoaderWith(Set s) => CurrentClassLoader;

    public void loadStorageDependencies(Set s, bool b1, bool b2, bool b3)
    {
    }

    public void close()
    {
    }
}