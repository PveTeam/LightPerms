using java.lang;
using java.util.concurrent;
using me.lucko.luckperms.common.plugin.bootstrap;
using me.lucko.luckperms.common.plugin.scheduler;
using Torch.API;

namespace LuckPerms.Torch.Impl;

public class LpSchedulerAdapter(LuckPermsBootstrap bootstrap, ITorchBase torch) : AbstractJavaScheduler(bootstrap)
{
    private readonly Executor _torchExecutor = new TorchExecutor(torch);

    public override Executor sync() => _torchExecutor;

    private class TorchExecutor(ITorchBase torch) : Executor
    {
        public void execute(Runnable command) => torch.Invoke(command.run);
    }
}