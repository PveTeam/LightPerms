using LuckPerms.Torch.Discord.Config;
using LuckPerms.Torch.Discord.Managers;
using LuckPerms.Torch.Discord.Utils;
using Microsoft.Extensions.Configuration;
using Torch;
using Torch.API;
using Torch.API.Managers;
using Torch.API.Session;
using Torch.Managers;

namespace LuckPerms.Torch.Discord;

public class Plugin : TorchPluginBase
{
    private static readonly Guid LuckPermsGuid = new("7E4B3CC8-64FA-416E-8910-AACDF2DA5E2C");

    public override void Init(ITorchBase torch)
    {
        var storagePath = Directory.CreateDirectory(Path.Combine(StoragePath, "luckperms.discord")).FullName;

        var configManager = new ConfigManager(storagePath);
        torch.Managers.AddManager(configManager);

        var sessionManager = torch.Managers.GetManager<ITorchSessionManager>();

        if (torch.Managers.GetManager<PluginManager>().Plugins.ContainsKey(LuckPermsGuid))
        {
            sessionManager.AddFactory(_ => new LuckPermsCalculatorManager());
            sessionManager.AddFactory(_ => new InternalLinkManager(this));
        }

        sessionManager.AddFactory(s =>
        {
            var globalChatConfig = new GlobalChatMirroringConfig(configManager.Configuration.GetRequiredSection("discord-api:global-chat-mirroring"));

            if (globalChatConfig is not null && configManager.Configuration.GetRequiredSection("discord-api:global-chat-mirroring:enabled").Get<bool>())
            {
                return new GlobalChatMirrorManager(globalChatConfig, s.Torch);
            }

            return null;
        });

        sessionManager.AddFactory(_ =>
        {
            if (configManager.Configuration.GetValue<bool>("discord-api:command-executor:enabled"))
            {
                return new CommandExecutorManager(
                    configManager.Configuration.GetValue("discord-api:command-executor:name", "eval")!
                );
            }

            return null;
        });

        sessionManager.AddFactory(s =>
        {
            var mode = configManager.Configuration["discord-api-mode"] ?? "internal";
            
            if (mode == "sedb" && !s.Torch.Managers.GetManager<IPluginManager>().IsSedbInstalled())
                throw new InvalidOperationException("Sedb not installed");

            var token = mode switch
            {
                "internal" => configManager.Configuration["internal-discord-api:token"],
                "sedb" => s.Torch.Managers.GetManager<IPluginManager>().GetSedbToken(),
                _ => throw new InvalidOperationException("Invalid discord api mode")
            };

            return new DiscordManager(s.Torch, token ?? throw new InvalidOperationException("Missing discord token"),
                configManager.Configuration.GetValue<long?>("discord-api:main-guild-id") ??
                throw new InvalidOperationException("Missing main guild id"));
        });

        /*torch.GameStateChanged += (_, state) =>
        {
            if (state != TorchGameState.Created)
                return;
            
            var mode = configManager.Configuration["link-mode"] ?? "internal";

            switch (mode)
            {
                case "link-steam-discord":
                {
                    sessionManager.AddFactory(_ => new LinkSteamDiscordClientManager(configManager.Configuration));
                    sessionManager.AddFactory(_ => new LinkSteamDiscordLinkManager());
                    break;
                }
                case "internal":
                {
                    sessionManager.AddFactory(_ => new InternalLinkManager(this,
                        Path.Combine(storagePath,
                            configManager.Configuration["internal-link:db-path"] ??
                            throw new InvalidOperationException("Missing internal link db path")),
                        configManager.Configuration.GetValue<ulong?>("internal-link:application-id") ??
                        throw new InvalidOperationException("Missing internal link application id")));
                    break;
                }
            }
        };*/
    }
}