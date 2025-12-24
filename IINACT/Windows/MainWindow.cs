using System.IO;
using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Components;
using Dalamud.Interface.Windowing;
using FFXIV_ACT_Plugin.Config;
using RainbowMage.OverlayPlugin;
using System.Net;
using System.Net.Sockets;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.ImGuiNotification;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using IINACT.Latihas;

namespace IINACT.Windows;

public class MainWindow : Window, IDisposable
{
    private int selectedOverlayIndex;

    public MainWindow() : base(LWindow.WindowPrefix)
    {
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(307, 207),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
        };
    }

    public IPluginConfig? OverlayPluginConfig { get; set; }
    public IReadOnlyList<RainbowMage.OverlayPlugin.IOverlayTemplate>? OverlayPresets { get; set; }
    private string[]? OverlayNames => OverlayPresets?.Select(x => x.Name).ToArray();
    public RainbowMage.OverlayPlugin.WebSocket.ServerController? Server { get; set; }

    public void Dispose() { }

    public override void Draw()
    {
        using var bar = ImRaii.TabBar("settingsTabs");
        if (!bar) return;
        LWindow.DrawHelpSettings();
        DrawMainWindow();
        DrawParseSettings();
        DrawTTSSettings();
        LWindow.DrawTriggerSettings();
        LWindow.DrawTestSettings();
        LWindow.DrawSettings();
    }

    private void DrawMainWindow()
    {
        using var tab = ImRaii.TabItem("运行状态");
        if (!tab) return;

        ImGui.Spacing();
        ImGui.TextColored(ImGuiColors.DalamudGrey, "OverlayPlugin 状态:");
        ImGuiHelpers.ScaledRelativeSameLine(155);
        ImGui.Text(Plugin.Instance.OverlayPluginStatus);
        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        ImGui.TextColored(ImGuiColors.DalamudGrey, "Overlay URI 生成器:");

        var comboWidth = ImGui.GetWindowWidth() * 0.8f;

        var selectedIndexOverlayName = OverlayNames?[selectedOverlayIndex] ?? "";
        var selectedOverlayName = Plugin.Configuration.SelectedOverlay ?? selectedIndexOverlayName;
        if (selectedOverlayName != selectedIndexOverlayName)
            for (var i = 0; i < OverlayNames?.Length; i++)
                if (OverlayNames?[i] == selectedOverlayName)
                    selectedOverlayIndex = i;

        ImGui.SetNextItemWidth(comboWidth);
        if (ImGui.BeginCombo("悬浮窗", selectedOverlayName))
        {
            for (var i = 0; i < OverlayNames?.Length; i++)
            {
                var currentOverlayName = OverlayNames?[i] ?? "";
                if (ImGui.Selectable(currentOverlayName, currentOverlayName == selectedOverlayName))
                {
                    selectedOverlayIndex = i;
                    Plugin.Configuration.SelectedOverlay = currentOverlayName;
                    Plugin.Configuration.Save();
                }
            }

            ImGui.EndCombo();
        }

        var selectedOverlay = OverlayPresets?[selectedOverlayIndex];
        Uri.TryCreate($"ws://{Server?.Address}:{Server?.Port}/ws", UriKind.Absolute, out var webSocketServer);
        var overlayUri = selectedOverlay?.ToOverlayUri(webSocketServer);
        var overlayUriString = overlayUri?.ToString() ?? "<生成URI失败>";

        ImGui.SetNextItemWidth(comboWidth);
        ImGui.InputText("URI", ref overlayUriString, 1000, ImGuiInputTextFlags.ReadOnly);

        ImGui.Spacing();

        ImGui.Text("在线的部分网页(如Timeline)不一定是最新的，IINACTEx尽量提供最新版Diemoe ACT内置的资源");
        ImGui.Text("cactbot.zip可在");
        ImGui.SameLine();
        const string cactboturl = "https://raw.githubusercontent.com/Latihas/dalamud-plugins/main/cactbot.zip";
        var cactbotDir = Path.Combine(Plugin.PluginConfigDirectory, "cactbot");
        if (ImGui.Button(cactboturl)) LWindow.Start(cactboturl);
        ImGui.SameLine();
        ImGui.Text("下载");
        ImGui.Text("也可以尝试");
        ImGui.SameLine();
        if (ImGui.Button("一键下载解压"))
        {
            if (FileDownloaderCactbot != null)
            {
                Plugin.NotificationManager.AddNotification(new Notification
                {
                    Type = NotificationType.Warning,
                    Content = "有未完成的下载任务"
                });
            }
            else
            {
                var zipPath = Path.Combine(Plugin.PluginConfigDirectory, "cactbot.zip");
                FileDownloaderCactbot = new FileDownloader(cactboturl, zipPath, () =>
                {
                    FileDownloaderCactbot = null;
                    Plugin.UnzipWithoutPassword(zipPath, cactbotDir);
                });
                if (File.Exists(zipPath)) File.Delete(zipPath);
                _ = FileDownloaderCactbot.DownloadFileAsync();
            }
        }
        if (FileDownloaderCactbot != null)
        {
            ImGui.SameLine();
            ImGui.ProgressBar(FileDownloaderCactbot.Progress, new Vector2(300, 24), "下载中");
        }
        ImGui.SameLine();
        ImGui.Text("会强制覆盖旧版，但是受网络影响较大，实在不行只能手动下载。");
        ImGui.Text("手动下载完成后放在IINACTEx插件的安装目录下，可以选择手动解压，也可以在插件下次加载时自动解压。解压后，即可打开资源文件夹。");

        if (ImGui.Button("打开资源文件夹")) LWindow.Start(Plugin.PluginConfigDirectory);
        ImGui.Text("更多网页可见cactbot文件夹。以下是开发者喜欢用的网址，点击复制，贴进bw即可:");
        foreach (var url in new[]
                 {
                     ("伤害统计", $"http://overlay.diemoe.net/kagerou/overlay/?HOST_PORT=ws://{Server?.Address}:{Server?.Port}"),
                     ("时间轴", $"file:///{cactbotDir}/ui/raidboss/raidboss.html?timeline=1&alerts=1&OVERLAY_WS=ws://{Server?.Address}:{Server?.Port}/ws".Replace('\\', '/')),
                     ("设置", $"http://cactbot.diemoe.net/ui/config/config.html?OVERLAY_WS=ws://{Server?.Address}:{Server?.Port}/ws")
                 })
        {
            if (ImGui.Button(url.Item1)) ImGui.SetClipboardText(url.Item1);
            ImGui.SameLine();
            ImGui.Text(":");
            ImGui.SameLine();
            if (ImGui.Button(url.Item2)) ImGui.SetClipboardText(url.Item2);
        }
        ImGui.Text("每次插件加载会自动刷新bw的上述名称(时间轴,设置)的悬浮窗。");
        ImGui.Text("IINACTEx也有内置一个原生的伤害统计悬浮窗，" + Plugin.OverlayCommandName);
        ImGui.SameLine();
        if (ImGui.Button("点击打开")) Plugin.Instance.OverlayWindow.IsOpen = true;
        ImGui.Separator();
        ImGui.Spacing();
        var serverStatus = Server is null ? "初始化中..." : "已停止";

        if (Server?.Running ?? false)
            serverStatus = $"监听 {Server?.Address}:{Server?.Port}";

        if (Server?.Failed ?? false)
        {
            serverStatus = Server.LastException?.Message ?? "失败";
            if (Server.LastException is SocketException { ErrorCode: 10048 })
                serverStatus = $"端口 {Server?.Port} 已被占用";
        }

        ImGui.TextColored(ImGuiColors.DalamudGrey, $"WebSocket 服务:");
        ImGuiHelpers.ScaledRelativeSameLine(155);
        ImGui.Text(serverStatus);
        ImGui.GetWindowDpiScale();

        if (Server?.Running ?? false)
        {
            if (ImGui.Button("停止"))
                Server.Stop();

            ImGui.SameLine();

            if (ImGui.Button("重启"))
            {
                Server.Restart();
                Plugin.Instance.OverlayWindow.Init(Server);
            }
        }
        else if (Server is not null)
        {
            if (ImGui.Button("启动"))
                Server.Start();
        }
        DrawWebSocketSettings();
    }

    private static FileDownloader? FileDownloaderCactbot;

    private void DrawParseSettings()
    {
        using var tab = ImRaii.TabItem("解析设置");
        if (!tab) return;

        ImGui.Spacing();
        var elementWidth = ImGui.GetWindowWidth() - (150 * ImGuiHelpers.GlobalScale);
        var logFilePath = Plugin.Configuration.LogFilePath;
        ImGui.SetNextItemWidth(elementWidth);
        ImGui.InputText("日志文件路径", ref logFilePath, 200, ImGuiInputTextFlags.ReadOnly);
        ImGui.SameLine();
        if (ImGuiComponents.DisabledButton(FontAwesomeIcon.Folder))
        {
            Plugin.FileDialogManager.OpenFolderDialog("选择保存日志的文件夹", (success, path) =>
            {
                if (!success) return;
                Plugin.Configuration.LogFilePath = path;
                Plugin.Configuration.Save();
            }, Plugin.Configuration.LogFilePath);
        }
        ImGui.Spacing();
        ImGui.SetNextItemWidth(elementWidth);
        if (ImGui.BeginCombo("解析过滤器",
                             GetParseFilterModeText((ParseFilterMode)Plugin.Configuration.ParseFilterMode)))
        {
            foreach (var filter in Enum.GetValues<ParseFilterMode>())
                if (ImGui.Selectable(GetParseFilterModeText(filter),
                                     (ParseFilterMode)Plugin.Configuration.ParseFilterMode == filter))
                {
                    Plugin.Configuration.ParseFilterMode = (int)filter;
                    Plugin.Configuration.Save();
                }

            ImGui.EndCombo();
        }

        ImGui.Spacing();
        var writeLogFile = Plugin.Configuration.WriteLogFile;
        var writeActLogFile = Plugin.Configuration.WriteActLogFile;
        if (ImGui.Checkbox("写入网络日志文件", ref writeLogFile))
        {
            Plugin.Configuration.WriteLogFile = writeLogFile;
            Plugin.Configuration.Save();
        }
        if (Plugin.Configuration.WriteLogFile && ImGui.Checkbox("写入ACT日志文件(.actxt)", ref writeActLogFile))
        {
            Plugin.Configuration.WriteActLogFile = writeActLogFile;
            Plugin.Configuration.Save();
        }
        var disablePvp = Plugin.Configuration.DisablePvp;
        if (ImGui.Checkbox("在PvP中禁用写入网络日志文件", ref disablePvp))
        {
            if (Plugin.ClientState.IsPvP && disablePvp) Plugin.Configuration.DisableWritingPvpLogFile = true;

            Plugin.Configuration.DisablePvp = disablePvp;
            Plugin.Configuration.Save();
        }

        var disableDamageShield = Plugin.Configuration.DisableDamageShield;
        if (ImGui.Checkbox("禁用伤害盾估计", ref disableDamageShield))
        {
            Plugin.Configuration.DisableDamageShield = disableDamageShield;
            Plugin.Configuration.Save();
        }

        var disableCombinePets = Plugin.Configuration.DisableCombinePets;
        if (ImGui.Checkbox("禁用宠物合并", ref disableCombinePets))
        {
            Plugin.Configuration.DisableCombinePets = disableCombinePets;
            Plugin.Configuration.Save();
        }

        var showDebug = Plugin.Configuration.ShowDebug;
        if (ImGui.Checkbox("显示调试选项", ref showDebug))
        {
            Plugin.Configuration.ShowDebug = showDebug;
            Plugin.Configuration.Save();
        }

        if (!showDebug) return;

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        var simulateIndividualDoTCrits = Plugin.Configuration.SimulateIndividualDoTCrits;
        if (ImGui.Checkbox("模拟单体 DoT 暴击", ref simulateIndividualDoTCrits))
        {
            Plugin.Configuration.SimulateIndividualDoTCrits = simulateIndividualDoTCrits;
            Plugin.Configuration.Save();
        }

        var showRealDoTTicks = Plugin.Configuration.ShowRealDoTTicks;
        if (ImGui.Checkbox("显示真实 DoT Ticks", ref showRealDoTTicks))
        {
            Plugin.Configuration.ShowRealDoTTicks = showRealDoTTicks;
            Plugin.Configuration.Save();
        }
    }

    private void DrawWebSocketSettings()
    {
        // using var tab = ImRaii.TabItem("WebSocket 服务");
        // if (!tab) return;

        ImGui.Spacing();
        var wsServerIp = OverlayPluginConfig?.WSServerIP ?? "";
        ImGui.InputText("IP地址", ref wsServerIp, 100, ImGuiInputTextFlags.None);

        if (IPAddress.TryParse(wsServerIp, out var address))
        {
            if (OverlayPluginConfig is not null)
                OverlayPluginConfig.WSServerIP = address.ToString();
        }
        else if (wsServerIp == "*")
        {
            if (OverlayPluginConfig is not null)
                OverlayPluginConfig.WSServerIP = "*";
        }

        var wsServerPort = OverlayPluginConfig?.WSServerPort.ToString() ?? "";
        ImGui.InputText("端口", ref wsServerPort, 100, ImGuiInputTextFlags.None);

        if (int.TryParse(wsServerPort, out var port))
        {
            if (OverlayPluginConfig is not null)
                OverlayPluginConfig.WSServerPort = port;
        }

        OverlayPluginConfig?.Save();
    }

    private void DrawTTSSettings()
    {
        using var tab = ImRaii.TabItem("文字转语音");
        if (!tab) return;

        ImGui.Spacing();
        var useEdgeTTS = Plugin.Configuration.UseEdgeTTS;
        if (ImGui.Checkbox("使用EdgeTTS（不勾选则使用本地TTS）", ref useEdgeTTS))
            Plugin.TextToSpeechProvider.SetUseEdgeTTS(useEdgeTTS);
        if (useEdgeTTS)
        {
            ImGui.SameLine();
            if (ImGui.Button("打开设置"))
            {
                Plugin.OpenEdgeTTSWindow();
            }
        }
        var useLatihasTTS = Plugin.Configuration.UseLatihasTts;
        if (ImGui.Checkbox("使用LatihasTTS（均不勾选则使用本地TTS）", ref useLatihasTTS))
            Plugin.TextToSpeechProvider.SetUseLatihasTTS(useLatihasTTS);
        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();
    }

    private string GetParseFilterModeText(ParseFilterMode mode)
    {
        return mode switch
        {
            ParseFilterMode.None => "无 (None)",
            ParseFilterMode.Self => "仅自己 (Self)",
            ParseFilterMode.Party => "仅小队成员 (Party)",
            ParseFilterMode.Alliance => "仅团队成员 (Alliance)",
            _ => mode.ToString()
        };
    }
}
