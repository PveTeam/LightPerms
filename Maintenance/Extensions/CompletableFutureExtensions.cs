using java.util.concurrent;
using java.util.function;
using Torch.Utils;

namespace Maintenance.Extensions;

public static class CompletableFutureExtensions
{
    [ReflectedStaticMethod(Type = typeof(Consumer), Name = "<default>andThen")]
    private static readonly Func<Consumer, Consumer, Consumer> AndThenDefault = null!;
    
    public static Task<T> ToTask<T>(this CompletableFuture completableFuture)
    {
        var taskCompletionSource = new TaskCompletionSource<T>();

        completableFuture.thenAccept(new TaskConsumer<T>(taskCompletionSource));

        return taskCompletionSource.Task;
    }

    private sealed class TaskConsumer<T>(TaskCompletionSource<T> completionSource) : Consumer
    {
        
        public void accept(object t)
        {
            completionSource.SetResult((T)t);
        }

        public Consumer andThen(Consumer after) => AndThenDefault(this, after);
    }
}