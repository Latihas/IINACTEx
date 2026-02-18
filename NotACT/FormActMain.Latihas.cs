using System.ComponentModel;

namespace Advanced_Combat_Tracker;

public partial class FormActMain {
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public List<ActPluginData> ActPlugins { get; set; } = [];
    

    public ActPluginData? PluginGetSelfData(IActPluginV1 MyPluginInstance) {
        return ActGlobals.oFormActMain.ActPlugins.FirstOrDefault(t => t.pluginObj == MyPluginInstance);
    }

    public static void AddDefaultPlugins(dynamic o1, IActPluginV1 o2,IActPluginV1 o3,IActPluginV1 o4) {
        ActGlobals.oFormActMain.ActPlugins.Add(new ActPluginData("FFXIV_ACT_Plugin.dll", o1.ffxivActPlugin, false,false));
        ActGlobals.oFormActMain.ActPlugins.Add(new ActPluginData("OverlayPlugin.dll", o2, false,false));
        ActGlobals.oFormActMain.ActPlugins.Add(new ActPluginData("Triggernometry.dll", o3, false,false));
        ActGlobals.oFormActMain.ActPlugins.Add(new ActPluginData("PostNamazu.dll", o4, false,false));
    }
}