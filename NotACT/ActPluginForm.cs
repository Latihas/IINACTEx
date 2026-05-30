using System.Drawing;
using System.Windows.Forms;

namespace Advanced_Combat_Tracker;

public class ActPluginForm : Form {
	public readonly Label lblPluginStatus = new();
	public readonly TabPage tpPluginSpace = new();
	public bool canClose;

	public ActPluginForm() {
		InitializeComponent();
		Text = "插件面板";
		Size = new Size(1200, 900);
		StartPosition = FormStartPosition.CenterScreen;
		lblPluginStatus.Dock = DockStyle.Top;
		lblPluginStatus.Height = 150;
		lblPluginStatus.Padding = new Padding(10, 0, 0, 0);
		lblPluginStatus.BackColor = Color.LightGray;
		var tabControl1 = new TabControl {
			Dock = DockStyle.Fill,
			AllowDrop = false,
			Multiline = false
		};
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
		ClientSize = new Size(1200, 900);
		Name = "ActPluginForm";
		ResumeLayout(false);
	}
}