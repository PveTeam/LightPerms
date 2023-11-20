using System;
using System.Linq;
using java.util;
using java.util.stream;
using LuckPerms.Torch.Extensions;
using LuckPerms.Torch.Impl.Calculator;
using LuckPerms.Torch.Impl.Listeners;
using LuckPerms.Torch.Impl.Messaging;
using LuckPerms.Torch.ModApi;
using LuckPerms.Torch.PlatformHooks;
using LuckPerms.Torch.Utils.Extensions;
using me.lucko.luckperms.common.api;
using me.lucko.luckperms.common.calculator;
using me.lucko.luckperms.common.command;
using me.lucko.luckperms.common.config.generic.adapter;
using me.lucko.luckperms.common.context.manager;
using me.lucko.luckperms.common.dependencies;
using me.lucko.luckperms.common.@event;
using me.lucko.luckperms.common.messaging;
using me.lucko.luckperms.common.model;
using me.lucko.luckperms.common.model.manager.group;
using me.lucko.luckperms.common.model.manager.track;
using me.lucko.luckperms.common.model.manager.user;
using me.lucko.luckperms.common.plugin;
using me.lucko.luckperms.common.plugin.bootstrap;
using me.lucko.luckperms.common.plugin.util;
using me.lucko.luckperms.common.sender;
using Torch.API;
using Torch.API.Managers;
using Torch.API.Session;
using Torch.Managers.PatchManager;
using Torch.Server.Managers;

namespace LuckPerms.Torch.Impl;

public class LpTorchPlugin(LuckPermsBootstrap bootstrap, ITorchBase torch) : AbstractLuckPermsPlugin
{
    private StandardUserManager? _userManager;
    private StandardGroupManager? _groupManager;
    private StandardTrackManager? _trackManager;
    private LpCommandManager? _commandManager;
    private LpContextManager? _contextManager;
    private LpSenderFactory? _senderFactory;
    private LpConnectionListener? _connectionListener;
    private global::Torch.Managers.DependencyManager? _dependencyManager;
    public override LuckPermsBootstrap getBootstrap() => bootstrap;

    protected override void setupSenderFactory()
    {
        _senderFactory = new(this);
    }

    public override Sender getConsoleSender() => _senderFactory?.wrap(torch) ?? throw new InvalidOperationException("call setupSenderFactory first");

    protected override ConfigurationAdapter provideConfigurationAdapter() => new LpConfigurationAdapter(this, resolveConfig("config.yml"));

    protected override void registerPlatformListeners()
    {
    }

    protected override MessagingFactory provideMessagingFactory() => new LpMessagingFactory(this);

    protected override void registerCommands()
    {
        _commandManager = new(this, _senderFactory!);
    }

    protected override void setupManagers()
    {
        _userManager = new(this);
        _groupManager = new(this);
        _trackManager = new(this);
        _connectionListener = new(this);
        if (torch.CurrentSession is null)
        {
            torch.Managers.GetManager<ITorchSessionManager>().AddFactory(_ => _senderFactory);
            torch.Managers.GetManager<ITorchSessionManager>().AddFactory(_ => _commandManager);
            torch.Managers.GetManager<ITorchSessionManager>().AddFactory(_ => _connectionListener);
            torch.Managers.GetManager<ITorchSessionManager>().AddFactory(_ => new ModApiManager());
        }
        else
        {
            _dependencyManager = new(torch.CurrentSession.Managers);
            
            _dependencyManager.AddManager(_senderFactory);
            _dependencyManager.AddManager(_commandManager);
            _dependencyManager.AddManager(_connectionListener);
            _dependencyManager.AddManager(new ModApiManager());
        }
    }

    protected override CalculatorFactory provideCalculatorFactory() => new LpCalculatorFactory(this);

    protected override void setupContextManager()
    {
        _contextManager = new(this);
    }

    public override GroupManager getGroupManager() => _groupManager ?? throw new InvalidOperationException("call setupManagers first");

    public override TrackManager getTrackManager() => _trackManager ?? throw new InvalidOperationException("call setupManagers first"); 

    public override CommandManager getCommandManager() => _commandManager ?? throw new InvalidOperationException("call registerCommands first");

    public override AbstractConnectionListener getConnectionListener() => _connectionListener ?? throw new InvalidOperationException("call setupManagers first");

    public override Optional getQueryOptionsForUser(User value)
    {
        var player = bootstrap.getPlayer(value.getUniqueId());
        if (!player.isPresent())
            return Optional.empty();

        var options = _contextManager?.getQueryOptions(player.get());
        
        return options is null ? Optional.empty() : Optional.of(options);
    }

    public override Stream getOnlineSenders() =>
        Stream.concat(
            Stream.of(getConsoleSender()),
            torch.CurrentSession?.Managers.GetManager<MultiplayerManagerDedicated>().Players.Values
                .Select(_senderFactory!.wrap).ToCollection().stream() ?? Stream.empty()
        );

    public override ContextManager getContextManager() => _contextManager ?? throw new InvalidOperationException("call setupContextManager first"); 

    public override UserManager getUserManager() => _userManager ?? throw new InvalidOperationException("call setupManagers first");

    protected override void setupPlatformHooks()
    {
        var patchManager = torch.Managers.GetManager<PatchManager>();
        var context = patchManager.AcquireContext();
        
        PlayerPatch.Patch(context);
        CommandPermissionsPatch.Patch(context);
        CommandPrefixPatch.Patch(context);
        
        patchManager.Commit();

        if (_dependencyManager is null)
        {
            torch.Managers.GetManager<ITorchSessionManager>().AddFactory(_ => _contextManager);
        }
        else
        {
            _dependencyManager.AddManager(_contextManager);
            
            _dependencyManager.Attach();
        }
    }
    
    protected override void removePlatformHooks()
    {
        base.removePlatformHooks();
        _dependencyManager?.Detach();
    }

    protected override AbstractEventBus provideEventBus(LuckPermsApiProvider luckPermsApiProvider) => new LpEventBus(this, luckPermsApiProvider, torch);

    protected override void registerApiOnPlatform(net.luckperms.api.LuckPerms luckPerms)
    {
    }

    protected override void performFinalSetup()
    {
    }

    protected override DependencyManager createDependencyManager() => new LpDependencyManager();
}