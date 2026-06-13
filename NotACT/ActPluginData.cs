using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Windows.Forms;

namespace Advanced_Combat_Tracker;

[SuppressMessage("ReSharper", "UnusedMember.Global")]
[SuppressMessage("ReSharper", "FieldCanBeMadeReadOnly.Global")]
public class ActPluginData {
	public readonly Label lblPluginStatus = new();
	public readonly ActPluginForm? PluginForm;
	public readonly TabPage tpPluginSpace = new();
	public Button btnXButton = new();
	public CheckBox cbEnabled = new() {
		Checked = true
	};
	public string? IScriptBaseDesc;
	public bool isIScriptBase;
	public Label lblPluginTitle = new();
	public FileInfo pluginFile;
	public string pluginFileName;
	public IActPluginV1 pluginObj;
	public string pluginVersion;
	public Panel pPluginInfo = new();

	public ActPluginData(string pluginFile, IActPluginV1 pluginObj, bool isIScriptBase, bool useForm = true) {
		this.pluginFile = new FileInfo(Path.Combine(ActGlobals.oFormActMain.DalamudPlugin.PluginActScriptDirectory, pluginFile));
		this.pluginObj = pluginObj;
		this.isIScriptBase = isIScriptBase;
		pluginFileName = pluginFile;
		if (!useForm || isIScriptBase) return;
		PluginForm = new ActPluginForm();
		tpPluginSpace = PluginForm.tpPluginSpace;
		lblPluginStatus = PluginForm.lblPluginStatus;
	}

	public override bool Equals(object? other) => other != null && pluginFileName == ((ActPluginData)other).pluginFileName;
}