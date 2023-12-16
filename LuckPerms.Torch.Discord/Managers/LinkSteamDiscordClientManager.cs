using System.Net;
using System.Net.Http;
using Microsoft.Extensions.Configuration;
using NLog;
using Torch.API.Managers;

namespace LuckPerms.Torch.Discord.Managers;

public class LinkSteamDiscordClientManager(IConfiguration configuration) : IManager
{
    private static readonly ILogger Log = LogManager.GetCurrentClassLogger();
    
    public string ApiUrl => _client.BaseAddress!.ToString();
    
    private readonly HttpClient _client = new()
    {
        BaseAddress = new(configuration.GetValue<string>("backendUrl") ??
                          throw new ArgumentNullException(nameof(configuration), "backendUrl is null")),
        DefaultRequestHeaders =
        {
            {
                "X-API-Key",
                configuration.GetValue<string>("apiKey") ??
                throw new ArgumentNullException(nameof(configuration), "apiKey is null")
            } 
        }
    };

    public void Attach()
    {
    }

    public void Detach()
    {
        _client.Dispose();
    }

    /// <summary>
    /// Checks if a Steam ID is linked.
    /// </summary>
    /// <param name="steamId">The Steam ID to check.</param>
    /// <returns>
    /// A boolean value indicating whether the Steam ID is linked.
    /// </returns>
    /// <exception cref="HttpRequestException">Thrown when an HTTP request to the check API fails.</exception>
    public async Task<bool> IsSteamIdLinkedAsync(ulong steamId)
    {
        using var response = await _client.GetAsync($"steam/check/{steamId}");
        
        if (response.IsSuccessStatusCode) return true;

        if (response.StatusCode is HttpStatusCode.NotFound) return false;
        
        Log.Warn("Failed to check steam id {0}\n{1}", steamId, response);
        
        response.EnsureSuccessStatusCode();
        
        return false; // should never happen
    }

    /// <summary>
    /// Retrieves the Steam ID associated with the given Discord ID asynchronously.
    /// </summary>
    /// <param name="discordId">The Discord ID to lookup the associated Steam ID for.</param>
    /// <returns>The associated Steam ID if found, otherwise null.</returns>
    /// <exception cref="HttpRequestException">Thrown when an HTTP request to the lookup API fails.</exception>
    public async Task<ulong?> LookupSteamIdAsync(ulong discordId)
    {
        using var response = await _client.GetAsync($"lookup/discord/{discordId}");

        if (response.IsSuccessStatusCode) return ulong.Parse(await response.Content.ReadAsStringAsync());

        if (response.StatusCode is HttpStatusCode.NotFound) return null;
        
        Log.Warn("Failed to lookup discord id {0}\n{1}", discordId, response);
        response.EnsureSuccessStatusCode();

        return null; // should never happen
    }

    /// <summary>
    /// Retrieves the Discord ID associated with a given Steam ID asynchronously.
    /// </summary>
    /// <param name="steamId">The Steam ID to look up.</param>
    /// <returns>The associated Discord ID, or null if no association exists.</returns>
    /// <exception cref="HttpRequestException">Thrown when an HTTP request to the lookup API fails.</exception>
    public async Task<ulong?> LookupDiscordIdAsync(ulong steamId)
    {
        using var response = await _client.GetAsync($"lookup/steam/{steamId}");
        
        if (response.IsSuccessStatusCode) return ulong.Parse(await response.Content.ReadAsStringAsync());

        if (response.StatusCode is HttpStatusCode.NotFound) return null;
        
        Log.Warn("Failed to lookup steam id {0}\n{1}", steamId, response);
        response.EnsureSuccessStatusCode();

        return null; // should never happen
    }
}