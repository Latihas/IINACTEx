using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using Newtonsoft.Json;
using RainbowMage.OverlayPlugin.WebSocket;

namespace IINACT.Latihas.Overlay;

public class OverlayWindow() : Window("IINACTEx Overlay###IINACTEx Overlay"), IDisposable
{
    private const ImGuiTableFlags ImGuiTableFlag = ImGuiTableFlags.Borders | ImGuiTableFlags.Resizable | ImGuiTableFlags.RowBg;
    private WebSocketClient? webSocketClient;
    private CombatDataWrapper? currentCombatData;
    private readonly List<HistoricalCombatData> historicalRecords = [];
    private HistoricalCombatData? selectedHistoricalRecord;
    private bool isActive;
    public void Dispose() => webSocketClient?.Dispose();

    public void Parse(string data)
    {
        if (webSocketClient is not { Ready: true }) return;
        try
        {
            var x = JsonConvert.DeserializeObject<CombatDataWrapper>(data);
            Plugin.Log.Info(data);
            if (x is { Type: "broadcast", MsgType: "CombatData" } && x.Msg.Combatant.Count != 0)
            {
                currentCombatData = x;
                var newActive = bool.Parse(x.Msg.IsActive);
                if (isActive && !newActive)
                    AddHistoricalRecord(currentCombatData);
                isActive = newActive;
            }
        }
        catch (Exception)
        {
            //
        }
    }

    public void Init(ServerController? server)
    {
        try
        {
            webSocketClient = new WebSocketClient();
            _ = webSocketClient.Connect($"ws://{server?.Address}:{server?.Port}/MiniParse");
            webSocketClient.OnDataReceived += Parse;
        }
        catch (Exception e)
        {
            Plugin.Log.Error(e.ToString());
        }
    }

    private void AddHistoricalRecord(CombatDataWrapper combatData)
    {
        var str = JsonConvert.SerializeObject(combatData);
        foreach (var c in historicalRecords)
            if (c.RawStr == str)
                return;
        var encounter = combatData.Msg.Encounter;
        var record = new HistoricalCombatData
        {
            RawStr = str,
            ZoneName = encounter.CurrentZoneName,
            Duration = encounter.duration,
            EncounterData = encounter,
            CombatantData = new Dictionary<string, Combatant>(combatData.Msg.Combatant)
        };
        historicalRecords.Add(record);
        Plugin.Log.Info($"记录战斗结束: {record.ZoneName}，时长: {record.Duration}");
    }

    public override void Draw()
    {
        if (currentCombatData == null)
        {
            ImGui.Text("等待战斗数据...");
            return;
        }
        
        WindowName = $"IINACTEx Overlay ({currentCombatData.Msg.Encounter.duration})###IINACTEx Overlay";
        using var combatDataTabs = ImRaii.TabBar("CombatDataTabs");
        if (combatDataTabs)
        {
            using (var dps = ImRaii.TabItem("DPS"))
                if (dps)
                    DrawCatDetails("DPS", c => new Cat(c.name, c.Job, c.ENCDPS));
            using (var hps = ImRaii.TabItem("HPS"))
                if (hps)
                    DrawCatDetails("HPS", c => new Cat(c.name, c.Job, c.ENCHPS));
            using (var dmg = ImRaii.TabItem("DMG"))
                if (dmg)
                    DrawCatDetails("DMG", c => new Cat(c.name, c.Job, c.damagetaken));
            using (var historyTab = ImRaii.TabItem("历史记录"))
                if (historyTab)
                    DrawHistoricalRecords();
            using (var comb = ImRaii.TabItem("战斗概览"))
                if (comb)
                {
                    DrawEncounterOverview(currentCombatData);
                    DrawCombatantDetails(currentCombatData);
                }
        }
    }

    private void DrawHistoricalRecords()
    {
        using var split = ImRaii.Table("HistorySplit", 2, ImGuiTableFlag);
        if (split)
        {
            ImGui.TableSetupColumn("历史记录列表", ImGuiTableColumnFlags.WidthStretch, 0.3f);
            ImGui.TableSetupColumn("详情", ImGuiTableColumnFlags.WidthStretch, 0.6f);
            ImGui.TableNextRow();
            ImGui.TableSetColumnIndex(0);
            using (var listChild = ImRaii.Child("HistoryList", new Vector2(0, 0), true))
            {
                if (listChild)
                {
                    if (ImGui.Button("清空历史记录"))
                    {
                        historicalRecords.Clear();
                        selectedHistoricalRecord = null;
                    }

                    ImGui.Separator();
                    if (historicalRecords.Count == 0)
                    {
                        ImGui.Text("暂无历史记录");
                    }
                    else
                    {
                        foreach (var record in historicalRecords)
                        {
                            var isSelected = selectedHistoricalRecord?.RawStr == record.RawStr;
                            var displayText = $"{record.Timestamp:yyyy-MM-dd HH:mm} | {record.ZoneName} ({record.Duration})";
                            if (ImGui.Selectable(displayText, isSelected))
                            {
                                selectedHistoricalRecord = record;
                            }
                        }
                    }
                }
            }


            ImGui.TableSetColumnIndex(1);
            using (var detailChild = ImRaii.Child("HistoryDetail", new Vector2(0, 0)))
            {
                if (detailChild)
                {
                    if (selectedHistoricalRecord == null)
                    {
                        ImGui.Text("请选择一条历史记录查看详情");
                        return;
                    }
                    var data = new CombatDataWrapper
                    {
                        Msg = new CombatData
                        {
                            Encounter = selectedHistoricalRecord.EncounterData,
                            Combatant = selectedHistoricalRecord.CombatantData
                        }
                    };
                    DrawEncounterOverview(data);
                    DrawCombatantDetails(data);
                }
            }
        }
    }

    private void DrawEncounterOverview(CombatDataWrapper combatData)
    {
        if (combatData?.Msg.Encounter == null) return;
        var encounter = combatData.Msg.Encounter;
        ImGui.Text($"区域: {encounter.CurrentZoneName}");
        ImGui.Text($"战斗时长: {encounter.duration}");
        ImGui.Separator();
        using var table = ImRaii.Table("EncounterStats", 2);
        if (table)
        {
            foreach (var data in new[]
                     {
                         ("总伤害", encounter.damage_star),
                         ("DPS", encounter.ENCDPS),
                         ("命中/失误", $"{encounter.hits}/{encounter.hitfailed}"),
                         ("暴击次数", $"{encounter.crithits} ({encounter.crithit_percent})"),
                         ("最大伤害", $"{encounter.maxhit_star} ({encounter.MAXHIT_star})"),
                         ("承伤", encounter.damagetaken_star)
                     })
            {
                ImGui.TableNextRow();
                ImGui.TableSetColumnIndex(0);
                ImGui.Text(data.Item1);
                ImGui.TableSetColumnIndex(1);
                ImGui.Text(data.Item2);
            }
        }
    }

    private static readonly Func<Cat, int> Catval = c => c.ValueString is "∞" or "---" ? 0 : int.Parse(c.ValueString);

    private void DrawCatDetails(string name, Func<Combatant, Cat> selector)
    {
        if (currentCombatData == null)
        {
            ImGui.Text("暂无战斗数据");
            return;
        }
        var combatantList = currentCombatData.Msg.Combatant.Values.Select(selector)
                                             .Where(c => !string.IsNullOrEmpty(c.Name))
                                             .OrderByDescending(Catval).ToList();

        if (combatantList.Count == 0)
        {
            ImGui.Text("无有效数据");
            return;
        }

        using var table = ImRaii.Table("ValueTable", 4, ImGuiTableFlags.Resizable);
        if (table)
        {
            ImGui.TableSetupColumn("玩家", ImGuiTableColumnFlags.WidthFixed, 100f);
            ImGui.TableSetupColumn("职业", ImGuiTableColumnFlags.WidthFixed, 60f);
            ImGui.TableSetupColumn(name, ImGuiTableColumnFlags.WidthFixed, 80f);
            ImGui.TableSetupColumn("占比", ImGuiTableColumnFlags.WidthStretch);
            ImGui.TableHeadersRow();
            var maxDps = combatantList.Max(Catval);
            var totalDps = combatantList.Sum(Catval);
            ;
            foreach (var combatant in combatantList)
            {
                ImGui.TableNextRow();
                ImGui.TableSetColumnIndex(0);
                ImGui.Text(combatant.Name);
                ImGui.TableSetColumnIndex(1);
                ImGui.Text(combatant.Job);
                ImGui.TableSetColumnIndex(2);
                ImGui.Text(combatant.ValueString);
                ImGui.TableSetColumnIndex(3);
                var barWidth = ImGui.GetContentRegionAvail().X;
                var dpsRatio = maxDps > 0 ? 1f * Catval(combatant) / maxDps : 0f;
                var drawList = ImGui.GetWindowDrawList();
                var barPos = ImGui.GetCursorScreenPos();
                var barSize = new Vector2(barWidth, ImGui.GetTextLineHeight());
                drawList.AddRectFilled(barPos, barPos + barSize, ImGui.GetColorU32(ImGuiCol.FrameBg));
                drawList.AddRectFilled(barPos, barPos + barSize with
                {
                    X = barWidth * dpsRatio
                }, combatant.Name == "YOU" ? ImGui.GetColorU32(new Vector4(1, 1, 1, .5f)) : GetProgressColor(dpsRatio));
                ImGui.SetCursorScreenPos(barPos + new Vector2(5, (barSize.Y - ImGui.GetTextLineHeight()) / 2));
                ImGui.Text($"{Math.Round(dpsRatio * 100, 1)}%({Math.Round(100f * Catval(combatant) / totalDps, 1)}%)");
            }
        }
    }

    private static uint GetProgressColor(float ratio)
    {
        var r = (byte)(255 * (1 - ratio));
        var g = (byte)(255 * ratio);
        const byte b = 60;
        return ImGui.GetColorU32(new Vector4(r / 255f, g / 255f, b / 255f, .7f));
    }

    private static void DrawCombatantDetails(CombatDataWrapper combatData)
    {
        if (combatData?.Msg.Combatant == null) return;
        foreach (var combatant in combatData.Msg.Combatant)
        {
            var combatantData = combatant.Value;
            if (ImGui.CollapsingHeader($"{combatantData.name} ({combatantData.Job})"))
            {
                ImGui.BeginTable("DamageStats", 2);

                ImGui.TableNextRow();
                ImGui.TableSetColumnIndex(0);
                ImGui.Text("伤害占比:");
                ImGui.TableSetColumnIndex(1);
                ImGui.Text($"{combatantData.damage_percent}");

                ImGui.TableNextRow();
                ImGui.TableSetColumnIndex(0);
                ImGui.Text("总伤害:");
                ImGui.TableSetColumnIndex(1);
                ImGui.Text($"{combatantData.damage_star}");

                ImGui.TableNextRow();
                ImGui.TableSetColumnIndex(0);
                ImGui.Text("DPS:");
                ImGui.TableSetColumnIndex(1);
                ImGui.Text($"{combatantData.ENCDPS}");

                ImGui.TableNextRow();
                ImGui.TableSetColumnIndex(0);
                ImGui.Text("最大伤害技能:");
                ImGui.TableSetColumnIndex(1);
                ImGui.Text($"{combatantData.maxhit_star}");

                ImGui.EndTable();

                ImGui.BeginTable("HitStats", 2);

                ImGui.TableNextRow();
                ImGui.TableSetColumnIndex(0);
                ImGui.Text("命中次数:");
                ImGui.TableSetColumnIndex(1);
                ImGui.Text($"{combatantData.hits}");

                ImGui.TableNextRow();
                ImGui.TableSetColumnIndex(0);
                ImGui.Text("暴击次数:");
                ImGui.TableSetColumnIndex(1);
                ImGui.Text($"{combatantData.crithits}");

                ImGui.TableNextRow();
                ImGui.TableSetColumnIndex(0);
                ImGui.Text("命中率:");
                ImGui.TableSetColumnIndex(1);
                ImGui.Text($"{combatantData.tohit}%");

                ImGui.EndTable();

                ImGui.BeginTable("SurvivalStats", 2);

                ImGui.TableNextRow();
                ImGui.TableSetColumnIndex(0);
                ImGui.Text("承受伤害:");
                ImGui.TableSetColumnIndex(1);
                ImGui.Text($"{combatantData.damagetaken_star}");

                ImGui.TableNextRow();
                ImGui.TableSetColumnIndex(0);
                ImGui.Text("格挡率:");
                ImGui.TableSetColumnIndex(1);
                ImGui.Text($"{combatantData.BlockPct}");

                ImGui.TableNextRow();
                ImGui.TableSetColumnIndex(0);
                ImGui.Text("招架率:");
                ImGui.TableSetColumnIndex(1);
                ImGui.Text($"{combatantData.ParryPct}");

                ImGui.EndTable();
            }
        }
    }

    public class HistoricalCombatData
    {
        public string RawStr { get; set; } = null!;

        public DateTime Timestamp { get; set; } = DateTime.Now;

        public string ZoneName { get; set; } = string.Empty;

        public string Duration { get; set; } = string.Empty;

        public Encounter EncounterData { get; set; }

        public Dictionary<string, Combatant> CombatantData { get; set; }
    }

    public record Cat(
        string Name,
        string Job,
        string ValueString
    );
}
