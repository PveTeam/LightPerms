using net.luckperms.api.query;
using net.luckperms.api.util;
using Torch.API;

namespace LuckPerms.Torch.Api;

public interface ILpPlayerModel : IPlayer
{
    Tristate HasPermission(string permission);
    Tristate HasPermission(string permission, QueryOptions queryOptions);
    
    string? GetOption(string key);
    string? GetOption(string key, QueryOptions queryOptions);
}