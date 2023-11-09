using me.lucko.luckperms.common.cacheddata.result;
using me.lucko.luckperms.common.calculator.processor;
using net.luckperms.api.util;

namespace LuckPerms.Torch.Impl.Calculator;

public class ConsoleProcessor : AbstractPermissionProcessor
{
    private static readonly TristateResult TrueResult =
        new TristateResult.Factory(typeof(ConsoleProcessor)).result(Tristate.TRUE);

    public static readonly ConsoleProcessor Instance = new();
    
    protected override TristateResult hasPermission(string str) => TrueResult;
}