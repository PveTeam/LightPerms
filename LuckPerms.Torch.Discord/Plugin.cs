using LuckPerms.Torch.Discord.Managers;
using LuckPerms.Torch.Discord.Utils;
using Microsoft.Extensions.Configuration;
using Torch;
using Torch.API;
using Torch.API.Managers;
using Torch.API.Session;

namespace LuckPerms.Torch.Discord;

public class Plugin : TorchPluginBase
{
    public override void Init(ITorchBase torch)
    {
        var storagePath = Directory.CreateDirectory(Path.Combine(StoragePath, "luckperms.discord")).FullName;

        var configManager = new ConfigManager(storagePath);
        torch.Managers.AddManager(configManager);

        var sessionManager = torch.Managers.GetManager<ITorchSessionManager>();

        sessionManager.AddFactory(_ => new LuckPermsCalculatorManager());

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
        
        sessionManager.AddFactory(_ => new InternalLinkManager(this));

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