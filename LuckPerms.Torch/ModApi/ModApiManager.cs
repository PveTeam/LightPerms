using System;
using System.Collections.Generic;
using System.Linq;
using ikvm.extensions;
using ikvm.runtime;
using java.lang;
using Torch.API.Managers;
using VRage.Scripting;

namespace LuckPerms.Torch.ModApi;

public class ModApiManager : IManager
{
    public void Attach()
    {
        // TODO make and reference platform helpers (uuid extensions, delegate converters etc)
        MyScriptCompiler.Static.AddConditionalCompilationSymbols("LUCKPERMS_API");

        MyScriptCompiler.Static.AddReferencedAssemblies(
            typeof(net.luckperms.api.LuckPerms).Assembly.Location, // net.luckperms.api.dll
            typeof(java.lang.Boolean).Assembly.Location // IKVM.Java.dll
        );
        
        using var whitelist = MyScriptCompiler.Static.Whitelist.OpenBatch();
        
        whitelist.AllowNamespaceOfTypes(MyWhitelistTarget.ModApi, typeof(net.luckperms.api.LuckPerms).Assembly.GetTypes().Distinct(TypeNamespaceComparer.Instance).ToArray());
        whitelist.AllowNamespaceOfTypes(MyWhitelistTarget.ModApi, typeof(java.util.function.Consumer), typeof(java.util.UUID), typeof(java.time.Instant), typeof(java.time.temporal.Temporal));
        whitelist.AllowTypes(MyWhitelistTarget.ModApi, typeof(java.lang.Boolean), typeof(java.lang.Enum),
            typeof(AutoCloseable), typeof(java.util.concurrent.TimeUnit),
            typeof(java.util.concurrent.CompletableFuture));
        whitelist.AllowMembers(MyWhitelistTarget.ModApi,
            typeof(Class).GetMethod("op_Implicit", new[] { typeof(Type) }),
            typeof(Class).GetMethod(nameof(Class.getName)),
            typeof(Class).GetMethod(nameof(Class.getTypeName)),
            typeof(Util).GetMethod(nameof(Util.getClassFromObject)),
            typeof(Util).GetMethod(nameof(Util.getFriendlyClassFromType)),
            typeof(Util).GetMethod(nameof(Util.getInstanceTypeFromClass)),
            typeof(Util).GetMethod(nameof(Util.getRuntimeTypeFromClass)),
            typeof(ExtensionMethods).GetMethod(nameof(ExtensionMethods.getClass), new []{ typeof(object) }),
            typeof(java.lang.Object).GetMethod(nameof(java.lang.Object.getClass)));
    }

    public void Detach()
    {
    }

    private sealed class TypeNamespaceComparer : IEqualityComparer<Type>
    {
        public static readonly IEqualityComparer<Type> Instance = new TypeNamespaceComparer();
        
        public bool Equals(Type x, Type y)
        {
            if (ReferenceEquals(x, y)) return true;
            if (ReferenceEquals(x, null)) return false;
            if (ReferenceEquals(y, null)) return false;
            if (x.GetType() != y.GetType()) return false;
            return x.Namespace == y.Namespace;
        }

        public int GetHashCode(Type obj)
        {
            return obj.Namespace != null ? obj.Namespace.GetHashCode() : 0;
        }
    }
}