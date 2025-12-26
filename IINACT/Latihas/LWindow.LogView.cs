using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.ImGuiFileDialog;
using Dalamud.Interface.ImGuiNotification;
using Dalamud.Interface.Windowing;
using SharpDX.Direct2D1;
using Triggernometry.Core;
using WebSocketSharp;
using static IINACT.Latihas.Color;
using FileDialog = Dalamud.Interface.ImGuiFileDialog.FileDialog;

namespace IINACT.Latihas;

public partial class LWindow
{
    public class TriggernometryLogView() : Window($"{WindowPrefix}TriggernometryLogView")
    {
        private static string RegexExpr = "";
        private static Regex? Reg;
        private Dictionary<RealPlugin.DebugLevelEnum, bool> check = Enum.GetValues<RealPlugin.DebugLevelEnum>()
                                                                        .ToDictionary(i => i, _ => true);

        public override void Draw()
        {
            ImGui.Text($"共{RealPlugin.Instance.logFlattenTrn.Count}条");
            if (ImGui.InputText("## TrnRegex", ref RegexExpr))
                Reg = RegexExpr.IsNullOrEmpty() ? null : new Regex(RegexExpr);
            var i = 1;
            foreach (var k in check.Keys)
            {
                var b = check[k];
                if (ImGui.Checkbox(k.ToString(), ref b)) check[k] = b;
                if (i++ != check.Count) ImGui.SameLine();
            }
            try
            {
                NewTable(["Time", "Lvl", "Log"], RealPlugin.Instance.logFlattenTrn.Where(i => check[i.Level] && (Reg == null || Reg.IsMatch(i.Message))).ToArray(), [
                        i => ImGui.Text(i.Timestamp.ToString()),
                        i => ImGui.Text(i.Level.ToString()),
                        i => ImGui.TextWrapped(i.Message)
                    ], i => i.Level switch
                    {
                        RealPlugin.DebugLevelEnum.None => TWhite,
                        RealPlugin.DebugLevelEnum.Error => TRed,
                        RealPlugin.DebugLevelEnum.Warning => TYellow,
                        RealPlugin.DebugLevelEnum.Custom => TCyan,
                        RealPlugin.DebugLevelEnum.Custom2 => TPurple,
                        RealPlugin.DebugLevelEnum.Info => TBlue,
                        RealPlugin.DebugLevelEnum.Verbose => TGray,
                        RealPlugin.DebugLevelEnum.Inherit => TWhite,
                        _ => Black
                    }
                );
            }
            catch (Exception)
            {
                //
            }
        }
    }

    public partial class ACTLogView() : Window($"{WindowPrefix}ACTLogView")
    {
        private static string RegexExpr = "";
        private static bool FromFile;
        private static Regex? Reg;
        private static readonly Regex regexChatLog = RegexChatLog(), regexOverlay = RegexOverlay();
        private static readonly FileDialogManager FileDialogManager = new();
        private static string FilePath = "";
        private static List<string> ImportLogs = [];
        private static string[] F(IEnumerable<string> l) => l.Where(i => Reg == null || Reg.IsMatch(i)).ToArray();

        public static string ReadLockedTextFile(string filePath)
        {
            using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var reader = new StreamReader(stream);
            return reader.ReadToEnd();
        }

        public override void Draw()
        {
            ImGui.Checkbox("从.actxt文件导入", ref FromFile);
            if (FromFile)
            {
                FileDialogManager.Draw();
                if (ImGui.Button("选择文件"))
                    FileDialogManager.OpenFileDialog("Selector", ".actxt", (succ, str) =>
                    {
                        if (succ)
                        {
                            FilePath = str.First();
                            ImportLogs = ReadLockedTextFile(FilePath).Split(Environment.NewLine).ToList();
                        }
                    }, 1, Plugin.Configuration.LogFilePath);
                ImGui.SameLine();
                ImGui.Text(FilePath);
            }

            ImGui.Text($"共{(FromFile ? ImportLogs.Count : RealPlugin.Instance.logFlattenACT.Count)}条");
            if (ImGui.InputText("## ActRegex", ref RegexExpr))
                Reg = RegexExpr.IsNullOrEmpty() ? null : new Regex(RegexExpr);
            try
            {
                NewTable(["Log"], F(FromFile ? ImportLogs : RealPlugin.Instance.logFlattenACT), [
                    i =>
                    {
                        ImGui.PushID(i);
                        ImGui.TextWrapped(i);
                        if (ImGui.IsItemClicked(ImGuiMouseButton.Left))
                        {
                            ImGui.SetClipboardText(i);
                            Plugin.NotificationManager.AddNotification(new Notification
                            {
                                Content = "已复制"
                            });
                        }
                        ImGui.PopID();
                    }
                ], i =>
                {
                    if (regexChatLog.IsMatch(i)) return TWhite;
                    if (regexOverlay.IsMatch(i)) return TYellow;
                    return Black;
                });
            }
            catch (Exception)
            {
                //
            }
        }

        [GeneratedRegex(@"^.{14} ChatLog ")]
        private static partial Regex RegexChatLog();

        [GeneratedRegex(@"^.{14} \d{3} \d{3}:")]
        private static partial Regex RegexOverlay();
    }
}
