using System;
using System.IO;
using System.Linq;
using java.io;
using java.lang;
using java.nio.file;
using java.time;
using java.util;
using java.util.concurrent;
using LuckPerms.Torch.Extensions;
using LuckPerms.Torch.Utils.Extensions;
using me.lucko.luckperms.common.plugin.bootstrap;
using me.lucko.luckperms.common.plugin.classpath;
using me.lucko.luckperms.common.plugin.logging;
using me.lucko.luckperms.common.plugin.scheduler;
using net.luckperms.api.platform;
using NLog;
using Sandbox.Game.Multiplayer;
using Torch.API;
using Torch.API.Managers;
using Torch.API.Plugins;
using Torch.Server.Managers;
using Path = java.nio.file.Path;

namespace LuckPerms.Torch.Impl;

public class LpTorchBootstrap : LuckPermsBootstrap
{
    private SchedulerAdapter? _schedulerAdapter;
    private ClassPathAppender? _classPathAppender;
    private readonly PluginLogger _pluginLogger;
    public CountDownLatch LoadLatch { get; } = new(1);
    public CountDownLatch EnableLatch { get; } = new(1);
    private readonly ITorchServer _torch;
    private readonly ITorchPlugin _torchPlugin;
    private readonly Path _configDir;
    private readonly Path _dataDir;

    public LpTorchPlugin Plugin { get; }

    public LpTorchBootstrap(ITorchServer torch, ITorchPlugin torchPlugin, ILogger logger, string configDir)
    {
        _pluginLogger = new NLogPluginLogger(logger);
        _torch = torch;
        _torchPlugin = torchPlugin;
        var configDirInfo = Directory.CreateDirectory(configDir);
        _configDir = Paths.get(configDirInfo.FullName);
        _dataDir = Paths.get(configDirInfo.CreateSubdirectory("data").FullName);
        Plugin = new(this, torch);
    }

    public ITorchPlugin GetTorchPlugin() => _torchPlugin;

    public Path getDataDirectory() => _dataDir;

    public string identifyClassLoader(ClassLoader classLoader) => classLoader.toString();

    public Collection getPlayerList() => Sync.Players?.GetAllPlayers()
        .Select(b => Sync.Players.TryGetPlayerIdentity(b)?.DisplayName).Where(b => b is not null).ToCollection() ?? Collections.EMPTY_LIST;

    public Platform.Type getType() => Platform.Type.BUKKIT; // meh

    public SchedulerAdapter getScheduler() => _schedulerAdapter ??= new LpSchedulerAdapter(this, _torch);

    public Path getConfigDirectory() => _configDir;

    public bool isPlayerOnline(UUID uuid) =>
        _torch.CurrentSession?.Managers.GetManager<MultiplayerManagerDedicated>().Players
            .ContainsKey(uuid.GetSteamId()) ?? false;

    public string getVersion() => _torchPlugin.Version.TrimStart('v');

    public string getServerBrand() => "Torch";

    public string getServerVersion() => _torch.TorchVersion.ToString();

    public int getPlayerCount() =>
        _torch.CurrentSession?.Managers.GetManager<MultiplayerManagerDedicated>().Players.Count ?? 0;

    public Instant getStartupTime() => Instant.ofEpochMilli((DateTimeOffset.Now - _torch.ElapsedPlayTime).ToUnixTimeMilliseconds());

    public ClassPathAppender getClassPathAppender() => _classPathAppender ??= new LpClassPathAppender();

    public InputStream? getResourceStream(string path)
    {
        var normalizedPath = path.Replace('/', '.');
        using var dotnetStream = typeof(LpTorchBootstrap).Assembly.GetManifestResourceStream(normalizedPath);
        
        return dotnetStream?.GetInputStream();
    }

    public PluginLogger getPluginLogger() => _pluginLogger;

    public Optional lookupUniqueId(string str)
    {
        if (Sync.Players?.GetAllIdentities().FirstOrDefault(b => b.DisplayName == str)?.IdentityId is
                { } identityId && Sync.Players.TryGetSteamId(identityId) is { } steamId and > 0)
            return Optional.of(steamId.GetUuid());
        
        return Optional.empty();
    }

    public Optional lookupUsername(UUID uuid)
    {
        if (Sync.Players?.TryGetPlayerIdentity(uuid.GetSteamId())?.DisplayName is { } displayName)
            return Optional.of(displayName);
        
        return Optional.empty();
    }

    public Collection getOnlinePlayers()
    {
        return _torch.CurrentSession?.Managers.GetManager<MultiplayerManagerDedicated>().Players.Select(b => b.Key.GetUuid())
            .ToCollection() ?? Collections.EMPTY_LIST;
    }

    public CountDownLatch getLoadLatch() => LoadLatch;

    public CountDownLatch getEnableLatch() => EnableLatch;

    public string getServerName() => _torch.Managers.GetManager<InstanceManager>().DedicatedConfig.ServerName;

    public Optional getPlayer(UUID uuid) => _torch.CurrentSession?.Managers.GetManager<MultiplayerManagerDedicated>().Players.TryGetValue(uuid.GetSteamId(), out var player) ?? false
        ? Optional.of(player)
        : Optional.empty();
}