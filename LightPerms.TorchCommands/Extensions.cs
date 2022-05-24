using Torch.Commands;
namespace LightPerms.TorchCommands;

public static class Extensions
{
    public static string GetPermissionString(this Command command)
    {
        return $"command.{string.Join(".", command.Path.Select(b => b.ToLowerInvariant()))}";
    }
}
