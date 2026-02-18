namespace Advanced_Combat_Tracker;

public class PluginForm : Form {
    // 声明指定名称的控件（可外部访问，按需设置访问修饰符）
    public TabPage TabPage;
    public Label Label;
    private TabControl pluginTabControl;

    public PluginForm(string formTitle, TabPage pluginScreenSpace, Label pluginStatusText) {
        Text = formTitle;
        Size = new Size(800, 600);
        StartPosition = FormStartPosition.CenterScreen;
        pluginTabControl = new TabControl();
        pluginTabControl.Dock = DockStyle.Fill;
        TabPage = pluginScreenSpace;
        Label = pluginStatusText;
        TabPage.Controls.Add(Label);
        pluginTabControl.TabPages.Add(TabPage);
        Controls.Add(pluginTabControl);
    }
}

public class ActPluginData {
    public Panel pPluginInfo = new();
    public readonly ActPluginForm? PluginForm;
    public readonly TabPage tpPluginSpace = new();

    public FileInfo pluginFile;

    public Label lblPluginTitle = new();

    public readonly Label lblPluginStatus = new();

    public Button btnXButton = new();

    public CheckBox cbEnabled = new() {
        Checked = true
    };

    public string pluginVersion;

    public IActPluginV1 pluginObj;
    public bool isIScriptBase;
    public string pluginFileName;

    public ActPluginData(string pluginFile, IActPluginV1 pluginObj, bool isIScriptBase, bool useForm = true) {
        this.pluginFile = new FileInfo(Path.Combine(ActGlobals.oFormActMain.DalamudPlugin.PluginActScriptDirectory, pluginFile));
        this.pluginObj = pluginObj;
        this.isIScriptBase = isIScriptBase;
        pluginFileName = pluginFile;
        if (!useForm) return;
        PluginForm = new ActPluginForm();
        tpPluginSpace = PluginForm.tpPluginSpace;
        lblPluginStatus = PluginForm.lblPluginStatus;
    }

    public override bool Equals(object? other) => other != null && pluginFileName == ((ActPluginData)other).pluginFileName;
}