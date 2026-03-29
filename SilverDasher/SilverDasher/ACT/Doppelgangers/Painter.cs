using SilverDasher.ACT.Views;

namespace SilverDasher.ACT.Doppelgangers;

public class Painter(SilverDasher silverDasher) : Doppelganger(silverDasher) {
	internal PluginControl pluginControl;

	internal override void Init() {
		pluginControl ??= new PluginControl(this);
		PluginControl.SetPluginStatus(PluginStatus.INITIALIZED);
	}

	internal override void Deinit() {
	}

	internal void RepaintNodes() {
		var obj = pluginControl.ViewModel;
		obj.ClearAllNodes();
		obj.SetPatchNames(Painter.Keeper.Patches.PatchByCode);
		obj.SetupHuntMobs(Painter.Keeper.Mobs.HuntByBnpcNameID, Painter.Keeper.SpHunts.HuntGroupsTree, Keeper.Config.HuntSubscriptions);
		obj.SetupFates(Painter.Keeper.Fates.FateByID, Painter.Keeper.SpFates.FateGroupsTree, Keeper.Config.FateSubscriptions);
	}

	public void DrawImGui() => pluginControl.Draw();
}