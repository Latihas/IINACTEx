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

	public static void AddFFXIV_ACT_Plugin(dynamic o1) => oFormActMain.ActPlugins.Add(new ActPluginData("FFXIV_ACT_Plugin.dll", o1.ffxivActPlugin, false, false));
}