using Torch;
using Torch.API.Managers;

namespace LuckPerms.Torch.ModApi;

public static class TorchApi
{
    public static IDependencyProvider Managers =>
#pragma warning disable CS0618 // Type or member is obsolete
        TorchBase.Instance.CurrentSession?.Managers ?? TorchBase.Instance.Managers;
#pragma warning restore CS0618 // Type or member is obsolete
}