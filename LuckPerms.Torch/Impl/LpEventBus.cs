using System;
using System.Linq;
using me.lucko.luckperms.common.api;
using me.lucko.luckperms.common.@event;
using me.lucko.luckperms.common.plugin;
using Torch.API;
using Torch.API.Managers;

namespace LuckPerms.Torch.Impl;

public class LpEventBus(LuckPermsPlugin plugin, LuckPermsApiProvider apiProvider, ITorchBase torch)
    : AbstractEventBus(plugin, apiProvider)
{
    protected override object checkPlugin(object obj)
    {
        return torch.Managers.GetManager<IPluginManager>()
                   .FirstOrDefault(b => b.GetType().Assembly == obj.GetType().Assembly) ??
               throw new InvalidOperationException("plugin not found");
    }
}