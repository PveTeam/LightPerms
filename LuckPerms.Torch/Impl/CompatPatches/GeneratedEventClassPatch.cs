using System;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using ikvm.runtime;
using java.lang;
using java.lang.invoke;
using java.util.concurrent.atomic;
using me.lucko.luckperms.common.@event.gen;
using net.luckperms.api.@event;
using net.luckperms.api.@event.type;
using net.luckperms.api.@event.util;
using Torch.Managers.PatchManager;
using MethodType = java.lang.invoke.MethodType;
using Object = java.lang.Object;
using Void = java.lang.Void;

namespace LuckPerms.Torch.Impl.CompatPatches;

[PatchShim]
[HarmonyPatch(typeof(GeneratedEventClass), HarmonyLib.MethodType.Constructor, typeof(Class))]
public static class GeneratedEventClassPatch
{
    private static readonly MethodInfo GetClassFromHandle =
        typeof(Util).GetMethod(nameof(Util.getClassFromTypeHandle), new[] { typeof(RuntimeTypeHandle) })!;

    private static readonly MethodInfo LookupMethod =
        typeof(MethodHandles).GetMethod(nameof(MethodHandles.lookup), Array.Empty<Type>())!;

    private static readonly ConstructorInfo AbstractEventCtor =
        typeof(AbstractEvent).GetConstructor(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly, null,
            new[] { typeof(net.luckperms.api.LuckPerms) }, Array.Empty<ParameterModifier>())!;
    
    // [ReflectedMethodInfo(typeof(GeneratedEventClassPatch), nameof(Prefix))]
    // private static MethodInfo _prefixMethod = null!;

    private static ModuleBuilder? _builder;

    public static void Patch(PatchContext context)
    {
        // var target = typeof(GeneratedEventClass).GetConstructor(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly,
        //     null,
        //     new[] { typeof(Class) }, Array.Empty<ParameterModifier>())!;
        //
        // context.GetPattern(target).Prefixes.Add(_prefixMethod);

        new Harmony("GeneratedEventClassPatch").CreateClassProcessor(typeof(GeneratedEventClassPatch)).Patch();
    }

    public static bool Prefix(Class __0, ref MethodHandle ___constructor, ref MethodHandle[] ___setters)
    {
        var eventClassSuffix = __0.getName()[((Class)typeof(LuckPermsEvent)).getPackage().getName().Length..];
        var packageWithName = ((Class)typeof(GeneratedEventClass)).getName();
        var generatedClassName = packageWithName[..packageWithName.LastIndexOf('.')] + eventClassSuffix;

        const string name = "LuckPerms.GeneratedEventClasses";
        _builder ??= AssemblyBuilder.DefineDynamicAssembly(new(name), AssemblyBuilderAccess.Run).DefineDynamicModule(name);
        
        // create a subclass of AbstractEvent
        // using the predetermined generated class name
        var typeBuilder = _builder.DefineType(generatedClassName, TypeAttributes.Class | TypeAttributes.Public | TypeAttributes.Sealed, typeof(AbstractEvent));

        var eventInterfaceType = Util.getRuntimeTypeFromClass(__0);
        
        // implement the event interface
        typeBuilder.AddInterfaceImplementation(eventInterfaceType);

        var methods = eventInterfaceType
            .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Where(b => b.HasAttribute<ParamAttribute>())
            .OrderBy(b => ((Param)b.GetCustomAttribute<ParamAttribute>()!).value())
            .ToArray();
        
        // implement all methods annotated with Param by simply returning the value from the corresponding field with the same name
        foreach (var methodInfo in methods)
        {
            var valueField = typeBuilder.DefineField(methodInfo.Name, methodInfo.ReturnType, FieldAttributes.Private);
            
            var methodBuilder = typeBuilder.DefineMethod(methodInfo.Name,
                MethodAttributes.Public | MethodAttributes.Virtual, methodInfo.ReturnType,
                methodInfo.GetParameters().Select(p => p.ParameterType).ToArray());

            var ilGenerator = methodBuilder.GetILGenerator();
            
            ilGenerator.Emit(OpCodes.Ldarg_0);
            ilGenerator.Emit(OpCodes.Ldfld, valueField);
            ilGenerator.Emit(OpCodes.Ret);
        }
        
        // implement non param methods to their default impl
        FieldBuilder? cancellationStateField = null;
        foreach (var methodInfo in eventInterfaceType.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                     .Concat(eventInterfaceType.GetInterfaces()
                         .SelectMany(b => b.GetMethods(BindingFlags.Public | BindingFlags.Instance))).Except(methods))
        {
            var methodBuilder = typeBuilder.DefineMethod(methodInfo.Name,
                MethodAttributes.Public | MethodAttributes.Virtual, methodInfo.ReturnType,
                methodInfo.GetParameters().Select(p => p.ParameterType).ToArray());

            var ilGenerator = methodBuilder.GetILGenerator();

            if (methodInfo.DeclaringType == typeof(Cancellable) && methodInfo.Name == "cancellationState")
            {
                cancellationStateField = typeBuilder.DefineField("cancellationState", typeof(AtomicBoolean), FieldAttributes.Private);
                ilGenerator.Emit(OpCodes.Ldarg_0);
                ilGenerator.Emit(OpCodes.Ldfld, cancellationStateField);
                ilGenerator.Emit(OpCodes.Ret);
                continue;
            }

            var defaultMethodsType = methodInfo.DeclaringType!.GetNestedType("__DefaultMethods", BindingFlags.NonPublic | BindingFlags.Static);
            var defaultMethod = defaultMethodsType?.GetMethod(methodInfo.Name,
                methodInfo.GetParameters().Select(p => p.ParameterType).ToArray());

            if (defaultMethodsType is null || defaultMethod is null)
            {
                if (methodInfo.ReturnType != typeof(void))
                    ilGenerator.Emit(OpCodes.Ldnull);
                ilGenerator.Emit(OpCodes.Ret);
                continue;
            }

            for (var i = 0; i < methodInfo.GetParameters().Length; i++)
            {
                ilGenerator.Emit(OpCodes.Ldarg, i + 1);
            }
            
            ilGenerator.Emit(OpCodes.Call, defaultMethod);
            ilGenerator.Emit(OpCodes.Ret);
        }
        
        // implement LuckPermsEvent#getEventType by returning the event class type
        var getEventTypeBuilder = typeBuilder.DefineMethod("getEventType", MethodAttributes.Public | MethodAttributes.Virtual, typeof(Class), null);
        var getEventTypeIlGenerator = getEventTypeBuilder.GetILGenerator();
        
        getEventTypeIlGenerator.Emit(OpCodes.Ldtoken, eventInterfaceType);
        getEventTypeIlGenerator.Emit(OpCodes.Call, GetClassFromHandle);
        getEventTypeIlGenerator.Emit(OpCodes.Ret);
        
        typeBuilder.DefineMethodOverride(getEventTypeBuilder, typeof(AbstractEvent).GetMethod(nameof(AbstractEvent.getEventType))!);
        
        // implement AbstractEvent#mh by calling & returning the value of MethodHandles.lookup()

        var mhlBuilder = typeBuilder.DefineMethod("mhl", MethodAttributes.Public | MethodAttributes.Virtual,
            typeof(MethodHandles.Lookup), null);
        
        mhlBuilder.SetImplementationFlags(MethodImplAttributes.NoInlining);
        
        var mhlIlGenerator = mhlBuilder.GetILGenerator();
        
        mhlIlGenerator.Emit(OpCodes.Call, LookupMethod);
        mhlIlGenerator.Emit(OpCodes.Ret);
        
        typeBuilder.DefineMethodOverride(mhlBuilder, typeof(AbstractEvent).GetMethod(nameof(AbstractEvent.mhl))!);
        
        // define constructor
        var constructorBuilder = typeBuilder.DefineConstructor(
            MethodAttributes.Public, CallingConventions.HasThis, 
            new[] { typeof(net.luckperms.api.LuckPerms) });
        var constructorIlGenerator = constructorBuilder.GetILGenerator();
        
        constructorIlGenerator.Emit(OpCodes.Ldarg_0);
        constructorIlGenerator.Emit(OpCodes.Ldarg_1);
        constructorIlGenerator.Emit(OpCodes.Call, AbstractEventCtor);

        if (cancellationStateField is not null)
        {
            constructorIlGenerator.Emit(OpCodes.Ldarg_0);
            constructorIlGenerator.Emit(OpCodes.Ldc_I4_0);
            constructorIlGenerator.Emit(OpCodes.Newobj, typeof(AtomicBoolean).GetConstructors()[0]);
            constructorIlGenerator.Emit(OpCodes.Stfld, cancellationStateField);
        }
        
        constructorIlGenerator.Emit(OpCodes.Ret);

        var type = typeBuilder.CreateType();

        var lookup = ((AbstractEvent)type.GetConstructor(new[] { typeof(net.luckperms.api.LuckPerms) })!
            .Invoke(new object?[] { null })).mhl();

        ___constructor =
            lookup.findConstructor(type, MethodType.methodType(Void.TYPE, typeof(net.luckperms.api.LuckPerms)))
                .asType(MethodType.methodType(typeof(AbstractEvent), typeof(net.luckperms.api.LuckPerms)));

        ___setters = methods.Select(b =>
            lookup.findSetter(type, b.Name, b.ReturnType)
                .asType(MethodType.methodType(Void.TYPE,
                new Class[] { typeof(AbstractEvent), typeof(Object) })))
            .ToArray();
        
        return false;
    }
}