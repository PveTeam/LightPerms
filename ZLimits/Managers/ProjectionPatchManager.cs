using Microsoft.Extensions.Configuration;
using Torch.API.Managers;
using Torch.Managers;
using Torch.Managers.PatchManager;
using ZLimits.Patches;

namespace ZLimits.Managers;

internal class ProjectionPatchManager : IManager
{
    [Manager.Dependency]
    private readonly PatchManager _patchManager = null!;
    
    [Manager.Dependency]
    private readonly ConfigManager _configManager = null!;

    private PatchContext? _context;
    
    public void Attach()
    {
        _context = _patchManager.AcquireContext();
        
        if (!_configManager.Configuration.GetValue("countProjectionPcu", true))
            ProjectionPcuPatch.Patch(_context);
        
        _patchManager.Commit();
    }

    public void Detach()
    {
        if (_context is not null) 
            _patchManager.FreeContext(_context);
    }
}