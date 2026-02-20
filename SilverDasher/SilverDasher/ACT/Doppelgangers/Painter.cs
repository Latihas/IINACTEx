using System.Windows.Forms;
using System.Windows.Forms.Integration;
using SilverDasher.ACT.Storages;
using SilverDasher.ACT.ViewModels;
using SilverDasher.ACT.Views;

namespace SilverDasher.ACT.Doppelgangers;

internal class Painter(SilverDasher silverDasher, Label pluginStatusText, TabPage pluginScreenSpace) : Doppelganger(silverDasher) {
    internal PluginControl pluginControl;

    private ElementHost controlHost;

    internal override void Init() {
        if (pluginControl == null) {
            pluginControl = new PluginControl(this);
            controlHost = new ElementHost {
                Dock = DockStyle.Fill,
                Child = pluginControl
            };
        }
        pluginScreenSpace.Text = DataStorage.Title;
        pluginScreenSpace.Controls.Clear();
        pluginScreenSpace.Controls.Add(controlHost);
        pluginControl.SetPluginStatus(PluginStatus.INITIALIZED);
    }

    internal override void Deinit() {
    }
    // => pluginStatusText.Text = "收回了银山雀儿。(Exited)";


    internal void RepaintNodes() {
        var obj = (PluginViewModel)pluginControl.DataContext;
        obj.ClearAllNodes();
        obj.SetPatchNames(Painter.Keeper.Patches.PatchByCode);
        obj.SetupHuntMobs(Painter.Keeper.Mobs.HuntByBnpcNameID, Painter.Keeper.SpHunts.HuntGroupsTree, Keeper.Config.HuntSubscriptions);
        obj.SetupFates(Painter.Keeper.Fates.FateByID, Painter.Keeper.SpFates.FateGroupsTree, Keeper.Config.FateSubscriptions);
    }

    internal void SetPluginStatus(string text) => pluginStatusText.Text = text;
}