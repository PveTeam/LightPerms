using System;
using java.util;
using LuckPerms.Torch.Extensions;
using LuckPerms.Torch.PlatformApi;
using LuckPerms.Torch.Utils.Extensions;
using me.lucko.luckperms.common.config;
using me.lucko.luckperms.common.context.manager;
using me.lucko.luckperms.common.plugin;
using net.luckperms.api.context;
using net.luckperms.api.query;
using Torch.API;
using Torch.API.Managers;
using Torch.Managers;
using Torch.Server.Managers;
using VRage.Game.ModAPI;
using Boolean = java.lang.Boolean;
using ContextManager = me.lucko.luckperms.common.context.manager.ContextManager;
using String = java.lang.String;

namespace LuckPerms.Torch.Impl;

public class LpContextManager(LuckPermsPlugin plugin) : ContextManager(plugin, typeof(IPlayer), typeof(IPlayer)), IManager
{
    public static readonly OptionKey PromotionOption = OptionKey.of("keen.promotion", typeof(String));
    public static readonly OptionKey ConsoleOption = OptionKey.of("console", typeof(Boolean));
    
    [Manager.Dependency]
    private MultiplayerManagerDedicated _multiplayerManager = null!;

    public QueryOptionsCache CreateQueryOptionsCache(LpPlayerModel player) => new(player, this);

    public override QueryOptionsSupplier getCacheFor(object obj)
    {
        return ((LpPlayerModel)obj).QueryOptionsCache ??
               throw new ArgumentException($"Trying to get query options for non registered player {obj}");
    }

    protected override void invalidateCache(object obj)
    {
        ((QueryOptionsCache)getCacheFor(obj)).invalidate();
    }

    public override QueryOptions formQueryOptions(object obj, ImmutableContextSet mmutableContextSet)
    {
        var queryOptions = ((QueryOptions)plugin.getConfiguration().get(ConfigKeys.GLOBAL_QUERY_OPTIONS)).toBuilder();
        
        var player = (IPlayer)obj;

        queryOptions.option(PromotionOption,
            _multiplayerManager.GetUserPromoteLevel(player.SteamId).ToString().ToLowerInvariant());

        return queryOptions.context(mmutableContextSet).build();
    }

    public override UUID getUniqueId(object obj)
    {
        return ((IPlayer)obj).SteamId.GetUuid();
    }

    public void Attach()
    {
        _multiplayerManager.PlayerPromoted += MultiplayerManagerOnPlayerPromoted;
    }

    private void MultiplayerManagerOnPlayerPromoted(ulong steamId, MyPromoteLevel arg2)
    {
        signalContextUpdate(_multiplayerManager.Players[steamId]);
    }

    public void Detach()
    {
    }
}