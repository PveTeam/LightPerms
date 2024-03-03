using java.lang;
using java.util;
using java.util.concurrent;
using LuckPerms.Torch.Discord.Utils;
using LuckPerms.Torch.Utils.Extensions;
using net.dv8tion.jda.api;
using net.dv8tion.jda.api.hooks;
using net.dv8tion.jda.api.requests;
using net.dv8tion.jda.api.utils.cache;
using NLog;
using Torch.API;
using Torch.Managers;

namespace LuckPerms.Torch.Discord.Managers;

public class DiscordManager(ITorchBase torch, string token, long mainGuildId) : Manager(torch)
{
    public long MainGuildId { get; } = mainGuildId;
    private static readonly ILogger Log = LogManager.GetCurrentClassLogger();

    public JDA Client { get; private set; } = null!;
    
    public override void Attach()
    {
        var builder = JDABuilder.create(GatewayIntent.GUILD_MEMBERS, GatewayIntent.GUILD_MESSAGES, GatewayIntent.MESSAGE_CONTENT)
            .setToken(token)
            .setEventPool(new TorchExecutor(Torch))
            .disableCache(CacheFlag.ACTIVITY, CacheFlag.VOICE_STATE, CacheFlag.STICKER,
                CacheFlag.CLIENT_STATUS, CacheFlag.ONLINE_STATUS, CacheFlag.SCHEDULED_EVENTS);
        
        foreach (var listenerAdapter in Torch.CurrentSession.Managers.AttachOrder.OfType<ListenerAdapter>())
        {
            builder.addEventListeners(listenerAdapter);
        }
        
        Client = builder.build();
        
        Log.Info("Initializing Discord client...");

        Client.awaitReady();
    }
    
    private class TorchExecutor(ITorchBase torch) : ExecutorService
    {
        private bool _shutdown;
        
        public void execute(Runnable command) => 
            torch.Invoke(command.run);

        public void shutdown()
        {
            _shutdown = true;
        }

        public List shutdownNow()
        {
            shutdown();
            return com.sun.tools.javac.util.List.nil();
        }

        public bool isShutdown() => _shutdown;

        public bool isTerminated() => _shutdown;

        public bool awaitTermination(long timeout, TimeUnit unit)
        {
            return true;
        }

        public Future submit(Callable task)
        {
            return torch.InvokeAsync(task.call).AsFuture();
        }

        public Future submit(Runnable task, object result)
        {
            return torch.InvokeAsync(() =>
            {
                task.run();
                return result;
            }).AsFuture();
        }

        public Future submit(Runnable task)
        {
            return torch.InvokeAsync(task.run).AsFuture();
        }

        public List invokeAll(Collection tasks)
        {
            var array = tasks.iterator().AsEnumerable<Callable>().Select<Callable, Task>(b => torch.InvokeAsync(b.call)).ToArray();

            try
            {
                Task.WaitAll(array);
            }
            catch
            {
                // we don't care
            }
            
            return (List)array.Select(b => ((Task<object>)b).AsFuture()).ToCollection();
        }

        public List invokeAll(Collection tasks, long timeout, TimeUnit unit)
        {
            var array = tasks.iterator().AsEnumerable<Callable>().Select<Callable, Task>(b => torch.InvokeAsync(b.call)).ToArray();

            try
            {
                Task.WaitAll(array, TimeSpan.FromMilliseconds(unit.toMillis(timeout)));
            }
            catch (AggregateException e) when (e.InnerExceptions is [OperationCanceledException canceledException])
            {
                throw new InterruptedException(canceledException.Message);
            }
            catch
            {
                // we don't care
            }
            
            return (List)array.Select(b => ((Task<object>)b).AsFuture()).ToCollection();
        }

        public object invokeAny(Collection tasks)
        {
            var array = tasks.iterator().AsEnumerable<Callable>().Select<Callable, Task>(b => torch.InvokeAsync(b.call)).ToArray();

            var idx = Task.WaitAny(array);
            
            return ((Task<object>)array[idx]).Result;
        }

        public object invokeAny(Collection tasks, long timeout, TimeUnit unit)
        {
            var array = tasks.iterator().AsEnumerable<Callable>().Select<Callable, Task>(b => torch.InvokeAsync(b.call)).ToArray();

            var idx = Task.WaitAny(array, TimeSpan.FromMilliseconds(unit.toMillis(timeout)));
            
            return ((Task<object>)array[idx]).Result;
        }
    }
}