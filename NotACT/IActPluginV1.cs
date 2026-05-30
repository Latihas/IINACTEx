using System.Diagnostics.CodeAnalysis;
using System.Windows.Forms;

namespace Advanced_Combat_Tracker;

public interface IActPluginV1 {
	[SuppressMessage("ReSharper", "UnusedParameter.Global")]
	void InitPlugin(TabPage pluginScreenSpace, Label pluginStatusText);

	void DeInitPlugin();
}