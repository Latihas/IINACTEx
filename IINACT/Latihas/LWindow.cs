using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Numerics;
using System.Runtime.Loader;
using Advanced_Combat_Tracker;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.ImGuiNotification;
using Dalamud.Interface.Utility.Raii;
using Triggernometry;
using Triggernometry.Core;
using Triggernometry.Core.Serialization;
using Triggernometry.Core.Variables;
using Triggernometry.UI.CustomControls;

namespace IINACT.Latihas;

public static partial class LWindow
{
    internal static string WindowPrefix = "IINACTEx ";

    internal static void DrawTriggerSettings()
    {
        using var tab = ImRaii.TabItem("触发器");
        if (!tab) return;
        using var bar = ImRaii.TabBar("TriggerBar");
        if (!bar) return;
        DrawTriggerTriggerSettings();
        DrawTriggerVarSettings();
        DrawTriggerDebug();
    }

    internal static void DrawTriggerTriggerSettings()
    {
        using var tab = ImRaii.TabItem("触发器仓库");
        if (!tab) return;
        if (ImGui.Button("刷新触发器"))
        {
            UserInterface.BuildTriggerTreeFromConfiguration(null, null);
        }
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

    internal static void DrawTriggerDebug()
    {
        using var tab = ImRaii.TabItem("触发器调试");
        if (!tab) return;
        using var bar = ImRaii.TabBar("触发器调试Bar");
        if (!bar) return;
        DrawTriggerDebugLog();
        DrawTriggerDebugTrigger();
        DrawTriggerDebugEvalTest();
        DrawTriggerDebugInternalTest();
        DrawTriggerDebugCommonExpr();
    }

    internal static void DrawTriggerDebugLog()
    {
        using var tab = ImRaii.TabItem("日志");
        if (!tab) return;
        var DebugLevel = (int)RealPlugin.Instance.cfg.DebugLevel;
        if (ImGui.Combo("Trn输出调试日志等级", ref DebugLevel,
                        Enum.GetValues<RealPlugin.DebugLevelEnum>()
                            .Select(i => i.ToString()).ToList()))
            RealPlugin.Instance.cfg.DebugLevel = (RealPlugin.DebugLevelEnum)DebugLevel;
        var LogFlattenMaxCount = RealPlugin.Instance.cfg.LogFlattenMaxCount.ToString();
        if (ImGui.InputText("最大日志队列数量", ref LogFlattenMaxCount))
            RealPlugin.Instance.cfg.LogFlattenMaxCount = int.Parse(LogFlattenMaxCount);
        if (ImGui.Button("Trn日志")) Plugin.Instance.TriggernometryLogView.Toggle();
        ImGui.SameLine();
        if (ImGui.Button("ACT日志")) Plugin.Instance.ACTLogView.Toggle();
        ImGui.SameLine();
        if (ImGui.Button("清空日志队列")) RealPlugin.Instance.ClearLog();
    }

    internal static void DrawTriggerDebugInternalTest()
    {
        using var tab = ImRaii.TabItem("内置测试表达式");
        if (!tab) return;
        ImGui.Text("内置测试表达式");
        foreach (var t in Test1.Test().Concat(Test2.Test()))
        {
            var ts = t.ToString();
            if (t.IsCorrect is null or false)
            {
                if (ImGui.Button(ts))
                    ImGui.SetClipboardText(ts);
            }
            else ImGui.Text($"{t}");
        }
    }

    private static string Sbe = "", Sb = "";

    internal static void DrawTriggerDebugEvalTest()
    {
        using var tab = ImRaii.TabItem("评估");
        if (!tab) return;
        ImGui.SetNextItemWidth(-1);
        ImGui.InputTextMultiline("代码", ref TestCode, 1145141);
        if (ImGui.Button("编译代码"))
            Sbe = CSharpScriptCompiler.CompileScript(TestCode, true)
                      ? "成功"
                      : "失败，详情见/xllog";
        ImGui.SameLine();
        if (ImGui.Button("编译并运行代码(无反馈)"))
            RealPlugin._instance.scripting.Evaluate(TestCode, null, null);
        ImGui.SameLine();
        if (ImGui.Button("清空编译错误历史")) RealPlugin.Instance.cfg.CompileFailedScripts.Clear();
        ImGui.Separator();
        ImGui.InputText("## 表达式", ref TestExpression);
        ImGui.SameLine();
        if (ImGui.Button("评估表达式"))
            Sbe = TestContext.ExpandVariables(null, null, false, TestExpression);
        if (!string.IsNullOrEmpty(Sbe)) ImGui.Text(Sbe);
    }

    internal static void DrawTriggerDebugCommonExpr()
    {
        using var tab = ImRaii.TabItem("常用表达式");
        if (!tab) return;
        ImGui.Text("${_systemtime} = " + TestContext.ExpandVariables(null, null, false, "${_systemtime}"));
        ImGui.Text("${_systemtimems} = " + TestContext.ExpandVariables(null, null, false, "${_systemtimems}"));
        ImGui.Text("${_me}/${_ffxivplayer} = " + TestContext.ExpandVariables(null, null, false, "${_me}"));
        ImGui.Text("${_me.id} = " + TestContext.ExpandVariables(null, null, false, "${_me.id}"));
        ImGui.Text("${_ffxivzoneid} = " + TestContext.ExpandVariables(null, null, false, "${_ffxivzoneid}"));
        // ImGui.Text("${_ffxivpartyorder} = "+testContext.ExpandVariables(null, null, false, "${_ffxivpartyorder}"));
        ImGui.Text("${_ffxivprocid} = " + TestContext.ExpandVariables(null, null, false, "${_ffxivprocid}"));
        ImGui.Text("${_ffxivprocname} = " + TestContext.ExpandVariables(null, null, false, "${_ffxivprocname}"));
        ImGui.Text("${_ffxivversion} = " + TestContext.ExpandVariables(null, null, false, "${_ffxivversion}"));
        ImGui.Text("${_ffxivlanguage} = " + TestContext.ExpandVariables(null, null, false, "${_ffxivlanguage}"));
        ImGui.Text("${_ffxivlanguageid} = " + TestContext.ExpandVariables(null, null, false, "${_ffxivlanguageid}"));
        ImGui.Text("${_ffxivisglobal} = " + TestContext.ExpandVariables(null, null, false, "${_ffxivisglobal}"));
        ImGui.Text("${_ffxivincombat} = " + TestContext.ExpandVariables(null, null, false, "${_ffxivincombat}"));
        ImGui.Text("${_incombat} = " + TestContext.ExpandVariables(null, null, false, "${_incombat}"));
        ImGui.Text("${_duration} = " + TestContext.ExpandVariables(null, null, false, "${_duration}"));
        ImGui.Separator();
        ImGui.Text("目标信息");
        var targ = Plugin.TargetManager.Target;
        if (targ == null)
            if (Plugin.ClientState.LocalPlayer != null)
                targ = Plugin.ClientState.LocalPlayer;
        if (targ != null)
        {
            ImGui.Text("Target.Name: " + targ.Name);
            ImGui.Text("Target.Address: 0x" + targ.Address.ToString("X"));
            ImGui.Text("Target.BaseId: 0x" + targ.BaseId.ToString("X"));
            ImGui.Text("Target.EntityId: 0x" + targ.EntityId.ToString("X"));
            ImGui.Text("Target.GameObjectId: 0x" + targ.GameObjectId.ToString("X"));
            ImGui.Text("Target.OwnerId: 0x" + targ.OwnerId.ToString("X"));
        }
        ImGui.Separator();
        ImGui.Text("...蓝笔了不写了");
    }

    internal static void DrawTriggerDebugTrigger()
    {
        using var tab = ImRaii.TabItem("触发器验证");
        if (!tab) return;
        ImGui.InputText("触发器Id", ref TestTriggerId);
        ImGui.SameLine();
        if (ImGui.Button("验证触发器"))
        {
            Sb = $"总量: {RealPlugin.Instance.Triggers.Count}";
            foreach (var t in RealPlugin.Instance.Triggers)
                if (t.Id.ToString() == TestTriggerId)
                    Sb += "存活于Triggers.";
            foreach (var t in RealPlugin.Instance.ActiveTextTriggers)
                if (t.Id.ToString() == TestTriggerId)
                    Sb += "存活于ActiveTextTriggers.";
            foreach (var t in RealPlugin.Instance.ActiveACTTriggers)
                if (t.Id.ToString() == TestTriggerId)
                    Sb += "存活于ActiveACTTriggers.";
            foreach (var t in RealPlugin.Instance.ActiveEndpointTriggers)
                if (t.Id.ToString() == TestTriggerId)
                    Sb += "存活于ActiveEndpointTriggers.";
            foreach (var t in RealPlugin.Instance.ActiveFFXIVNetworkTriggers)
                if (t.Id.ToString() == TestTriggerId)
                    Sb += "存活于ActiveFFXIVNetworkTriggers.";
        }
    }

    internal static void DrawSettingsIINACT()
    {
        using var tab = ImRaii.TabItem("IINACT设置");
        if (!tab) return;
        var ShowWindowOnInit = Plugin.Configuration.ShowWindowOnInit;
        if (ImGui.Checkbox("启动时显示界面", ref ShowWindowOnInit))
        {
            Plugin.Configuration.ShowWindowOnInit = ShowWindowOnInit;
            Plugin.Configuration.Save();
        }
        var ShowOverlayOnInit = Plugin.Configuration.ShowOverlayOnInit;
        if (ImGui.Checkbox("启动时显示Overlay", ref ShowOverlayOnInit))
        {
            Plugin.Configuration.ShowOverlayOnInit = ShowOverlayOnInit;
            Plugin.Configuration.Save();
        }
        var TtsOnInit = Plugin.Configuration.TtsOnInit;
        if (ImGui.Checkbox("启动时TTS提示加载完成", ref TtsOnInit))
        {
            Plugin.Configuration.TtsOnInit = TtsOnInit;
            Plugin.Configuration.Save();
        }
    }

    internal static void DrawSettingsTrn()
    {
        using var tab = ImRaii.TabItem("Trn设置");
        if (!tab) return;
        var EnableModuleBase = RealPlugin.Instance.cfg.EnableModuleBase;
        if (ImGui.Checkbox("启用ModuleBase(极有可能炸游戏的功能，如绘图等。尤其是VfxModule，用于管理绘图极其容易爆炸，禁用掉可以大幅提升稳定性)", ref EnableModuleBase))
            RealPlugin.Instance.cfg.EnableModuleBase = EnableModuleBase;
        if (EnableModuleBase)
        {
            var modules = Triggernometry.PluginBridges.BridgeNamazu.BridgeNamazu.Modules.Concat(Triggernometry.PluginBridges.BridgeNamazu.BridgeNamazu.SideloadModules).Select(i => i.Key.Name.ToString()).ToArray();
            foreach (var name in modules)
            {
                ImGui.Indent();
                var cChecked = !RealPlugin.Instance.cfg.PostnamazuModuleDisabled.Contains(name);
                if (ImGui.Checkbox(name, ref cChecked))
                {
                    if (cChecked) RealPlugin.Instance.cfg.PostnamazuModuleDisabled.Remove(name);
                    else RealPlugin.Instance.cfg.PostnamazuModuleDisabled.Add(name);
                }
                ImGui.Unindent();
            }
        }
    }

    internal static void DrawSettingsPostnmz()
    {
        using var tab = ImRaii.TabItem("鲇鱼精设置");
        if (!tab) return;
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
        if (ImGui.Button("清空")) Plugin.Instance.PostNamazuPlugin.PluginUi.lstMessages.Items.Clear();
        var PostNamazuAutoStart = Plugin.Instance.PostNamazuPlugin.PluginUi.CheckAutoStart.Checked;
        if (ImGui.Checkbox("鲇鱼精监听自动启动", ref PostNamazuAutoStart))
            Plugin.Instance.PostNamazuPlugin.PluginUi.CheckAutoStart.Checked = PostNamazuAutoStart;
        ImGui.Text("启用功能");
        var iter = 1;
        foreach (var c in Plugin.Instance.PostNamazuPlugin.PluginUi.flowLayoutActions.Controls.OfType<CheckBox>())
        {
            var cChecked = c.Checked;
            if (ImGui.Checkbox(c.Text, ref cChecked))
            {
                c.Checked = cChecked;
                Plugin.Instance.PostNamazuPlugin.PluginUi.ActionEnabled[c.Text] = cChecked;
            }
            if (iter++ != Plugin.Instance.PostNamazuPlugin.PluginUi.flowLayoutActions.Controls.Count) ImGui.SameLine();
        }
        ImGui.Separator();
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

    internal static void DrawSettings()
    {
        using var tab = ImRaii.TabItem("设置");
        if (!tab) return;
        using var bar = ImRaii.TabBar("设置Bar");
        if (!bar) return;
        DrawSettingsIINACT();
        DrawSettingsTrn();
        DrawSettingsPostnmz();
        DrawSettingsScripts();
        DrawSettingsOpCodes();
    }

    internal static void DrawSettingsScripts()
    {
        using var tab = ImRaii.TabItem("脚本设置");
        if (!tab) return;
        ImGui.Text("脚本一览(待开发)");
        ImGui.Text(string.Join(",",ActGlobals.oFormActMain.ActPlugins.Select(x => x.pluginFileName)));
        ImGui.SameLine();
        if (ImGui.Button("打开脚本文件夹")) Start(Plugin.Instance.PluginActScriptDirectory);
        //TODO 检测重复 GetFileNameWithoutExtension
        NewTable(["名称", "状态", "操作"], Directory.GetFiles(Plugin.Instance.PluginActScriptDirectory, "*.cs", SearchOption.TopDirectoryOnly)
                                              .Concat(Directory.GetFiles(Plugin.Instance.PluginActScriptDirectory, "*.dll", SearchOption.TopDirectoryOnly))
                                              .Select(Path.GetFileName).Cast<string>().ToArray(), [
            i => ImGui.Text(i),
            i =>
            {
                if (i.StartsWith('_'))
                {
                    ImGui.Text("内置插件");
                    return;
                }
                var plugins = ActGlobals.oFormActMain.ActPlugins.Select(x => x.pluginFileName).Where(x => x == i).ToList();
                if (plugins.Count == 0) ImGui.Text("未载入");
                else ImGui.Text(ActGlobals.oFormActMain.ActPlugins.First(x => x.pluginFileName == i).cbEnabled.Enabled ? "已启用" : "已禁用");
            },
            i =>
            {
                if (i.StartsWith('_')) return;
                var plugins = ActGlobals.oFormActMain.ActPlugins.Select(x => x.pluginFileName).Where(x => x == i).ToList();
                if (plugins.Count == 0)
                {
                    if (ImGui.Button($"载入##{i}"))
                        if (i.EndsWith(".cs"))
                            Plugin.LoadPScript(i);
                        else 
                            Plugin.LoadIActPluginV1(i);
                }
                else
                {
                    var plugin = ActGlobals.oFormActMain.ActPlugins.First(x => x.pluginFileName == i);
                    if (ImGui.Button($"禁用##{i}"))
                        Plugin.DeInitIActPluginV1(plugin);
                    ImGui.SameLine();
                    if (ImGui.Button($"打开界面##{i}"))
                        plugin.PluginForm.Show();
                }
            }
        ]);
    }

    internal static void DrawSettingsOpCodes()
    {
        using var tab = ImRaii.TabItem("OpCodes设置");
        if (!tab) return;
        ImGui.PushStyleColor(ImGuiCol.Text, Color.LRed);
        ImGui.Text("警告: 该功能十分甚至九分危险，如果你不知道你在干什么请不要擅动。该功能未经充分验证。");
        ImGui.PopStyleColor(1);
        ImGui.Text("IINACTEx支持外部文件替换Opcode。");
        ImGui.Text("在插件安装目录下如果存在opcodes.txt，则会在加载插件的时候替换掉解析插件的OpCode。");
        ImGui.Text("在插件安装目录下如果存在opcodes.jsonc，则会在加载插件的时候替换掉Overlay插件的OpCode。");
        ImGui.Separator();
        ImGui.PushStyleColor(ImGuiCol.Text, Color.LPurple);
        ImGui.Text("当前状态:");
        ImGui.PopStyleColor(1);
        if (Plugin.Instance.opcodestxtReplaced)
        {
            ImGui.PushStyleColor(ImGuiCol.Text, Color.LRed);
            ImGui.Text("opcodes.txt已替换");
            ImGui.PopStyleColor(1);
            ImGui.SameLine();
            if (Plugin.Instance.opcodestxtCanReplace)
            {
                if (ImGui.Button("删除opcodes.txt")) File.Delete(Plugin.Instance.opcodestxtPath);
            }
            else ImGui.Text("(文件缺失，将在下次加载恢复内置)");
        }
        else if (Plugin.Instance.opcodestxtCanReplace)
        {
            ImGui.PushStyleColor(ImGuiCol.Text, Color.LYellow);
            ImGui.Text("opcodes.txt将在下次加载插件时替换");
            ImGui.PopStyleColor(1);
            ImGui.SameLine();
            if (ImGui.Button("删除opcodes.txt")) File.Delete(Plugin.Instance.opcodestxtPath);
        }
        else ImGui.Text("opcodes.txt为内置版本");
        if (Plugin.Instance.opcodesjsoncReplaced)
        {
            ImGui.PushStyleColor(ImGuiCol.Text, Color.LRed);
            ImGui.Text("opcodes.jsonc已替换");
            ImGui.PopStyleColor(1);
            ImGui.SameLine();
            if (Plugin.Instance.opcodesjsoncCanReplace)
            {
                if (ImGui.Button("删除opcodes.jsonc")) File.Delete(Plugin.Instance.opcodesjsoncPath);
            }
            else ImGui.Text("(文件缺失，将在下次加载恢复内置)");
        }
        else if (Plugin.Instance.opcodesjsoncCanReplace)
        {
            ImGui.PushStyleColor(ImGuiCol.Text, Color.LYellow);
            ImGui.Text("opcodes.jsonc将在下次加载插件时替换");
            ImGui.PopStyleColor(1);
            ImGui.SameLine();
            if (ImGui.Button("删除opcodes.jsonc")) File.Delete(Plugin.Instance.opcodesjsoncPath);
        }
        else ImGui.Text("opcodes.jsonc为内置版本");
        ImGui.Separator();
        ImGui.Text("这里可以从呆萌源获取两个文件");
        if (FileDownloaderOpcodestxt == null)
        {
            if (ImGui.Button("自动下载生成opcodes.txt"))
            {
                var dp = Path.Combine(Plugin.Instance.PluginAssemblyDirectory, "chinese.zip");
                FileDownloaderOpcodestxt = new FileDownloader("https://cdn.diemoe.net/files/ACT.DieMoe/Packs/FFXIV_ACT_Plugin/chinese.zip", dp, () =>
                {
                    FileDownloaderOpcodestxt = null;
                    try
                    {
                        var exp = Path.Combine(Plugin.Instance.PluginAssemblyDirectory, "chinese");
                        Plugin.UnzipWithoutPassword(dp, exp, true);
                        var alc = new AssemblyLoadContext(null, isCollectible: true);
                        var dllAssembly = alc.LoadFromAssemblyPath(Path.Combine(exp, "FFXIV_ACT_Plugin.dll"));
                        var dllp = Path.Combine(exp, "machina.ffxiv.dll");
                        using (var stream = dllAssembly.GetManifestResourceStream("costura.machina.ffxiv.dll.compressed")!)
                        using (var destination = new FileStream(dllp, FileMode.Create))
                        using (var deflateStream = new DeflateStream(stream, CompressionMode.Decompress))
                            deflateStream.CopyTo(destination);
                        dllAssembly = alc.LoadFromAssemblyPath(dllp);
                        var outputPath = Path.Combine(Plugin.Instance.PluginAssemblyDirectory, "opcodes.txt");
                        using (var resourceStream2 = dllAssembly.GetManifestResourceStream("Machina.FFXIV.Headers.Opcodes.Chinese.txt")!)
                        using (var fileStream2 = new FileStream(outputPath, FileMode.Create, FileAccess.Write))
                            resourceStream2.CopyTo(fileStream2);
                    }
                    catch (Exception e)
                    {
                        Plugin.Log.Error(e.ToString());
                    }
                });
                FileDownloaderOpcodestxt.DownloadFileAsync();
            }
        }
        else ImGui.Text("处理中");

        ImGui.SameLine();
        if (FileDownloaderOpcodesjsonc == null)
        {
            if (ImGui.Button("自动下载生成opcodes.jsonc"))
            {
                FileDownloaderOpcodesjsonc = new FileDownloader("https://assets.diemoe.net/OverlayPlugin/OverlayPlugin.Core/resources/opcodes.jsonc", Path.Combine(Plugin.Instance.PluginAssemblyDirectory, "opcodes.jsonc"),
                                                                () => { FileDownloaderOpcodesjsonc = null; });
                FileDownloaderOpcodesjsonc.DownloadFileAsync();
            }
        }
        else ImGui.Text("处理中");
    }

    private static byte[] DecompressGzip(byte[] compressedData)
    {
        using var compressedStream = new MemoryStream(compressedData);
        using var gzipStream = new GZipStream(compressedStream, CompressionMode.Decompress);
        using var resultStream = new MemoryStream();
        gzipStream.CopyTo(resultStream);
        return resultStream.ToArray();
    }

    private static FileDownloader? FileDownloaderOpcodestxt, FileDownloaderOpcodesjsonc;

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

    private static void NewTable<T>(string[] header, T[]? data, Action<T>[] acts, Func<T, Vector4>? setColor = null)
    {
        if (data is null || data.Length == 0) return;
        if (ImGui.BeginTable("Table", acts.Length, ImGuiTableFlag))
        {
            foreach (var item in header) ImGui.TableSetupColumn(item, ImGuiTableColumnFlags.WidthStretch);
            ImGui.TableHeadersRow();
            foreach (var res in data)
            {
                ImGui.TableNextRow();
                if (setColor != null)
                {
                    var color = setColor(res);
                    ImGui.PushStyleColor(ImGuiCol.TableRowBg, color);
                    ImGui.PushStyleColor(ImGuiCol.TableRowBgAlt, color);
                }
                for (var i = 0; i < acts.Length; i++)
                {
                    ImGui.TableSetColumnIndex(i);
                    acts[i](res);
                }
            }
            if (setColor != null) ImGui.PopStyleColor(2 * data.Length);
            ImGui.EndTable();
        }
    }


    private const ImGuiTableFlags ImGuiTableFlag = ImGuiTableFlags.Borders | ImGuiTableFlags.Resizable | ImGuiTableFlags.RowBg;
}
