using System.ComponentModel;

namespace Advanced_Combat_Tracker;

public partial class FormActMain
{
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public List<ActPluginData> ActPlugins { get; set; } = [];
    public ActPluginData PluginGetSelfData(IActPluginV1 MyPluginInstance)
    {
        for (int i = 0; i < ActGlobals.oFormActMain.ActPlugins.Count; i++)
        {
            if (ActGlobals.oFormActMain.ActPlugins[i].pluginObj == MyPluginInstance)
            {
                return ActGlobals.oFormActMain.ActPlugins[i];
            }
        }
        return null;
    }
}
