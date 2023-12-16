using LuckPerms.Torch.Api.Managers;
using LuckPerms.Torch.Discord.Abstractions;
using LuckPerms.Torch.Utils.Extensions;
using net.dv8tion.jda.api.entities;
using net.luckperms.api.context;
using Torch.API;
using Torch.API.Managers;
using Torch.Managers;

namespace LuckPerms.Torch.Discord.Managers;

public class LuckPermsCalculatorManager : IManager, ContextCalculator
{
    private const string ContextLinked = "discordsrv:linked";
    private const string ContextBoosting = "discordsrv:boosting";
    private const string ContextRole = "discordsrv:role";
    private const string ContextRoleId = "discordsrv:role_id";
    private const string ContextServerId = "discordsrv:server_id";
    
    [Manager.Dependency]
    private readonly DiscordManager _discordManager = null!;
    
    [Manager.Dependency]
    private readonly ILinkManager _linkManager = null!;
    
    [Manager.Dependency]
    private readonly ILuckPermsPlatformManager _platformManager = null!;
    
    public void Attach()
    {
        _platformManager.Api.getContextManager().registerCalculator(this);
    }

    public void Detach()
    {
        _platformManager.Api.getContextManager().unregisterCalculator(this);
    }

    public void calculate(object obj, ContextConsumer consumer)
    {
        var player = (IPlayer)obj;

        var discordId = _linkManager.ResolveDiscordId(player);
        
        consumer.accept(ContextLinked, discordId.HasValue ? "true" : "false");
        
        if (!discordId.HasValue)
            return;
        
        var member = _discordManager.Client.getGuildById(_discordManager.MainGuildId)
            .getMemberById(discordId.Value);
        
        consumer.accept(ContextBoosting, member?.isBoosting() is true ? "true" : "false");
        
        if (member is null)
            return;
        
        consumer.accept(ContextServerId, member.getGuild().getId());
        
        foreach (var role in member.getRoles().iterator().AsEnumerable<Role>())
        {
            if (string.IsNullOrEmpty(role.getName()))
                continue;

            consumer.accept(ContextRoleId, role.getId());
            consumer.accept(ContextRole, role.getName());
        }
    }

    public ContextSet estimatePotentialContexts()
    {
        var builder = ImmutableContextSet.builder();
        
        builder.add(ContextLinked, "true");
        builder.add(ContextLinked, "false");
        
        builder.add(ContextBoosting, "true");
        builder.add(ContextBoosting, "false");
        
        foreach (var role in _discordManager.Client.getGuildById(_discordManager.MainGuildId).getRoles()
                     .iterator().AsEnumerable<Role>())
        {
            if (string.IsNullOrEmpty(role.getName()))
                continue;

            builder.add(ContextRoleId, role.getId());
            builder.add(ContextRole, role.getName());
        }

        builder.add(ContextServerId, _discordManager.MainGuildId.ToString());
        
        return builder.build();
    }
}