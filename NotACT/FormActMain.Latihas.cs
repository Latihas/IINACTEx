using System.ComponentModel;

namespace Advanced_Combat_Tracker;

public partial class FormActMain {
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public List<ActPluginData> ActPlugins { get; set; } = [];
    

    public ActPluginData? PluginGetSelfData(IActPluginV1 MyPluginInstance) {
        return ActGlobals.oFormActMain.ActPlugins.FirstOrDefault(t => t.pluginObj == MyPluginInstance);
    }

    public static void AddDefaultPlugins(dynamic o1, IActPluginV1 o2,IActPluginV1 o3,IActPluginV1 o4) {
        ActGlobals.oFormActMain.ActPlugins.Add(new ActPluginData("_FFXIV_ACT_Plugin", o1.ffxivActPlugin, false));
        ActGlobals.oFormActMain.ActPlugins.Add(new ActPluginData("_OverlayPlugin", o2, false));
        ActGlobals.oFormActMain.ActPlugins.Add(new ActPluginData("_Triggernometry", o3, false));
        ActGlobals.oFormActMain.ActPlugins.Add(new ActPluginData("_PostNamazu", o4, false));
    }
}