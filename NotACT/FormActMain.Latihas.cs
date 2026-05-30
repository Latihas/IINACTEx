using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using static System.ComponentModel.DesignerSerializationVisibility;
using static Advanced_Combat_Tracker.ActGlobals;

namespace Advanced_Combat_Tracker;

public partial class FormActMain {
	[DesignerSerializationVisibility(Hidden)]
	[SuppressMessage("ReSharper", "AutoPropertyCanBeMadeGetOnly.Global")]
	public List<ActPluginData> ActPlugins { get; set; } = [];

	[SuppressMessage("ReSharper", "UnusedMember.Global")]
	[SuppressMessage("Performance", "CA1822")]
	public ActPluginData? PluginGetSelfData(IActPluginV1 MyPluginInstance) =>
		oFormActMain.ActPlugins.FirstOrDefault(t => t.pluginObj == MyPluginInstance);

	public static void AddDefaultPlugins(dynamic o1, IActPluginV1 o2, IActPluginV1 o3, IActPluginV1 o4) {
		oFormActMain.ActPlugins.Add(new ActPluginData("FFXIV_ACT_Plugin.dll", o1.ffxivActPlugin, false, false));
		oFormActMain.ActPlugins.Add(new ActPluginData("OverlayPlugin.dll", o2, false, false));
		oFormActMain.ActPlugins.Add(new ActPluginData("Triggernometry.dll", o3, false, false));
		oFormActMain.ActPlugins.Add(new ActPluginData("PostNamazu.dll", o4, false, false));
	}
}