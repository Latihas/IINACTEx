namespace Advanced_Combat_Tracker;

public class ActPluginForm : Form {
    public Label lblPluginStatus;
    public TabPage tpPluginSpace;
    public bool canClose;

    public ActPluginForm() {
        InitializeComponent();
        Text = "插件面板";
        Size = new Size(800, 600);
        StartPosition = FormStartPosition.CenterScreen;

        lblPluginStatus = new Label();
        lblPluginStatus.Dock = DockStyle.Top;
        lblPluginStatus.Height = 150;
        lblPluginStatus.Padding = new Padding(10, 0, 0, 0);
        lblPluginStatus.BackColor = Color.LightGray;

        var tabControl1 = new TabControl();
        tabControl1.Dock = DockStyle.Fill;
        tabControl1.AllowDrop = false;
        tabControl1.Multiline = false;

        tpPluginSpace = new TabPage();
        tpPluginSpace.Text = "插件界面";
        tpPluginSpace.UseVisualStyleBackColor = true;

        tabControl1.TabPages.Add(tpPluginSpace);
        FormClosing += (_, e) => {
            if (canClose) return;
            e.Cancel = true;
            MessageBox.Show("窗体无法手动关闭，卸载插件自动关闭");
        };
        Controls.Add(tabControl1);
        Controls.Add(lblPluginStatus);
    }

    private void InitializeComponent() {
        SuspendLayout();
        ClientSize = new Size(800, 600);
        Name = "ActPluginForm";
        ResumeLayout(false);
    }
}