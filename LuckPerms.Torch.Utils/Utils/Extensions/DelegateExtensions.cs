using System.Reflection;
using java.lang;
using java.util.concurrent;
using java.util.function;
using net.luckperms.api.@event;

namespace LuckPerms.Torch.Utils.Extensions;

public static class DelegateExtensions
{
    private static readonly Func<Consumer, Consumer, Consumer> AndThenDefault =
        (Func<Consumer, Consumer, Consumer>)typeof(Consumer).GetMethod("<default>andThen",
            BindingFlags.Static | BindingFlags.Public)!.CreateDelegate(typeof(Func<Consumer, Consumer, Consumer>));
    
    private static readonly Func<Predicate, Predicate, Predicate> AndDefault =
        (Func<Predicate, Predicate, Predicate>)typeof(Predicate).GetMethod("<default>and",
            BindingFlags.Static | BindingFlags.Public)!.CreateDelegate(typeof(Func<Predicate, Predicate, Predicate>));
    
    private static readonly Func<Predicate, Predicate> NegateDefault =
        (Func<Predicate, Predicate>)typeof(Predicate).GetMethod("<default>negate",
            BindingFlags.Static | BindingFlags.Public)!.CreateDelegate(typeof(Func<Predicate, Predicate>));
    
    private static readonly Func<Predicate, Predicate, Predicate> OrDefault =
        (Func<Predicate, Predicate, Predicate>)typeof(Predicate).GetMethod("<default>or",
            BindingFlags.Static | BindingFlags.Public)!.CreateDelegate(typeof(Func<Predicate, Predicate, Predicate>));
    
    public static Runnable ToRunnable(this Action action) => new DelegateRunnable(action);
    
    public static Consumer ToConsumer<T>(this Action<T> action) => new DelegateConsumer<T>(action);
    
    public static Supplier ToSupplier<T>(this Func<T> func) where T : notnull => new DelegateSupplier<T>(func);
    
    public static Predicate ToPredicate<T>(this Predicate<T> predicate) => new DelegatePredicate<T>(predicate);
    
    public static Callable ToCallable<T>(this Func<T> func) => new DelegateSupplier<T>(func);

    private sealed class DelegatePredicate<T>(Predicate<T> predicate) : Predicate
    {
        public bool test(object t) => predicate((T)t);

        public Predicate and(Predicate other) => AndDefault(this, other);

        public Predicate negate() => NegateDefault(this);

        public Predicate or(Predicate other) => OrDefault(this, other);
    }

    private sealed class DelegateSupplier<T>(Func<T> func) : Supplier, Callable where T : notnull
    {
        public object get() => func();
        public object call() => func();
    }

    private sealed class DelegateConsumer<T>(Action<T> action) : Consumer
    {
        public void accept(object t)
        {
            action((T)t);
        }

        public Consumer andThen(Consumer after) => AndThenDefault(this, after);
    }

    // ReSharper disable once InconsistentNaming
    // lets make it an overload for convenience
    public static void execute(this Executor executor, Action action) => executor.execute(action.ToRunnable());

    public static EventSubscription subscribe<TEvent>(this EventBus bus, object plugin, Action<TEvent> action)
        where TEvent : class, LuckPermsEvent => bus.subscribe(plugin, typeof(TEvent), action.ToConsumer());
    
    public static EventSubscription subscribe<TEvent>(this EventBus bus, Action<TEvent> action)
        where TEvent : class, LuckPermsEvent => bus.subscribe(typeof(TEvent), action.ToConsumer());

    private sealed class DelegateRunnable(Action action) : Runnable
    {
        public void run() => action();
    }
}