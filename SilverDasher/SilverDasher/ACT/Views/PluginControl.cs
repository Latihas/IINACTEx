using System;
using System.Collections.Generic;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility.Raii;
using SilverDasher.ACT.Doppelgangers;
using SilverDasher.ACT.Models;
using SilverDasher.ACT.ViewModels;

namespace SilverDasher.ACT.Views;

public class PluginControl {
    private readonly Painter Painter;
    private static readonly Dictionary<PluginStatus, string> ChineseStatusText = new() {
        {
            PluginStatus.SLEEPING, "睡觉中"
        }, {
            PluginStatus.INITIALIZED, "待命"
        }, {
            PluginStatus.CONNECTING, "连线中"
        }, {
            PluginStatus.CONNECTED, "在线"
        }, {
            PluginStatus.EXPIRED, "已过期"
        }, {
            PluginStatus.BANNED, "封印中"
        }, {
            PluginStatus.BLOCKED, "迷路中"
        }, {
            PluginStatus.FAILED, "连接失败"
        }, {
            PluginStatus.LEFT, "消失了"
        }
    };

    private readonly List<string> textLog = [];


    internal PluginViewModel ViewModel { get; } = new();

    internal PluginControl(Painter painter) {
        Painter = painter;
        Init();
    }

    private void Init() {
        ViewModel.SetPatchNames(Painter.Keeper.Patches.PatchByCode);
        ViewModel.SetupHuntMobs(Painter.Keeper.Mobs.HuntByBnpcNameID, Painter.Keeper.SpHunts.HuntGroupsTree, Keeper.Config.HuntSubscriptions);
        ViewModel.SetupFates(Painter.Keeper.Fates.FateByID, Painter.Keeper.SpFates.FateGroupsTree, Keeper.Config.FateSubscriptions);
    }

    private void DrawEvent() {
        using var bar = ImRaii.TabItem("订阅##SilverdasherEvent");
        if (!bar) return;
        ImGui.Text($"插件状态: {Enum.GetName(SilverDasher.pluginStatus)}");
        ImGui.Text($"状态描述: {ChineseStatusText[SilverDasher.pluginStatus]}");
        ViewModel.DrawImGui();
    }

    private void DrawSettings() {
        using var bar = ImRaii.TabItem("设置##SilverdasherSettings");
        if (!bar) return;
        var PauseInDuty = Keeper.Config.PauseInDuty;
        if (ImGui.Checkbox("副本任务中暂停推送", ref PauseInDuty)) {
            Keeper.Config.PauseInDuty = PauseInDuty;
            Config.Save();
        }
        ImGui.SameLine();
        if (ImGui.Button("刷新配置并重新连接")) ButtonRestartClicked();
        if (ImGui.CollapsingHeader("TTS", ImGuiTreeNodeFlags.DefaultOpen)) {
            ImGui.Indent();
            var TTS = Keeper.Config.TTS;
            if (ImGui.Checkbox("启用TTS", ref TTS)) {
                Keeper.Config.TTS = TTS;
                Config.Save();
            }
            ImGui.SameLine();
            if (ImGui.Button("测试##TTS")) ButtonTestTTSClicked();
            if (TTS) {
                var NotifySpottedTTS = Keeper.Config.NotifySpottedTTS;
                if (ImGui.Checkbox("健康##NotifySpottedTTS", ref NotifySpottedTTS)) {
                    Keeper.Config.NotifySpottedTTS = NotifySpottedTTS;
                    Config.Save();
                }
                ImGui.SameLine();
                var NotifyTauntedTTS = Keeper.Config.NotifyTauntedTTS;
                if (ImGui.Checkbox("开怪##NotifyTauntedTTS", ref NotifyTauntedTTS)) {
                    Keeper.Config.NotifyTauntedTTS = NotifyTauntedTTS;
                    Config.Save();
                }
                ImGui.SameLine();
                var NotifyBullyingTTS = Keeper.Config.NotifyBullyingTTS;
                if (ImGui.Checkbox("暴打##NotifyBullyingTTS", ref NotifyBullyingTTS)) {
                    Keeper.Config.NotifyBullyingTTS = NotifyBullyingTTS;
                    Config.Save();
                }
                ImGui.SameLine();
                var NotifyDiedTTS = Keeper.Config.NotifyDiedTTS;
                if (ImGui.Checkbox("死亡##NotifyDiedTTS", ref NotifyDiedTTS)) {
                    Keeper.Config.NotifyDiedTTS = NotifyDiedTTS;
                    Config.Save();
                }
            }
            ImGui.Unindent();
        }
        if (ImGui.CollapsingHeader("通知", ImGuiTreeNodeFlags.DefaultOpen)) {
            ImGui.Indent();
            var SystemToast = Keeper.Config.SystemToast;
            if (ImGui.Checkbox("启用通知", ref SystemToast)) {
                Keeper.Config.SystemToast = SystemToast;
                Config.Save();
            }
            ImGui.SameLine();
            if (ImGui.Button("测试##SystemToast")) ButtonTestToastClicked();
            if (SystemToast) {
                var NotifySpottedToast = Keeper.Config.NotifySpottedToast;
                if (ImGui.Checkbox("健康##NotifySpottedToast", ref NotifySpottedToast)) {
                    Keeper.Config.NotifySpottedToast = NotifySpottedToast;
                    Config.Save();
                }
                ImGui.SameLine();
                var NotifyTauntedToast = Keeper.Config.NotifyTauntedToast;
                if (ImGui.Checkbox("开怪##NotifyTauntedToast", ref NotifyTauntedToast)) {
                    Keeper.Config.NotifyTauntedTTS = NotifyTauntedToast;
                    Config.Save();
                }
                ImGui.SameLine();
                var NotifyBullyingToast = Keeper.Config.NotifyBullyingToast;
                if (ImGui.Checkbox("暴打##NotifyBullyingToast", ref NotifyBullyingToast)) {
                    Keeper.Config.NotifyBullyingTTS = NotifyBullyingToast;
                    Config.Save();
                }
                ImGui.SameLine();
                var NotifyDiedToast = Keeper.Config.NotifyDiedToast;
                if (ImGui.Checkbox("死亡##NotifyDiedToast", ref NotifyDiedToast)) {
                    Keeper.Config.NotifyDiedToast = NotifyDiedToast;
                    Config.Save();
                }
            }
            ImGui.Unindent();
        }
        if (ImGui.CollapsingHeader("大区接收", ImGuiTreeNodeFlags.DefaultOpen)) {
            ImGui.Indent();
            var CrossWorldHunt = Keeper.Config.CrossWorldHunt;
            if (ImGui.Checkbox("跨服接收狩猎", ref CrossWorldHunt)) {
                Keeper.Config.CrossWorldHunt = CrossWorldHunt;
                Config.Save();
            }
            if (CrossWorldHunt) {
                var CWHuntSS = Keeper.Config.CWHuntSS;
                if (ImGui.Checkbox("SS##CWHuntSS", ref CWHuntSS)) {
                    Painter.Self.Messager.Resubscribe("hunt", SilverDasher.Instance.tokenSource.Token);
                    Keeper.Config.CWHuntSS = CWHuntSS;
                    Config.Save();
                }
                ImGui.SameLine();
                var CWHuntS = Keeper.Config.CWHuntS;
                if (ImGui.Checkbox("S##CWHuntS", ref CWHuntS)) {
                    Painter.Self.Messager.Resubscribe("hunt", SilverDasher.Instance.tokenSource.Token);
                    Keeper.Config.CWHuntS = CWHuntS;
                    Config.Save();
                }
                ImGui.SameLine();
                var CWHuntA = Keeper.Config.CWHuntA;
                if (ImGui.Checkbox("A##CWHuntA", ref CWHuntA)) {
                    Painter.Self.Messager.Resubscribe("hunt", SilverDasher.Instance.tokenSource.Token);
                    Keeper.Config.CWHuntA = CWHuntA;
                    Config.Save();
                }
                ImGui.SameLine();
                var CWHuntB = Keeper.Config.CWHuntB;
                if (ImGui.Checkbox("B##CWHuntB", ref CWHuntB)) {
                    Painter.Self.Messager.Resubscribe("hunt", SilverDasher.Instance.tokenSource.Token);
                    Keeper.Config.CWHuntB = CWHuntB;
                    Config.Save();
                }
            }
            var CrossWorldFate = Keeper.Config.CrossWorldFate;
            if (ImGui.Checkbox("跨服接收Fate", ref CrossWorldFate)) {
                Keeper.Config.CrossWorldFate = CrossWorldFate;
                Config.Save();
            }
            if (CrossWorldFate) {
                var CWFateCommon = Keeper.Config.CWFateCommon;
                if (ImGui.Checkbox("普通Fate", ref CWFateCommon)) {
                    Painter.Self.Messager.Resubscribe("fate", SilverDasher.Instance.tokenSource.Token);
                    Keeper.Config.CWFateCommon = CWFateCommon;
                    Config.Save();
                }
                ImGui.SameLine();
                var CWFateSpecial = Keeper.Config.CWFateSpecial;
                if (ImGui.Checkbox("特殊Fate", ref CWFateSpecial)) {
                    Painter.Self.Messager.Resubscribe("fate", SilverDasher.Instance.tokenSource.Token);
                    Keeper.Config.CWFateSpecial = CWFateSpecial;
                    Config.Save();
                }
            }
            ImGui.Unindent();
        }
    }

    private void DrawLog() {
        using var bar = ImRaii.TabItem("日志##SilverdasherLog");
        if (!bar) return;
        var ExtendedReport = Keeper.Config.ExtendedReport;
        if (ImGui.Checkbox("调试模式", ref ExtendedReport)) {
            Keeper.Config.ExtendedReport = ExtendedReport;
            Config.Save();
        }
        ImGui.SameLine();
        if (ImGui.Button("清空日志")) textLog.Clear();
        foreach (var log in textLog) ImGui.Text(log);
    }

    public void Draw() {
        using var bar = ImRaii.TabBar("SilverDasherSettings");
        if (!bar) return;
        DrawEvent();
        DrawSettings();
        DrawLog();
    }

    public static void SetPluginStatus(PluginStatus s) {
        SilverDasher.pluginStatus = s;
    }

    public void Log(string s) {
        textLog.Add(s);
    }


    private void ButtonTestTTSClicked() => Notifier.TestTTS();

    private void ButtonTestToastClicked() => Painter.Notifier.TestToast();

    private void ButtonRestartClicked() => Painter.Self.Painter.Self.RestartLoop(true);
}