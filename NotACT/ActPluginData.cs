namespace Advanced_Combat_Tracker;

public class PluginForm : Form
{
    // 声明指定名称的控件（可外部访问，按需设置访问修饰符）
    public TabPage TabPage;
    public Label Label;
    private TabControl pluginTabControl;

    public PluginForm(string formTitle, TabPage pluginScreenSpace, Label pluginStatusText)
    {
        this.Text = formTitle;
        this.Size = new Size(800, 600);
        this.StartPosition = FormStartPosition.CenterScreen;
        pluginTabControl = new TabControl();
        pluginTabControl.Dock = DockStyle.Fill;
        TabPage = pluginScreenSpace;
        Label = pluginStatusText;
        TabPage.Controls.Add(Label);
        pluginTabControl.TabPages.Add(TabPage);
        this.Controls.Add(pluginTabControl);
    }
}

public class ActPluginData(string pluginFile, IActPluginV1 pluginObj, bool isIScriptBase)
{
    public Panel pPluginInfo = new();

    public TabPage tpPluginSpace = new();

    public FileInfo pluginFile = new FileInfo(Path.Combine(ActGlobals.oFormActMain.DalamudPlugin.PluginActScriptDirectory, pluginFile)) ;

    public Label lblPluginTitle = new();

    public Label lblPluginStatus = new();

    public Button btnXButton = new();

    public CheckBox cbEnabled = new CheckBox { Checked = true };

    public string pluginVersion;

    public IActPluginV1 pluginObj = pluginObj;
    public bool isIScriptBase = isIScriptBase;
    public string pluginFileName = pluginFile;

    public override bool Equals(object other)
    {
        return pluginFileName == ((ActPluginData)other).pluginFileName;
    }
}
