using me.lucko.luckperms.common.messaging;
using me.lucko.luckperms.common.plugin;

namespace LuckPerms.Torch.Impl.Messaging;

public class LpMessagingFactory(LuckPermsPlugin plugin) : MessagingFactory(plugin)
{
    protected override InternalMessagingService getServiceFor(string messagingType)
    {
        return base.getServiceFor(messagingType); // TODO add nexus messaging
    }
}