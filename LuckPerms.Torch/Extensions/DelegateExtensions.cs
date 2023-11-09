using System;
using java.lang;
using java.util.concurrent;

namespace LuckPerms.Torch.Extensions;

public static class DelegateExtensions
{
    public static Runnable ToRunnable(this Action action) => new DelegateRunnable(action);

    // ReSharper disable once InconsistentNaming
    // lets make it an overload for convenience
    public static void execute(this Executor executor, Action action) => executor.execute(action.ToRunnable());

    private sealed class DelegateRunnable(Action action) : Runnable
    {
        public void run() => action();
    }
}