using System.Linq;
using Torch.Commands;

namespace LuckPerms.Torch.Extensions;

public static class CommandExtensions
{
    public static string GetPermissionString(this Command command)
    {
        return $"minecraft.command.{string.Join(".", command.Path.Select(b => b.ToLowerInvariant()))}";
    }
}
