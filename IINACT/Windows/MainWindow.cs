using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Numerics;
using System.Reflection;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Components;
using Dalamud.Interface.ImGuiNotification;
using Dalamud.Interface.Textures.TextureWraps;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using FFXIV_ACT_Plugin.Config;
using IINACT.Latihas;
using IINACT.Latihas.Overlay;
using RainbowMage.OverlayPlugin;
using RainbowMage.OverlayPlugin.WebSocket;
using static IINACT.Latihas.LWindow;
using static IINACT.Plugin;

namespace IINACT.Windows;

public class MainWindow : Window {
	private int selectedOverlayIndex;

	public MainWindow() : base(WindowPrefix) {
		SizeConstraints = new WindowSizeConstraints {
			MinimumSize = new Vector2(307, 207),
			MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
		};
	}

	public IPluginConfig? OverlayPluginConfig { get; set; }
	public IReadOnlyList<IOverlayPreset>? OverlayPresets { get; set; }
	private string[]? OverlayNames => OverlayPresets?.Select(x => x.Name).ToArray();
	public ServerController? Server { get; set; }
	private static string currentPage = "帮助";
	private static readonly string[] PagesPlugin = ["解析", "悬浮窗", "触发器", "鲇鱼精", "银山雀儿"];
	private static readonly string[] PagesIINACTEx = ["脚本", "调试"];
	private static readonly string[] PagesOther = ["初始化", "帮助", "设置"];
	private IDalamudTextureWrap? logoTexture;

	private static Bitmap GetEmbeddedPngAsBitmap(string resourceFullName) {
		using var resourceStream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceFullName);
		return new Bitmap(resourceStream);
	}


	public override void Draw() {
		ImGui.PushStyleColor(ImGuiCol.Border, new Vector4(1, 1, 1, .5f));
		ImGui.PushStyleColor(ImGuiCol.Separator, new Vector4(1, 1, 1, .5f));
		ImGui.PushStyleVar(ImGuiStyleVar.ChildBorderSize, 1);
		ImGui.PushStyleVar(ImGuiStyleVar.FrameBorderSize, 1);
		ImGui.PushStyleVar(ImGuiStyleVar.TabRounding, 8);
		if (ImGui.BeginChild("##IINACTEx_LeftTabPanel", new Vector2(200, -1), true)) {
			if (logoTexture == null) {
				try {
					using var pngBitmap = GetEmbeddedPngAsBitmap("IINACT.logo.png");
					logoTexture = OverlayWindow.Bitmap2Texture(pngBitmap);
				}
				catch (Exception ex) {
					Log.Error(ex.ToString());
				}
			}
			else {
				// ImGui.Spacing();
				// ImGui.SetCursorPosX(50);
				// var pos = ImGui.GetCursorScreenPos();
				// var iconSize = new Vector2(100, 100);
				// ImGui.Image(logoTexture.Handle, iconSize);
				// ImGui.GetWindowDrawList().AddRectFilledMultiColor(
				// 	pos, // 矩形左上角
				// 	pos + iconSize, // 矩形右下角
				// 	0x7F92FE9D, // 左上角颜色（高光起点）
				// 	0x7FFF006E, // 右上角颜色
				// 	0x7F92FE9D, // 右下角颜色（高光终点）
				// 	0x7FFF006E // 左下角颜色
				// );
				// ImGui.Spacing();


				var time = (float)ImGui.GetTime();

				ImGui.Spacing();
				ImGui.SetCursorPosX(50);
				var pos = ImGui.GetCursorScreenPos();
				var iconSize = new Vector2(100, 100);
				ImGui.Image(logoTexture.Handle, iconSize);
				var hueOffset = MathF.Sin(time * 2) * 0.5f + 0.5f;
				var alphaBreath = MathF.Cos(time * 1.5f) * 0.2f + 0.8f;
				var colorTL = (uint)(alphaBreath * 0x7F) << 24 |
				              (uint)(0x92 + hueOffset * 0x30) << 16 |
				              (uint)(0xFE) << 8 |
				              (uint)(0x9D - hueOffset * 0x20);
				var colorTR = (uint)(alphaBreath * 0x7F) << 24 |
				              (uint)(0xFF - hueOffset * 0x30) << 16 |
				              (uint)(0x00) << 8 |
				              (uint)(0x6E + hueOffset * 0x20);
				ImGui.GetWindowDrawList().AddRectFilledMultiColor(
					pos, pos + iconSize,
					colorTL,
					colorTR,
					colorTL,
					colorTR
				);
				ImGui.Spacing();
			}
			foreach (var p in PagesPlugin) {
				ImGui.SetCursorPosX(20);
				var ncp = currentPage != p;
				if (ncp) ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0, 0, 0, 0));
				if (ImGui.Button(p, new Vector2(160, 0)))
					currentPage = p;
				if (ncp) ImGui.PopStyleColor();
			}
			ImGui.Spacing();
			ImGui.Separator();
			ImGui.Spacing();
			foreach (var p in PagesIINACTEx) {
				ImGui.SetCursorPosX(20);
				var ncp = currentPage != p;
				if (ncp) ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0, 0, 0, 0));
				if (ImGui.Button(p, new Vector2(160, 0)))
					currentPage = p;
				if (ncp) ImGui.PopStyleColor();
			}
			ImGui.Spacing();
			ImGui.Separator();
			ImGui.Spacing();
			foreach (var p in PagesOther) {
				ImGui.SetCursorPosX(20);
				var ncp = currentPage != p;
				if (ncp) ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0, 0, 0, 0));
				if (ImGui.Button(p, new Vector2(160, 0)))
					currentPage = p;
				if (ncp) ImGui.PopStyleColor();
			}
			ImGui.EndChild();
		}
		ImGui.SameLine();
		if (ImGui.BeginChild("##IINACTEx_RightTabPanel", new Vector2(-1, 0), true)) {
			// using var bar = ImRaii.TabBar("settingsTabs");
			// if (!bar) return;
			switch (currentPage) {
				case "解析":
					DrawParseSettings();
					break;
				case "悬浮窗":
					DrawMainWindow();
					break;
				case "触发器":
					DrawTriggerSettings();
					break;
				case "鲇鱼精":
					DrawSettingsPostnamazu();
					break;
				case "银山雀儿":
					DrawSilverDasher();
					break;

				case "脚本":
					DrawSettingsScripts();
					break;
				case "调试":
					DrawTriggerDebug();
					break;

				case "初始化":
					DrawInitSettings();
					break;
				case "帮助":
					DrawHelpSettings();
					break;
				case "设置":
					DrawSettings();
					break;
			}
			ImGui.EndChild();
		}
		ImGui.PopStyleColor(2);
		ImGui.PopStyleVar(3);
	}

	private static void DrawSettingsPostnamazu() {
		// using var tab = ImRaii.TabItem("Postnamazu");
		// if (!tab) return;
		var PluginUi = Instance.PostNamazuPlugin.PluginUi;
		var TextPort = PluginUi.TextPort.Text;
		if (ImGui.InputText("鲇鱼精端口", ref TextPort)) {
			PluginUi.TextPort.Text = TextPort;
			PluginUi.SaveSettings();
		}
		ImGui.SameLine();
		if (PluginUi.ButtonStart.Enabled)
			if (ImGui.Button("鲇鱼精启动监听"))
				Instance.PostNamazuPlugin.ServerStart();
		if (PluginUi.ButtonStop.Enabled)
			if (ImGui.Button("鲇鱼精停止监听"))
				Instance.PostNamazuPlugin.ServerStop();
		var PostNamazuAutoStart = PluginUi.CheckAutoStart.Checked;
		if (ImGui.Checkbox("鲇鱼精监听自动启动", ref PostNamazuAutoStart)) {
			PluginUi.CheckAutoStart.Checked = PostNamazuAutoStart;
			PluginUi.SaveSettings();
		}
		ImGui.Text("启用功能");
		ImGui.SameLine();
		var iter = 1;
		foreach (var c in PluginUi.flowLayoutActions.Controls.OfType<CheckBox>()) {
			var cChecked = c.Checked;
			if (ImGui.Checkbox(c.Text, ref cChecked)) {
				PluginUi.ActionEnabled[c.Text] = c.Checked = cChecked;
				PluginUi.SaveSettings();
			}
			if (iter++ != PluginUi.flowLayoutActions.Controls.Count) ImGui.SameLine();
		}
		ImGui.Separator();
		if (ImGui.Button("清空日志")) PluginUi.lstMessages.Items.Clear();
		var items = PluginUi.lstMessages.Items;
		for (var i = 0; i < items.Count; i++) {
			var item = items[i];
			if (ImGui.Selectable($"{item}## copyItem_{i}")) {
				ImGui.SetClipboardText(item.ToString());
				NotificationManager.AddNotification(new Notification {
					Content = "已复制"
				});
			}
		}
	}

	private static void CopyDirectoryContents(string sourceDir, string targetDir, bool overwrite) {
		foreach (var filePath in Directory.GetFiles(sourceDir)) {
			var fileName = Path.GetFileName(filePath);
			var targetFilePath = Path.Combine(targetDir, fileName);
			File.Copy(filePath, targetFilePath, overwrite);
		}
		foreach (var subDirPath in Directory.GetDirectories(sourceDir)) {
			var subDirName = Path.GetFileName(subDirPath);
			var targetSubDirPath = Path.Combine(targetDir, subDirName);
			Directory.CreateDirectory(targetSubDirPath);
			CopyDirectoryContents(subDirPath, targetSubDirPath, overwrite);
		}
	}

	internal static void EnableSilverDasher() {
		var targetDir = Path.Combine(Instance.PluginActScriptDirectory, "data");
		if (!Directory.Exists(targetDir)) {
			var sourceDir = Path.Combine(Instance.PluginAssemblyDirectory, "data");
			Directory.CreateDirectory(targetDir);
			CopyDirectoryContents(sourceDir, targetDir, overwrite: true);
		}
		SilverDasherPlugin = new SilverDasher.ACT.SilverDasher(Instance.PluginActScriptDirectory,
			ClientState, ObjectTable, Framework, NotificationManager);
	}

	// private static void DrawWinForm() {
	//     using var tab = ImRaii.TabItem("怀旧组件");
	//     if (!tab) return;
	// }

	internal static SilverDasher.ACT.SilverDasher? SilverDasherPlugin;

	private static void DrawSilverDasher() {
		// using var tab = ImRaii.TabItem("SilverDasher");
		// if (!tab) return;
		if (SilverDasherPlugin == null) {
			if (ImGui.Button("启用")) EnableSilverDasher();
		}
		else {
			if (ImGui.Button("禁用")) {
				SilverDasherPlugin.DeInitPlugin();
				SilverDasherPlugin = null;
			}
		}
		ImGui.SameLine();
		ImGui.Text("银山雀儿");
		ImGui.SameLine();
		var LoadSilverDasherOnInit = Plugin.Configuration.LoadSilverDasherOnInit;
		if (ImGui.Checkbox("启动时加载", ref LoadSilverDasherOnInit)) {
			Plugin.Configuration.LoadSilverDasherOnInit = LoadSilverDasherOnInit;
			Plugin.Configuration.Save();
		}
		ImGui.Separator();
		if (SilverDasherPlugin != null) {
			SilverDasher.ACT.SilverDasher.Instance.Painter.DrawImGui();
		}
	}

	internal void DrawOverlayLink() {
		ImGui.Text("以下是开发者喜欢用的网址，点击复制，贴进bw即可:");
		foreach (var url in new[] {
			         ("伤害统计", $"http://overlay.diemoe.net/kagerou/overlay/?HOST_PORT=ws://{Server?.Address}:{Server?.Port}"),
			         ("时间轴", $"file:///{Instance.cactbotDir}/ui/raidboss/raidboss.html?timeline=1&alerts=1&OVERLAY_WS=ws://{Server?.Address}:{Server?.Port}/ws".Replace('\\', '/')),
			         ("设置", $"http://cactbot.diemoe.net/ui/config/config.html?OVERLAY_WS=ws://{Server?.Address}:{Server?.Port}/ws")
		         }) {
			if (ImGui.Button(url.Item1)) ImGui.SetClipboardText(url.Item1);
			ImGui.SameLine();
			ImGui.Text(":");
			ImGui.SameLine();
			if (ImGui.Button(url.Item2)) ImGui.SetClipboardText(url.Item2);
		}
	}

	internal void DrawOverlayGen() {
		ImGui.TextColored(ImGuiColors.DalamudGrey, "Overlay URI 生成器:");

		var comboWidth = ImGui.GetWindowWidth() * 0.8f;

		var selectedIndexOverlayName = OverlayNames?[selectedOverlayIndex] ?? "";
		var selectedOverlayName = Plugin.Configuration.SelectedOverlay ?? selectedIndexOverlayName;
		if (selectedOverlayName != selectedIndexOverlayName)
			for (var i = 0; i < OverlayNames?.Length; i++)
				if (OverlayNames?[i] == selectedOverlayName)
					selectedOverlayIndex = i;

		ImGui.SetNextItemWidth(comboWidth);
		if (ImGui.BeginCombo("悬浮窗", selectedOverlayName)) {
			for (var i = 0; i < OverlayNames?.Length; i++) {
				var currentOverlayName = OverlayNames?[i] ?? "";
				if (ImGui.Selectable(currentOverlayName, currentOverlayName == selectedOverlayName)) {
					selectedOverlayIndex = i;
					Plugin.Configuration.SelectedOverlay = currentOverlayName;
					Plugin.Configuration.Save();
				}
			}

			ImGui.EndCombo();
		}

		var selectedOverlay = OverlayPresets?[selectedOverlayIndex];

		Uri? webSocketServer = null;
		if (Server?.Address is not null && Server.Port is not null) {
			Uri.TryCreate($"ws://{Server.Address}:{Server.Port}/ws", UriKind.Absolute, out webSocketServer);
		}
		else if (OverlayPluginConfig is not null &&
		         !string.IsNullOrEmpty(OverlayPluginConfig.WSServerIP) &&
		         OverlayPluginConfig.WSServerPort > 0) {
			Uri.TryCreate($"ws://{OverlayPluginConfig.WSServerIP}:{OverlayPluginConfig.WSServerPort}/ws",
				UriKind.Absolute,
				out webSocketServer);
		}
		var overlayUri = selectedOverlay?.ToOverlayUri(webSocketServer);
		var overlayUriString = overlayUri?.ToString() ?? "<生成URI失败>";
		ImGui.SetNextItemWidth(comboWidth);
		ImGui.InputText("URI", ref overlayUriString, 1000, ImGuiInputTextFlags.ReadOnly);
		ImGui.Spacing();
	}

	private void DrawMainWindow() {
		ImGui.TextColored(ImGuiColors.DalamudGrey, "OverlayPlugin 状态:");
		ImGuiHelpers.ScaledRelativeSameLine(155);
		ImGui.Text(Instance.OverlayPluginStatus);
		ImGui.Separator();
		ImGui.Text("cactbot在线资源与悬浮窗链接可在初始化栏配置");
		ImGui.Text("IINACTEx也有内置一个原生的伤害统计悬浮窗，" + OverlayCommandName);
		ImGui.SameLine();
		if (ImGui.Button("点击打开")) Instance.OverlayWindow.IsOpen = true;
		ImGui.Separator();
		ImGui.Spacing();
		var serverStatus = Server is null ? "初始化中..." : "已停止";

		if (Server?.Running ?? false)
			serverStatus = $"监听 {Server?.Address}:{Server?.Port}";

		if (Server?.Failed ?? false) {
			serverStatus = Server.LastException?.Message ?? "失败";
			if (Server.LastException is SocketException { ErrorCode: 10048 })
				serverStatus = $"端口 {Server?.Port} 已被占用";
		}

		ImGui.TextColored(ImGuiColors.DalamudGrey, $"WebSocket 服务:");
		ImGuiHelpers.ScaledRelativeSameLine(155);
		ImGui.Text(serverStatus);
		ImGui.GetWindowDpiScale();

		if (Server?.Running ?? false) {
			if (ImGui.Button("停止")) {
				Server.Stop();
				if (OverlayPluginConfig is not null) {
					OverlayPluginConfig.WSServerRunning = false;
					OverlayPluginConfig.Save();
				}
			}

			ImGui.SameLine();

			if (ImGui.Button("重启")) {
				Server.Restart();
				Instance.OverlayWindow.Init(Server);
				if (OverlayPluginConfig is not null) {
					OverlayPluginConfig.WSServerRunning = true;
					OverlayPluginConfig.Save();
				}
			}
		}
		else if (Server is not null) {
			if (ImGui.Button("启动")) {
				Server.Start();
				if (OverlayPluginConfig is not null) {
					OverlayPluginConfig.WSServerRunning = true;
					OverlayPluginConfig.Save();
				}
			}
		}
		DrawWebSocketSettings();
	}


	private void DrawParseSettings() {
		// using var tab = ImRaii.TabItem("解析设置");
		// if (!tab) return;

		ImGui.Spacing();
		var elementWidth = ImGui.GetWindowWidth() - 150 * ImGuiHelpers.GlobalScale;
		var logFilePath = Plugin.Configuration.LogFilePath;
		ImGui.SetNextItemWidth(elementWidth);
		ImGui.InputText("日志文件路径", ref logFilePath, 200, ImGuiInputTextFlags.ReadOnly);
		ImGui.SameLine();
		if (ImGuiComponents.DisabledButton(FontAwesomeIcon.Folder)) {
			FileDialogManager.OpenFolderDialog("选择保存日志的文件夹", (success, path) => {
				if (!success) return;
				Plugin.Configuration.LogFilePath = path;
				Plugin.Configuration.Save();
			}, Plugin.Configuration.LogFilePath);
		}
		ImGui.Spacing();
		ImGui.SetNextItemWidth(elementWidth);
		if (ImGui.BeginCombo("解析过滤器",
			    GetParseFilterModeText((ParseFilterMode)Plugin.Configuration.ParseFilterMode))) {
			foreach (var filter in Enum.GetValues<ParseFilterMode>())
				if (ImGui.Selectable(GetParseFilterModeText(filter),
					    (ParseFilterMode)Plugin.Configuration.ParseFilterMode == filter)) {
					Plugin.Configuration.ParseFilterMode = (int)filter;
					Plugin.Configuration.Save();
				}

			ImGui.EndCombo();
		}

		ImGui.Spacing();
		var WriteLogFile = Plugin.Configuration.WriteLogFile;
		if (ImGui.Checkbox("写入网络日志文件", ref WriteLogFile)) {
			Plugin.Configuration.WriteLogFile = WriteLogFile;
			Plugin.Configuration.Save();
		}
		var WriteActLogFile = Plugin.Configuration.WriteActLogFile;
		if (Plugin.Configuration.WriteLogFile && ImGui.Checkbox("写入ACT日志文件(.actxt)", ref WriteActLogFile)) {
			Plugin.Configuration.WriteActLogFile = WriteActLogFile;
			Plugin.Configuration.Save();
		}
		var WriteTrnLogFile = Plugin.Configuration.WriteTrnLogFile;
		if (Plugin.Configuration.WriteLogFile && ImGui.Checkbox("写入Trn日志文件(.trnxt)", ref WriteTrnLogFile)) {
			Plugin.Configuration.WriteTrnLogFile = WriteTrnLogFile;
			Plugin.Configuration.Save();
		}
		var disablePvp = Plugin.Configuration.DisablePvp;
		if (ImGui.Checkbox("在PvP中禁用写入网络日志文件", ref disablePvp)) {
			if (ClientState.IsPvP && disablePvp) Plugin.Configuration.DisableWritingPvpLogFile = true;

			Plugin.Configuration.DisablePvp = disablePvp;
			Plugin.Configuration.Save();
		}

		var disableDamageShield = Plugin.Configuration.DisableDamageShield;
		if (ImGui.Checkbox("禁用伤害盾估计", ref disableDamageShield)) {
			Plugin.Configuration.DisableDamageShield = disableDamageShield;
			Plugin.Configuration.Save();
		}

		var disableCombinePets = Plugin.Configuration.DisableCombinePets;
		if (ImGui.Checkbox("禁用宠物合并", ref disableCombinePets)) {
			Plugin.Configuration.DisableCombinePets = disableCombinePets;
			Plugin.Configuration.Save();
		}

		var showDebug = Plugin.Configuration.ShowDebug;
		if (ImGui.Checkbox("显示调试选项", ref showDebug)) {
			Plugin.Configuration.ShowDebug = showDebug;
			Plugin.Configuration.Save();
		}

		if (!showDebug) return;

		ImGui.Spacing();
		ImGui.Separator();
		ImGui.Spacing();

		var simulateIndividualDoTCrits = Plugin.Configuration.SimulateIndividualDoTCrits;
		if (ImGui.Checkbox("模拟单体 DoT 暴击", ref simulateIndividualDoTCrits)) {
			Plugin.Configuration.SimulateIndividualDoTCrits = simulateIndividualDoTCrits;
			Plugin.Configuration.Save();
		}

		var showRealDoTTicks = Plugin.Configuration.ShowRealDoTTicks;
		if (ImGui.Checkbox("显示真实 DoT Ticks", ref showRealDoTTicks)) {
			Plugin.Configuration.ShowRealDoTTicks = showRealDoTTicks;
			Plugin.Configuration.Save();
		}
	}

	private void DrawWebSocketSettings() {
		// using var tab = ImRaii.TabItem("WebSocket 服务");
		// if (!tab) return;

		ImGui.Spacing();
		var wsServerIp = OverlayPluginConfig?.WSServerIP ?? "";
		ImGui.InputText("IP地址", ref wsServerIp, 100);

		if (IPAddress.TryParse(wsServerIp, out var address)) {
			if (OverlayPluginConfig is not null)
				OverlayPluginConfig.WSServerIP = address.ToString();
		}
		else if (wsServerIp == "*") {
			if (OverlayPluginConfig is not null)
				OverlayPluginConfig.WSServerIP = "*";
		}

		var wsServerPort = OverlayPluginConfig?.WSServerPort.ToString() ?? "";
		ImGui.InputText("端口", ref wsServerPort, 100);

		if (int.TryParse(wsServerPort, out var port)) {
			if (OverlayPluginConfig is not null)
				OverlayPluginConfig.WSServerPort = port;
		}

		OverlayPluginConfig?.Save();
	}


	private string GetParseFilterModeText(ParseFilterMode mode) {
		return mode switch {
			ParseFilterMode.None => "无 (None)",
			ParseFilterMode.Self => "仅自己 (Self)",
			ParseFilterMode.Party => "仅小队成员 (Party)",
			ParseFilterMode.Alliance => "仅团队成员 (Alliance)",
			_ => mode.ToString()
		};
	}
}