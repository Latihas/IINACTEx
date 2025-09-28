using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility.Raii;
using Triggernometry;
using Triggernometry.Variables;

namespace IINACT.Latihas;

public static partial class LWindow
{
    internal static string WindowPrefix = "";


    internal static void DrawTriggerSettings()
    {
        using var tab = ImRaii.TabItem("Trigger");
        if (!tab) return;
        using var bar = ImRaii.TabBar("TriggerBar");
        if (!bar) return;
        DrawTriggerTriggerSettings();
        DrawTriggerVarSettings();
    }

    internal static void DrawTriggerTriggerSettings()
    {
        using var tab = ImRaii.TabItem("触发器");
        if (!tab) return;
        if (ImGui.Button("刷新触发器")) Triggernometry.CustomControls.UserInterface.BuildTriggerTreeFromConfiguration(null, null);
        ImGui.SameLine();
        if (ImGui.Button("保存设置") && !RealPlugin.plug.configBroken)
            RealPlugin.plug.SaveCurrentConfig();
        ImGui.Text("由于现版本不稳定，不会自动保存配置文件。请导入或修改过任何触发器/配置/...后手动保存。");
        ImGui.ProgressBar(RealPlugin.UProgress / 100f, new Vector2(300, 24), RealPlugin.UState);
        Triggernometry.CustomControls.UserInterface.BuildRenderTreeFromConfiguration(null, null, false);
    }

    internal static void DrawTriggerVarSettings()
    {
        using var tab = ImRaii.TabItem("变量");
        if (!tab) return;
        using var bar = ImRaii.TabBar("变量Bar");
        if (!bar) return;
        DrawTriggerVarScalerSettings();
        DrawTriggerVarPScalerSettings();
        DrawTriggerVarListSettings();
        DrawTriggerVarPListSettings();
        DrawTriggerVarTableSettings();
        DrawTriggerVarPTableSettings();
        DrawTriggerVarDictSettings();
        DrawTriggerVarPDictSettings();
    }

    private static void TScaler(SerializableDictionary<string, VariableScalar> data) =>
        NewTable(["名称", "值", "时间", "源"], data.ToArray(), [
            i => ImGui.Text(i.Key),
            i => ImGui.Text(i.Value.Value),
            i => ImGui.Text(i.Value.LastChanged.ToString()),
            i => ImGui.Text(i.Value.LastChanger),
        ]);

    internal static void DrawTriggerVarScalerSettings()
    {
        using var tab = ImRaii.TabItem("临时标量");
        if (!tab) return;
        TScaler(RealPlugin.plug.sessionvars.Scalar);
    }

    internal static void DrawTriggerVarPScalerSettings()
    {
        using var tab = ImRaii.TabItem("永久标量");
        if (!tab) return;
        TScaler(RealPlugin.plug.cfg.PersistentVariables.Scalar);
    }

    private static void TList(SerializableDictionary<string, VariableList> data) =>
        NewTable(["名称", "值", "时间", "源"], data.ToArray(), [
            i => ImGui.Text(i.Key),
            i => ImGui.Text(i.Value.Size.ToString()),
            i => ImGui.Text(i.Value.Values.ToString()),
            i => ImGui.Text(i.Value.LastChanged.ToString()),
            i => ImGui.Text(i.Value.LastChanger),
        ]);

    internal static void DrawTriggerVarListSettings()
    {
        using var tab = ImRaii.TabItem("临时列表");
        if (!tab) return;
        TList(RealPlugin.plug.sessionvars.List);
    }

    internal static void DrawTriggerVarPListSettings()
    {
        using var tab = ImRaii.TabItem("永久列表");
        if (!tab) return;
        TList(RealPlugin.plug.cfg.PersistentVariables.List);
    }

    private static void TTable(SerializableDictionary<string, VariableTable> data) =>
        NewTable(["名称", "宽", "高", "值", "时间", "源"], data.ToArray(), [
            i => ImGui.Text(i.Key),
            i => ImGui.Text(i.Value.Width.ToString()),
            i => ImGui.Text(i.Value.Height.ToString()),
            i => ImGui.Text(i.Value.Rows.ToString()),
            i => ImGui.Text(i.Value.LastChanged.ToString()),
            i => ImGui.Text(i.Value.LastChanger),
        ]);

    internal static void DrawTriggerVarTableSettings()
    {
        using var tab = ImRaii.TabItem("临时表格");
        if (!tab) return;
        TTable(RealPlugin.plug.sessionvars.Table);
    }

    internal static void DrawTriggerVarPTableSettings()
    {
        using var tab = ImRaii.TabItem("永久表格");
        if (!tab) return;
        TTable(RealPlugin.plug.cfg.PersistentVariables.Table);
    }

    private static void TDict(SerializableDictionary<string, VariableDictionary> data) =>
        NewTable(["名称", "长度", "值", "时间", "源"], data.ToArray(), [
            i => ImGui.Text(i.Key),
            i => ImGui.Text(i.Value.Size.ToString()),
            i => ImGui.Text(i.Value.Values.ToString()),
            i => ImGui.Text(i.Value.LastChanged.ToString()),
            i => ImGui.Text(i.Value.LastChanger),
        ]);

    internal static void DrawTriggerVarDictSettings()
    {
        using var tab = ImRaii.TabItem("临时字典");
        if (!tab) return;
        TDict(RealPlugin.plug.sessionvars.Dict);
    }

    internal static void DrawTriggerVarPDictSettings()
    {
        using var tab = ImRaii.TabItem("永久字典");
        if (!tab) return;
        TDict(RealPlugin.plug.cfg.PersistentVariables.Dict);
    }


    private static void NewTable<T>(string[] header, T[]? data, Action<T>[] acts)
    {
        if (data is null || data.Length == 0) return;
        if (ImGui.BeginTable("Table", acts.Length, ImGuiTableFlag))
        {
            foreach (var item in header) ImGui.TableSetupColumn(item, ImGuiTableColumnFlags.WidthStretch);
            ImGui.TableHeadersRow();
            foreach (var res in data)
            {
                ImGui.TableNextRow();
                for (var i = 0; i < acts.Length; i++)
                {
                    ImGui.TableSetColumnIndex(i);
                    acts[i](res);
                }
            }
            ImGui.EndTable();
        }
    }


    private const ImGuiTableFlags ImGuiTableFlag = ImGuiTableFlags.Borders | ImGuiTableFlags.Resizable | ImGuiTableFlags.RowBg;
}
