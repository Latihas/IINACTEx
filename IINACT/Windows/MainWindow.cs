using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Numerics;
using System.Reflection;
using System.Text;
using System.Windows.Forms;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Components;
using Dalamud.Interface.ImGuiNotification;
using Dalamud.Interface.Textures.TextureWraps;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Windowing;
using FetchDependencies;
using FFXIV_ACT_Plugin.Config;
using IINACT.Latihas.Overlay;
using RainbowMage.OverlayPlugin;
using RainbowMage.OverlayPlugin.WebSocket;
using static IINACT.Latihas.LWindow;
using static IINACT.Plugin;

namespace IINACT.Windows;

public class MainWindow() : Window(WindowPrefix) {
	private int selectedOverlayIndex;

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
		using var resourceStream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceFullName)!;
		return new Bitmap(resourceStream);
	}

	private readonly Dictionary<ImGuiCol, Vector4> ImGuiColor = new() {
		[ImGuiCol.Border] = new Vector4(1, 1, 1, .5f),
		[ImGuiCol.Separator] = new Vector4(1, 1, 1, .5f),
		[ImGuiCol.ChildBg] = new Vector4(36 / 255f, 40 / 255f, 47 / 255f, 1),
		[ImGuiCol.FrameBg] = new Vector4(37 / 255f, 39 / 255f, 44 / 255f, 1),
		[ImGuiCol.Button] = new Vector4(62 / 255f, 78 / 255f, 105 / 255f, 1),
		[ImGuiCol.ButtonHovered] = new Vector4(50 / 255f, 58 / 255f, 75 / 255f, 1),
		[ImGuiCol.ButtonActive] = new Vector4(88 / 255f, 111 / 255f, 150 / 255f, 1),
		[ImGuiCol.CheckMark] = new Vector4(26 / 255f, 159 / 255f, 1, 1),
		[ImGuiCol.ScrollbarBg] = new Vector4(0, 0, 0, 0),
		[ImGuiCol.ScrollbarGrab] = new Vector4(96 / 255f, 103 / 255f, 116 / 255f, 1),
		[ImGuiCol.ScrollbarGrabHovered] = new Vector4(123 / 255f, 131 / 255f, 146 / 255f, 1),
		[ImGuiCol.ScrollbarGrabActive] = new Vector4(123 / 255f, 131 / 255f, 146 / 255f, 1),
		[ImGuiCol.Header] = new Vector4(62 / 255f, 78 / 255f, 105 / 255f, 1),
		[ImGuiCol.HeaderHovered] = new Vector4(50 / 255f, 58 / 255f, 75 / 255f, 1),
		[ImGuiCol.HeaderActive] = new Vector4(88 / 255f, 111 / 255f, 150 / 255f, 1),
		[ImGuiCol.PlotHistogram] = new Vector4(20 / 255f, 90 / 255f, 141 / 255f, 1)
	};
	private readonly Dictionary<ImGuiStyleVar, int> ImGuiVar = new() {
		[ImGuiStyleVar.ChildBorderSize] = 4,
		[ImGuiStyleVar.FrameBorderSize] = 2
	};

	private static (float r, float g, float b) HsvToRgb(float hue, float saturation, float value) {
		var h = hue / 60f;
		var i = (int)Math.Floor(h);
		var f = h - i;
		var p = value * (1 - saturation);
		var q = value * (1 - saturation * f);
		var t = value * (1 - saturation * (1 - f));

		float r, g, b;
		switch (i) {
			case 0:
				r = value;
				g = t;
				b = p;
				break;
			case 1:
				r = q;
				g = value;
				b = p;
				break;
			case 2:
				r = p;
				g = value;
				b = t;
				break;
			case 3:
				r = p;
				g = q;
				b = value;
				break;
			case 4:
				r = t;
				g = p;
				b = value;
				break;
			case 5:
				r = value;
				g = p;
				b = q;
				break;
			default:
				r = value;
				g = value;
				b = value;
				break;
		}
		return (r, g, b);
	}

	private string? _windowName;
	private string? FFXIV_ACT_PluginVersion {
		get {
			field ??= Instance.fetchDependencies.GetFFXIV_ACT_PluginVersion();
			return field;
		}
	}

	public override void Draw() {
		WindowName = _windowName ??=
			$"IINACTEx [版本{Assembly.GetExecutingAssembly().GetName().Version?.ToString()}] [{ApiVersion.NamespaceIdentifier}] [核心{Instance.Version}] [解析{FFXIV_ACT_PluginVersion}{(Instance.Configuration.FFXIV_ACT_Plugin_CN_Update ? "(解析插件可更新，请重新加载插件以更新)" : "")}]###IINACTEx";
		foreach (var p in ImGuiColor) ImGui.PushStyleColor(p.Key, p.Value);
		foreach (var p in ImGuiVar) ImGui.PushStyleVar(p.Key, p.Value);
		var time = (float)ImGui.GetTime();
		var hsv = HsvToRgb(time * 90 % 360, .1f, .9f);
		ImGui.PushClipRect(ImGui.GetWindowPos(), ImGui.GetWindowPos() + ImGui.GetWindowSize(), false);
		ImGui.PushStyleColor(ImGuiCol.Border, new Vector4(hsv.r, hsv.g, hsv.b, 0.9f));
		if (ImGui.BeginChild("##IINACTEx_LeftTabPanel", new Vector2(200, -1), true)) {
			if (logoTexture == null) {
				try {
					using var pngBitmap = GetEmbeddedPngAsBitmap("IINACT.logo.png");
					logoTexture = OverlayWindow.Bitmap2Texture(pngBitmap);
				} catch (Exception ex) {
					Log.Error(ex.ToString());
				}
			} else {
				ImGui.Spacing();
				ImGui.SetCursorPosX(50);
				var pos = ImGui.GetCursorScreenPos();
				var iconSize = new Vector2(100, 100);
				ImGui.Image(logoTexture.Handle, iconSize);
				var hueOffset = MathF.Sin(time * 3.1f) * 0.5f + 0.5f;
				var alphaBreath = MathF.Cos(time * 1.7f) * 0.4f + 0.6f;
				var tlA = (byte)(alphaBreath * 0x6F);
				var tlR = (byte)(0x92 + hueOffset * 0x30);
				const byte tlG = 0xFE;
				var tlB = (byte)(0x9D - hueOffset * 0x20);
				var trA = (byte)(alphaBreath * 0x6F);
				var trR = (byte)(0xFF - hueOffset * 0x30);
				const byte trG = 0x00;
				var trB = (byte)(0x6E + hueOffset * 0x20);
				const float rotationSpeed = 2f;
				var rotationPhase = time * rotationSpeed;
				var phaseTL = rotationPhase;
				var phaseTR = rotationPhase + MathF.PI / 2;
				var phaseBR = rotationPhase + MathF.PI;
				var phaseBL = rotationPhase + MathF.PI * 3 / 2;
				var mixTL = (MathF.Sin(phaseTL) + 1f) / 2f;
				var mixTR = (MathF.Sin(phaseTR) + 1f) / 2f;
				var mixBR = (MathF.Sin(phaseBR) + 1f) / 2f;
				var mixBL = (MathF.Sin(phaseBL) + 1f) / 2f;
				var colorTL_Final = (uint)tlA << 24 | (uint)(tlR * (1 - mixTL) + trR * mixTL) << 16 |
				                    (uint)(tlG * (1 - mixTL) + trG * mixTL) << 8 | (uint)(tlB * (1 - mixTL) + trB * mixTL);
				var colorTR_Final = (uint)trA << 24 | (uint)(tlR * (1 - mixTR) + trR * mixTR) << 16 |
				                    (uint)(tlG * (1 - mixTR) + trG * mixTR) << 8 | (uint)(tlB * (1 - mixTR) + trB * mixTR);
				var colorBR_Final = (uint)tlA << 24 | (uint)(tlR * (1 - mixBR) + trR * mixBR) << 16 |
				                    (uint)(tlG * (1 - mixBR) + trG * mixBR) << 8 | (uint)(tlB * (1 - mixBR) + trB * mixBR);
				var colorBL_Final = (uint)trA << 24 | (uint)(tlR * (1 - mixBL) + trR * mixBL) << 16 |
				                    (uint)(tlG * (1 - mixBL) + trG * mixBL) << 8 | (uint)(tlB * (1 - mixBL) + trB * mixBL);
				ImGui.GetWindowDrawList().AddRectFilledMultiColor(
					pos, pos + iconSize, colorTL_Final, colorTR_Final, colorBR_Final, colorBL_Final
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
		ImGui.PopClipRect();
		ImGui.PopStyleColor(ImGuiColor.Count + 1);
		ImGui.PopStyleVar(ImGuiVar.Count);
	}

	private static void DrawSettingsPostnamazu() {
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

	internal static void CopyDirectoryContents(string sourceDir, string targetDir, bool overwrite) {
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
			CopyDirectoryContents(sourceDir, targetDir, true);
		}
		SilverDasherPlugin = new SilverDasher.ACT.SilverDasher(Instance.PluginActScriptDirectory,
			ClientState, ObjectTable, Framework, NotificationManager, DataManager,
			Instance.ZoneDownHookManager.unscrambler._constants);
	}

	internal static bool DisableSilverDasher() {
		if (SilverDasherPlugin == null) return false;
		SilverDasherPlugin.DeInitPlugin();
		SilverDasherPlugin = null;
		return true;
	}

	internal static SilverDasher.ACT.SilverDasher? SilverDasherPlugin;

	private static void DrawSilverDasher() {
		if (SilverDasherPlugin == null) {
			if (ImGui.Button("启用")) EnableSilverDasher();
		} else {
			if (ImGui.Button("禁用")) DisableSilverDasher();
		}
		ImGui.SameLine();
		ImGui.Text("银山雀儿");
		ImGui.SameLine();
		var LoadSilverDasherOnInit = Instance.Configuration.LoadSilverDasherOnInit;
		if (ImGui.Checkbox("启动时加载", ref LoadSilverDasherOnInit)) {
			Instance.Configuration.LoadSilverDasherOnInit = LoadSilverDasherOnInit;
			Instance.Configuration.Save();
		}
		ImGui.Separator();
		if (SilverDasherPlugin != null)
			SilverDasher.ACT.SilverDasher.Instance.Painter.DrawImGui();
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
		var selectedOverlayName = Instance.Configuration.SelectedOverlay ?? selectedIndexOverlayName;
		if (selectedOverlayName != selectedIndexOverlayName)
			for (var i = 0; i < OverlayNames?.Length; i++)
				if (OverlayNames?[i] == selectedOverlayName)
					selectedOverlayIndex = i;

		ImGui.SetNextItemWidth(comboWidth);
		if (ImGui.Combo("悬浮窗##OverlayCombo", ref selectedOverlayIndex, OverlayNames)) {
			Instance.Configuration.SelectedOverlay = OverlayNames?[selectedOverlayIndex];
			Instance.Configuration.Save();
		}

		var selectedOverlay = OverlayPresets?[selectedOverlayIndex];

		Uri? webSocketServer = null;
		if (Server?.Address is not null && Server.Port is not null) {
			Uri.TryCreate($"ws://{Server.Address}:{Server.Port}/ws", UriKind.Absolute, out webSocketServer);
		} else if (OverlayPluginConfig is not null &&
		           !string.IsNullOrEmpty(OverlayPluginConfig.WSServerIP) &&
		           OverlayPluginConfig.WSServerPort > 0) {
			Uri.TryCreate($"ws://{OverlayPluginConfig.WSServerIP}:{OverlayPluginConfig.WSServerPort}/ws",
				UriKind.Absolute, out webSocketServer);
		}
		var overlayUri = selectedOverlay?.ToOverlayUri(webSocketServer!);
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
		} else if (Server is not null) {
			if (ImGui.Button("启动")) {
				Server.Start();
				if (OverlayPluginConfig is not null) {
					OverlayPluginConfig.WSServerRunning = true;
					OverlayPluginConfig.Save();
				}
			}
		}
		DrawWebSocketSettings();
		if (ImGui.Button("尝试杀死其他占用端口程序"))
			KillOccupiedProcessByPort(Server?.Port);
	}

	private static void KillOccupiedProcessByPort(int? port) {
		if (port is null or < 1 or > 65535) return;
		int currentPid;
		using (var currentProcess = Process.GetCurrentProcess()) {
			currentPid = currentProcess.Id;
		}
		var pidSet = new HashSet<int>();
		var cmd = $"netstat -ano | findstr \":{port}\"";
		using (var proc = new Process()) {
			proc.StartInfo = new ProcessStartInfo {
				FileName = "cmd.exe",
				Arguments = $"/c {cmd}",
				UseShellExecute = false,
				RedirectStandardOutput = true,
				CreateNoWindow = true,
				StandardOutputEncoding = Encoding.UTF8
			};
			proc.Start();
			var output = proc.StandardOutput.ReadToEnd();
			proc.WaitForExit();
			var lines = output.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
			foreach (var line in lines) {
				var trimLine = line.Trim();
				if (!trimLine.Contains("LISTENING")) continue;
				var parts = trimLine.Split([' '], StringSplitOptions.RemoveEmptyEntries);
				if (parts.Length == 0) continue;
				if (int.TryParse(parts.Last(), out var pid) && pid > 0 && pid != currentPid)
					pidSet.Add(pid);
			}
		}
		if (pidSet.Count == 0) return;
		foreach (var pid in pidSet) {
			try {
				using var process = Process.GetProcessById(pid);
				process.CloseMainWindow();
				if (process.HasExited) continue;
				process.Kill();
			} catch {
				//
			}
		}
	}

	private void DrawParseSettings() {
		ImGui.Spacing();
		var elementWidth = ImGui.GetWindowWidth() - 150 * ImGuiHelpers.GlobalScale;
		var logFilePath = Instance.Configuration.LogFilePath;
		ImGui.SetNextItemWidth(elementWidth);
		ImGui.InputText("日志文件路径", ref logFilePath, 200, ImGuiInputTextFlags.ReadOnly);
		ImGui.SameLine();
		if (ImGuiComponents.DisabledButton(FontAwesomeIcon.Folder)) {
			FileDialogManager.OpenFolderDialog("选择保存日志的文件夹", (success, path) => {
				if (!success) return;
				Instance.Configuration.LogFilePath = path;
				Instance.Configuration.Save();
			}, Instance.Configuration.LogFilePath);
		}
		ImGui.Spacing();
		ImGui.SetNextItemWidth(elementWidth);
		var idx = Instance.Configuration.ParseFilterMode;
		if (ImGui.Combo("解析过滤器", ref idx, Enum.GetValues<ParseFilterMode>().Select(GetParseFilterModeText).ToList())) {
			Instance.Configuration.ParseFilterMode = idx;
			Instance.Configuration.Save();
		}

		ImGui.Spacing();
		var WriteLogFile = Instance.Configuration.WriteLogFile;
		if (ImGui.Checkbox("写入网络日志文件", ref WriteLogFile)) {
			Instance.Configuration.WriteLogFile = WriteLogFile;
			Instance.Configuration.Save();
		}
		var WriteActLogFile = Instance.Configuration.WriteActLogFile;
		if (Instance.Configuration.WriteLogFile && ImGui.Checkbox("写入ACT日志文件(.actxt)", ref WriteActLogFile)) {
			Instance.Configuration.WriteActLogFile = WriteActLogFile;
			Instance.Configuration.Save();
		}
		var WriteTrnLogFile = Instance.Configuration.WriteTrnLogFile;
		if (Instance.Configuration.WriteLogFile && ImGui.Checkbox("写入Trn日志文件(.trnxt)", ref WriteTrnLogFile)) {
			Instance.Configuration.WriteTrnLogFile = WriteTrnLogFile;
			Instance.Configuration.Save();
		}
		var disablePvp = Instance.Configuration.DisablePvp;
		if (ImGui.Checkbox("在PvP中禁用写入网络日志文件", ref disablePvp)) {
			if (ClientState.IsPvP && disablePvp) Instance.Configuration.DisableWritingPvpLogFile = true;

			Instance.Configuration.DisablePvp = disablePvp;
			Instance.Configuration.Save();
		}

		var disableDamageShield = Instance.Configuration.DisableDamageShield;
		if (ImGui.Checkbox("禁用伤害盾估计", ref disableDamageShield)) {
			Instance.Configuration.DisableDamageShield = disableDamageShield;
			Instance.Configuration.Save();
		}

		var disableCombinePets = Instance.Configuration.DisableCombinePets;
		if (ImGui.Checkbox("禁用宠物合并", ref disableCombinePets)) {
			Instance.Configuration.DisableCombinePets = disableCombinePets;
			Instance.Configuration.Save();
		}

		var showDebug = Instance.Configuration.ShowDebug;
		if (ImGui.Checkbox("显示调试选项", ref showDebug)) {
			Instance.Configuration.ShowDebug = showDebug;
			Instance.Configuration.Save();
		}

		if (!showDebug) return;

		ImGui.Spacing();
		ImGui.Separator();
		ImGui.Spacing();

		var simulateIndividualDoTCrits = Instance.Configuration.SimulateIndividualDoTCrits;
		if (ImGui.Checkbox("模拟单体 DoT 暴击", ref simulateIndividualDoTCrits)) {
			Instance.Configuration.SimulateIndividualDoTCrits = simulateIndividualDoTCrits;
			Instance.Configuration.Save();
		}

		var showRealDoTTicks = Instance.Configuration.ShowRealDoTTicks;
		if (ImGui.Checkbox("显示真实 DoT Ticks", ref showRealDoTTicks)) {
			Instance.Configuration.ShowRealDoTTicks = showRealDoTTicks;
			Instance.Configuration.Save();
		}
	}

	private void DrawWebSocketSettings() {
		ImGui.Spacing();
		var wsServerIp = OverlayPluginConfig?.WSServerIP ?? "";
		ImGui.InputText("IP地址", ref wsServerIp, 100);
		if (IPAddress.TryParse(wsServerIp, out var address))
			OverlayPluginConfig?.WSServerIP = address.ToString();
		else if (wsServerIp == "*")
			OverlayPluginConfig?.WSServerIP = "*";

		var wsServerPort = OverlayPluginConfig?.WSServerPort.ToString() ?? "";
		ImGui.InputText("端口", ref wsServerPort, 100);
		if (int.TryParse(wsServerPort, out var port))
			OverlayPluginConfig?.WSServerPort = port;
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