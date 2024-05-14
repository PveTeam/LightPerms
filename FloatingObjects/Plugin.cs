using System.IO;
using System.Windows;
using System.Windows.Controls;
using FloatingObjects.Patches;
using Sandbox;
using Sandbox.Engine.Utils;
using Torch;
using Torch.API;
using Torch.API.Managers;
using Torch.API.Plugins;
using Torch.Managers.PatchManager;
using Torch.Views;

namespace FloatingObjects;

public class Plugin : TorchPluginBase, IWpfPlugin
{
    private Persistent<Config> _persistent = null!;

    public override void Init(ITorchBase torch)
    {
        base.Init(torch);
        
        _persistent = Persistent<Config>.Load(Path.Combine(StoragePath, "FloatingObjects.cfg"));
        
        Torch.GameStateChanged += delegate(MySandboxGame _, TorchGameState state)
        {
            if (state != TorchGameState.Loaded || !_persistent.Data.Enabled) return;
            
            var manager = Torch.Managers.GetManager<PatchManager>();
            var patchContext = manager.AcquireContext();
            
            if (_persistent.Data.DisableEjection) EjectPatch.Patch(patchContext);
            if (_persistent.Data.AutoOrePickup) HandDrillPatch.Patch(patchContext);
            if (_persistent.Data.StackDropItems) StackItemsPatch.Patch(patchContext);
            if (_persistent.Data.DisableAmmoDetonation) AmmoDetonationPatch.Patch(patchContext);
            MyFakes.ENABLE_SCRAP = !_persistent.Data.DisableScrap;
            
            manager.Commit();
        };
    }

    public UserControl GetControl() =>
        new()
        {
            Content = new PropertyGrid
            {
                Margin = new Thickness(3)
            },
            DataContext = _persistent.Data
        };
}