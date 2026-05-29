using System.Windows.Forms;

namespace Advanced_Combat_Tracker;

public interface IActPluginV1 {
	void InitPlugin(TabPage pluginScreenSpace, Label pluginStatusText);

	void DeInitPlugin();
}