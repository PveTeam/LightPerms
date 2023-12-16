using java.util.concurrent;

namespace LuckPerms.Torch.Discord.Utils;

public static class TaskExtensions
{
    public static Future AsFuture(this Task task)
    {
        return new FutureAdapter(task);
    }
    
    public static Future AsFuture<T>(this Task<T> task)
    {
        return new FutureAdapter<T>(task);
    }
}

public class FutureAdapter<T>(Task<T> task) : Future
{
    public bool cancel(bool mayInterruptIfRunning)
    {
        throw new InvalidOperationException("Task is not cancellable");
    }

    public bool isCancelled() => task.IsCanceled;

    public bool isDone() => task.Status is TaskStatus.RanToCompletion or TaskStatus.Faulted or TaskStatus.Canceled;

    public object? get()
    {
        if (!isDone())
            try
            {
                task.Wait();
            } 
            catch (AggregateException e) when (e.InnerExceptions is [ TaskCanceledException canceledException ])
            {
                throw new CancellationException(canceledException.Message);
            }
            catch (AggregateException e)
            {
                throw new ExecutionException(e);
            }

        if (task.IsCanceled)
            throw new CancellationException();
        if (task.IsFaulted)
            throw new ExecutionException(task.Exception);

        return task.Result;
    }

    public object? get(long timeout, TimeUnit unit)
    {
        if (!isDone())
            try
            {
                task.Wait(TimeSpan.FromMilliseconds(unit.toMillis(timeout)));
            } // TODO: InterruptedException and TimeoutException
            catch (AggregateException e) when (e.InnerExceptions is [ TaskCanceledException canceledException ])
            {
                throw new CancellationException(canceledException.Message);
            }
            catch (AggregateException e)
            {
                throw new ExecutionException(e);
            }

        if (task.IsCanceled)
            throw new CancellationException();
        if (task.IsFaulted)
            throw new ExecutionException(task.Exception);

        return task.Result;
    }
}

public class FutureAdapter(Task task) : Future
{
    public bool cancel(bool mayInterruptIfRunning)
    {
        throw new InvalidOperationException("Task is not cancellable");
    }

    public bool isCancelled() => task.IsCanceled;

    public bool isDone() => task.Status is TaskStatus.RanToCompletion or TaskStatus.Faulted or TaskStatus.Canceled;

    public object? get()
    {
        if (!isDone())
            try
            {
                task.Wait();
            } 
            catch (AggregateException e) when (e.InnerExceptions is [ TaskCanceledException canceledException ])
            {
                throw new CancellationException(canceledException.Message);
            }
            catch (AggregateException e)
            {
                throw new ExecutionException(e);
            }

        if (task.IsCanceled)
            throw new CancellationException();
        if (task.IsFaulted)
            throw new ExecutionException(task.Exception);

        return null;
    }

    public object? get(long timeout, TimeUnit unit)
    {
        if (!isDone())
            try
            {
                task.Wait(TimeSpan.FromMilliseconds(unit.toMillis(timeout)));
            } // TODO: InterruptedException and TimeoutException
            catch (AggregateException e) when (e.InnerExceptions is [ TaskCanceledException canceledException ])
            {
                throw new CancellationException(canceledException.Message);
            }
            catch (AggregateException e)
            {
                throw new ExecutionException(e);
            }

        if (task.IsCanceled)
            throw new CancellationException();
        if (task.IsFaulted)
            throw new ExecutionException(task.Exception);

        return null;
    }
}