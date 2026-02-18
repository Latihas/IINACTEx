using System.Windows.Forms;
using Advanced_Combat_Tracker;

namespace SilverDasher.Loader;

public class Loader : IActPluginV1 {
    private ACT.SilverDasher RealPlugin;

    public void InitPlugin(TabPage pluginScreenSpace, Label pluginStatusText) {
        RealPlugin = new ACT.SilverDasher();
        RealPlugin.InitPlugin(pluginScreenSpace, pluginStatusText);
    }

    public void DeInitPlugin() {
        RealPlugin.DeInitPlugin();
    }
}