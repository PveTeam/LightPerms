using LuckPerms.Torch.Api;
using LuckPerms.Torch.Impl;
using me.lucko.luckperms.common.context.manager;
using me.lucko.luckperms.common.model;
using me.lucko.luckperms.common.verbose.@event;
using net.luckperms.api.query;
using net.luckperms.api.util;
using Torch.ViewModels;

namespace LuckPerms.Torch.PlatformApi;

public class LpPlayerModel(ulong steamId, string? name = null) : PlayerViewModel(steamId, name), ILpPlayerModel
{
    public QueryOptionsCache? QueryOptionsCache { get; private set; }
    public User? LpUser { get; private set; }

    internal void InitializePermissions(User user)
    {
        LpUser = user;

        QueryOptionsCache ??= ((LpContextManager)user.getPlugin().getContextManager()).CreateQueryOptionsCache(this);
    }
    
    public Tristate HasPermission(string permission)
    {
        if (LpUser is null || QueryOptionsCache is null)
            return Tristate.UNDEFINED;

        return HasPermission(permission, QueryOptionsCache.getQueryOptions());
    }
    
    public Tristate HasPermission(string permission, QueryOptions queryOptions)
    {
        if (LpUser is null || QueryOptionsCache is null)
            return Tristate.UNDEFINED;

        var data = LpUser.getCachedData().getPermissionData(queryOptions);

        return data.checkPermission(permission, CheckOrigin.PLATFORM_API_HAS_PERMISSION).result();
    }

    public string? GetOption(string key)
    {
        if (LpUser is null || QueryOptionsCache is null)
            return null;

        return GetOption(key, QueryOptionsCache.getQueryOptions());
    }

    public string? GetOption(string key, QueryOptions queryOptions)
    {
        if (LpUser is null || QueryOptionsCache is null)
            return null;
        
        var cache = LpUser.getCachedData().getMetaData(queryOptions);
        
        return cache.getMetaOrChatMetaValue(key, CheckOrigin.PLATFORM_API);
    }

    public override string ToString() => $"Player {{ SteamId = {SteamId}, Name = {Name} }}";
}