using java.util;

namespace LuckPerms.Torch.Utils.Extensions;

public static class UuidExtensions
{
    public static ulong GetSteamId(this UUID uuid) => (ulong)uuid.getLeastSignificantBits();
    
    public static UUID GetUuid(this ulong steamId) => new(0, (long)steamId);
}