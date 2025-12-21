using System.Diagnostics;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.ImGuiNotification;
using Dalamud.Interface.Utility.Raii;
using Triggernometry;
using Triggernometry.Core;
using Triggernometry.Core.Serialization;
using Triggernometry.Core.Variables;
using Triggernometry.UI.CustomControls;
using Triggernometry.Utilities;

namespace IINACT.Latihas;

public static partial class LWindow
{
    internal static string WindowPrefix = "";

    internal static void DrawTriggerSettings()
    {
        using var tab = ImRaii.TabItem("触发器");
        if (!tab) return;
        using var bar = ImRaii.TabBar("TriggerBar");
        if (!bar) return;
        DrawTriggerTriggerSettings();
        DrawTriggerVarSettings();
    }

    internal static void DrawTriggerTriggerSettings()
    {
        using var tab = ImRaii.TabItem("触发器仓库");
        if (!tab) return;
        if (ImGui.Button("刷新触发器")) UserInterface.BuildTriggerTreeFromConfiguration(null, null);
        ImGui.SameLine();
        if (ImGui.Button("保存设置") && !RealPlugin.Instance.configBroken)
            RealPlugin.Instance.SaveCurrentConfig();
        ImGui.Text("由于现版本不稳定，不会自动保存配置文件。请导入或修改过任何触发器/配置/...后手动保存。");
        ImGui.ProgressBar(RealPlugin.UProgress / 100f, new Vector2(300, 24), RealPlugin.UState);
        UserInterface.BuildRenderTreeFromConfiguration(null, null, false);
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
        DrawTriggerVarTextAuraSettings();
        DrawTriggerVarNamedCallbackSettings();
    }

    internal static void DrawSettings()
    {
        using var tab = ImRaii.TabItem("设置");
        if (!tab) return;
        var DebugLevelItem = (int)RealPlugin.Instance.cfg.DebugLevel;
        if (ImGui.Combo("DebugLevel", ref DebugLevelItem, Enum.GetValues<RealPlugin.DebugLevelEnum>().Select(i => i.ToString()).ToList()))
            RealPlugin.Instance.cfg.DebugLevel = (RealPlugin.DebugLevelEnum)DebugLevelItem;
        var EnableModuleBase = RealPlugin.Instance.cfg.EnableModuleBase;
        if (ImGui.Checkbox("启用EnableModuleBase(极有可能炸游戏的功能，如绘图等，需要重新加载插件生效)", ref EnableModuleBase))
            RealPlugin.Instance.cfg.EnableModuleBase = EnableModuleBase;
        var ShowWindowOnInit = Plugin.Configuration.ShowWindowOnInit;
        if (ImGui.Checkbox("启动时显示界面", ref ShowWindowOnInit))
            Plugin.Configuration.ShowWindowOnInit = ShowWindowOnInit;
        var ShowOverlayOnInit = Plugin.Configuration.ShowOverlayOnInit;
        if (ImGui.Checkbox("启动时显示Overlay", ref ShowOverlayOnInit))
            Plugin.Configuration.ShowOverlayOnInit = ShowOverlayOnInit;
        var TextPort = Plugin.Instance.PostNamazuPlugin.PluginUi.TextPort.Text;
        if (ImGui.InputText("鲇鱼精端口", ref TextPort))
            Plugin.Instance.PostNamazuPlugin.PluginUi.TextPort.Text = TextPort;
        ImGui.SameLine();
        if (Plugin.Instance.PostNamazuPlugin.PluginUi.ButtonStart.Enabled)
            if (ImGui.Button("鲇鱼精启动监听"))
                Plugin.Instance.PostNamazuPlugin.ServerStart();
        if (Plugin.Instance.PostNamazuPlugin.PluginUi.ButtonStop.Enabled)
            if (ImGui.Button("鲇鱼精停止监听"))
                Plugin.Instance.PostNamazuPlugin.ServerStop();
        ImGui.SameLine();
        if(ImGui.Button("清空"))Plugin.Instance.PostNamazuPlugin.PluginUi.lstMessages.Items.Clear();
        var PostNamazuAutoStart = Plugin.Configuration.PostNamazuAutoStart;
        if (ImGui.Checkbox("鲇鱼精监听自动启动", ref PostNamazuAutoStart))
            Plugin.Configuration.PostNamazuAutoStart = PostNamazuAutoStart;
        var items = Plugin.Instance.PostNamazuPlugin.PluginUi.lstMessages.Items;
        for (var i = 0; i < items.Count; i++)
        {
            var item = items[i];
            var itemId = $"## copyItem_{i}";
            if (ImGui.Selectable($"{item}{itemId}"))
            {
                ImGui.SetClipboardText(item.ToString());
                Plugin.NotificationManager.AddNotification(new Notification
                {
                    Content = "已复制"
                });
            }
        }
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
        TScaler(RealPlugin.Instance.sessionvars.Scalar);
        DrawTriggerVarESettings(false, TriggerVarType.Scalar);
    }

    internal static void DrawTriggerVarPScalerSettings()
    {
        using var tab = ImRaii.TabItem("永久标量");
        if (!tab) return;
        TScaler(RealPlugin.Instance.cfg.PersistentVariables.Scalar);
        DrawTriggerVarESettings(true, TriggerVarType.Scalar);
    }

    private enum TriggerVarType
    {
        Scalar,
        List,
        Table,
        Dict
    }

    private static string Ename = "";
    private static string Eexpr = "";

    private static void DrawTriggerVarESettings(bool persist, TriggerVarType type)
    {
    //     ImGui.Separator();
    //     ImGui.InputText("name", ref Ename);
    //     ImGui.InputText("expr", ref Eexpr);
    //     var code = $"using Triggernometry;\nRealPlugin._plug.{(persist ? "cfg.PersistentVariables" : "sessionvars")}.{type}[\"{Ename}\"]=new (){{{type switch {
    //         TriggerVarType.Scalar => "Value = \"",
    //         TriggerVarType.List => "Values = [",
    //         TriggerVarType.Dict => "Values = new Dictionary<string, Variable>()\n{",
    //     }}{Eexpr}{type switch {
    //     TriggerVarType.Scalar => "\"",
    //     TriggerVarType.List => "]",
    //     TriggerVarType.Dict => "}"
    // }}}};";
    //     ImGui.Text(code);
    //     if (ImGui.Button("执行"))
    //         RealPlugin._plug.scripting.Evaluate(code, null, null);
    }

    private static void TList(SerializableDictionary<string, VariableList> data) =>
        NewTable(["名称", "长度", "值", "时间", "源"], data.ToArray(), [
            i => ImGui.Text(i.Key),
            i => ImGui.Text(i.Value.Size.ToString()),
            i => ImGui.TextWrapped(string.Join(",", i.Value.Values)),
            i => ImGui.Text(i.Value.LastChanged.ToString()),
            i => ImGui.Text(i.Value.LastChanger),
        ]);

    internal static void DrawTriggerVarListSettings()
    {
        using var tab = ImRaii.TabItem("临时列表");
        if (!tab) return;
        TList(RealPlugin.Instance.sessionvars.List);
        DrawTriggerVarESettings(false, TriggerVarType.List);
    }

    internal static void DrawTriggerVarPListSettings()
    {
        using var tab = ImRaii.TabItem("永久列表");
        if (!tab) return;
        TList(RealPlugin.Instance.cfg.PersistentVariables.List);
        DrawTriggerVarESettings(true, TriggerVarType.List);
    }

    private static void TTable(SerializableDictionary<string, VariableTable> data) =>
        NewTable(["名称", "宽", "高", "值", "时间", "源"], data.ToArray(), [
            i => ImGui.Text(i.Key),
            i => ImGui.Text(i.Value.Width.ToString()),
            i => ImGui.Text(i.Value.Height.ToString()),
            i =>
            {
                List<string> sb = [];
                foreach (var item in i.Value.Rows)
                    sb.Add($"({string.Join(',', item.Values)})");
                ImGui.TextWrapped(string.Join(';', sb));
            },
            i => ImGui.Text(i.Value.LastChanged.ToString()),
            i => ImGui.Text(i.Value.LastChanger),
        ]);

    internal static void DrawTriggerVarTableSettings()
    {
        using var tab = ImRaii.TabItem("临时表格");
        if (!tab) return;
        TTable(RealPlugin.Instance.sessionvars.Table);
        DrawTriggerVarESettings(false, TriggerVarType.Table);
    }

    internal static void DrawTriggerVarPTableSettings()
    {
        using var tab = ImRaii.TabItem("永久表格");
        if (!tab) return;
        TTable(RealPlugin.Instance.cfg.PersistentVariables.Table);
        DrawTriggerVarESettings(true, TriggerVarType.Table);
    }

    private static void TDict(SerializableDictionary<string, VariableDictionary> data) =>
        NewTable(["名称", "长度", "值", "时间", "源"], data.ToArray(), [
            i => ImGui.Text(i.Key),
            i => ImGui.Text(i.Value.Size.ToString()),
            i =>
            {
                List<string> sb = [];
                foreach (var item in i.Value.Values)
                    sb.Add($"({item.Key}:{item.Value})");
                ImGui.TextWrapped(string.Join(',', sb));
            },
            i => ImGui.Text(i.Value.LastChanged.ToString()),
            i => ImGui.Text(i.Value.LastChanger),
        ]);

    internal static void DrawTriggerVarDictSettings()
    {
        using var tab = ImRaii.TabItem("临时字典");
        if (!tab) return;
        TDict(RealPlugin.Instance.sessionvars.Dict);
        DrawTriggerVarESettings(false, TriggerVarType.Dict);
    }

    internal static void DrawTriggerVarPDictSettings()
    {
        using var tab = ImRaii.TabItem("永久字典");
        if (!tab) return;
        TDict(RealPlugin.Instance.cfg.PersistentVariables.Dict);
        DrawTriggerVarESettings(true, TriggerVarType.Dict);
    }

    internal static void DrawTriggerVarNamedCallbackSettings()
    {
        using var tab = ImRaii.TabItem("具名回调");
        if (!tab) return;
        List<RealPlugin.NamedCallback> nCs = [];
        foreach (var cs in RealPlugin.Instance.callbacksByName.Values)
        foreach (var nc in cs)
            nCs.Add(nc);
        NewTable(["Id", "名称", "注册者", "注册时间"], nCs.ToArray(), [
            i => ImGui.Text(i.Id.ToString()),
            i => ImGui.Text(i.Name),
            i => ImGui.Text(i.Registrant),
            i => ImGui.Text(i.RegistrationTime.ToString()),
        ]);
    }

    internal static void DrawTriggerVarTextAuraSettings()
    {
        using var tab = ImRaii.TabItem("文本悬浮窗");
        if (!tab) return;
        NewTable(["悬浮窗名称", "名称", "文本"], RealPlugin._instance.textauras.ToArray(), [
            i => ImGui.Text(i.Key),
            i => ImGui.Text(i.Value.AuraName),
            i => ImGui.Text(i.Value.TextExpression.ToString()),
        ]);
    }

    internal static void Start(string cmd) => Process.Start(new ProcessStartInfo(cmd)
    {
        UseShellExecute = true
    });

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
