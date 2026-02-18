using System.Windows.Forms;
using System.Windows.Forms.Integration;
using SilverDasher.ACT.Storages;
using SilverDasher.ACT.ViewModels;
using SilverDasher.ACT.Views;

namespace SilverDasher.ACT.Doppelgangers;

internal class Painter : Doppelganger
{
	internal PluginControl pluginControl;

	internal ElementHost controlHost;

	internal Label lblStatus;

	internal TabPage pagePlugin;

	internal Painter(SilverDasher silverDasher, Label pluginStatusText, TabPage pluginScreenSpace)
		: base(silverDasher)
	{
		lblStatus = pluginStatusText;
		pagePlugin = pluginScreenSpace;
	}

	internal override void Init()
	{
		if (pluginControl == null)
		{
			pluginControl = new PluginControl(this);
			controlHost = new ElementHost
			{
				Dock = DockStyle.Fill,
				Child = pluginControl
			};
		}
		pagePlugin.Text = DataStorage.Title;
		pagePlugin.Controls.Clear();
		pagePlugin.Controls.Add(controlHost);
		pluginControl.SetPluginStatus(PluginStatus.INITIALIZED);
	}

	internal override void Deinit()
	{
		lblStatus.Text = "收回了银山雀儿。(Exited)";
	}

	internal void RepaintNodes()
	{
		PluginViewModel obj = pluginControl.DataContext as PluginViewModel;
		obj.ClearAllNodes();
		obj.SetPatchNames(Painter.Keeper.Patches.PatchByCode);
		obj.SetupHuntMobs(Painter.Keeper.Mobs.HuntByBnpcNameID, Painter.Keeper.SpHunts.HuntGroupsTree, Keeper.Config.HuntSubscriptions);
		obj.SetupFates(Painter.Keeper.Fates.FateByID, Painter.Keeper.SpFates.FateGroupsTree, Keeper.Config.FateSubscriptions);
	}

	internal void SetPluginStatus(string text)
	{
		lblStatus.Text = text;
	}
}
