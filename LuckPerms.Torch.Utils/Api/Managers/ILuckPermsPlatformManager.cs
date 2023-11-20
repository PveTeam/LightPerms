using Torch.API.Managers;

namespace LuckPerms.Torch.Api.Managers;

public interface ILuckPermsPlatformManager : IManager
{
    net.luckperms.api.LuckPerms Api { get; }
}