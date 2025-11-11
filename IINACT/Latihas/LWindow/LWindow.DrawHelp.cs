using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility.Raii;
using Triggernometry;

namespace IINACT.Latihas;

public static partial class LWindow
{
    private static string TestTriggerId = "", TestCode = "", TestExpression = "", TestTts = "", Sb = "";
    private static readonly Context TestContext = new();

    internal static void DrawHelpSettings()
    {
        using var tab = ImRaii.TabItem("帮助");
        if (!tab) return;
        ImGui.Text("重要提醒！！！请一定要先看完介绍再使用，本插件仍然不是很稳定，有炸游戏风险");
        ImGui.Text("重要提醒！！！请一定要先看完介绍再使用，本插件仍然不是很稳定，有炸游戏风险");
        ImGui.Text("重要提醒！！！请一定要先看完介绍再使用，本插件仍然不是很稳定，有炸游戏风险");
        ImGui.Text("=====项目介绍=====");
        ImGui.Text("修改IINACT的初衷旨在尽可能满足日常对ACT的基本需求，替代ACT，假装自己是西瓜玩。");
        ImGui.Text("本项目仍然处于野蛮开发期，代码管理极其混乱，暗藏神秘bug，仅作开发测试使用。");
        ImGui.Text("该插件会在一个类ACT的环境下运行FFXIV_ACT_Plugin与大量修改的Overlay Plugin以适配现代.NET。与此同时，也添加了大量修改的Triggernometry与Postnamazu。包括类ACT、Triggernometry、Postnamazu在内，这些并非完整的代码移植，并且仍在开发完善中，可能缺少部分原版的函数，开发时请注意。");
        ImGui.Text("本分支基于国服IINACT开发，如需移植到国际服理论上仅需修改IINACT官库与CN库的区别即可(可能有部分翻译由于开发时未注意多语言需要适配一下)。相关参考文献如下：");
        ImGui.Text("    IINACT官方: https://github.com/marzent/IINACT");
        ImGui.Text("    IINACT CN: https://github.com/MeowZWR/IINACT");
        ImGui.Text("    Machina: https://github.com/MeowZWR/machina/");
        ImGui.Text("    Edge TTS: https://github.com/AtmoOmen/EdgeTTS");
        ImGui.Text("    Triggernometry: https://github.com/MnFeN/Triggernometry");
        ImGui.Text("    PostNamazu: https://github.com/Natsukage/PostNamazu");
        ImGui.Text("=====悬浮窗=====");
        ImGui.Text("开发者偏好使用Browsingway，但是IINACT原版的URL生成器指向的在线页面可能不是最新版，建议指向本地ACT的目录");
        ImGui.Text("比如开发者的ACT装在D盘，那么URL就类似于file:///D:/ACT.DieMoe/Plugins/ACT.OverlayPlugin/cactbot/ui/raidboss/raidboss.html?timeline=1&alerts=1");
        ImGui.Text("有时候bw会不显示东西（如时间轴），需要手动在bw里面刷新一下");
        ImGui.Text("=====TTS=====");
        ImGui.Text("IINACT CN默认使用了EdgeTTS，如果你EdgeTTS工作正常可以跳过这部分。由于开发者比较喜欢用LatihasTTS(纯本地模型推理)所以也做了接口");
        ImGui.Text("仅需将TTS相关文件放入插件安装目录下即可使用，加号代表添加的文件，像这样:");
        ImGui.Text("$(installedPlugins/IINACTEx)");
        ImGui.Text("    Scripts/");
        ImGui.Text("    TriggernometryRepoBackups/");
        ImGui.Text("    + TtsAssets/");
        ImGui.Text("        + pinyin.txt");
        ImGui.Text("        + symbol.txt");
        ImGui.Text("        + vocab.txt");
        ImGui.Text("        + a.ort");
        ImGui.Text("        + v.ort");
        ImGui.Text("    Advanced Combat Tracker.dll");
        ImGui.Text("    Triggernometry.dll");
        ImGui.Text("    xxx.dll");
        ImGui.Text("    ...");
        ImGui.Text("=====已知限制=====");
        ImGui.Text("!!! 不要在插件加载后立刻卸载插件，否则大概率会线程回收失败，只能重启游戏解决。");
        ImGui.Text("!!! Triggernometry有时会因为宝宝椅的鲇鱼精扩展功能炸游戏/显示异常/...。目前已知场景是本里掉线上线会炸渲染，开发者用Penumbra可以恢复");
        ImGui.SameLine();
        if (ImGui.Button("/penumbra redraw")) RealPlugin.plug.InvokeNamedCallback("command", "/penumbra redraw");
        ImGui.Text("IActPluginV1仅为不报错存在，不会执行加载等逻辑");
        ImGui.Text("Postnamezu的Preset不可用");
        ImGui.Text("触发器有时候声音比较小，减小游戏音量以调整。");
        ImGui.Text("Triggernometry的配置文件与ACT版本完全兼容，可以直接把act的配置文件复制到插件config目录下");
        ImGui.Text("Triggernometry的触发器不支持导入文件，过大的触发器建议分批导入");
        ImGui.Text("Triggernometry不会自动保存配置文件。请导入或修改过任何触发器/配置/...后手动保存。");
        ImGui.Text("Triggernometry的部分高级内存操作与回调不可用");
        ImGui.Text("Triggernometry的悬浮窗不可用");
        ImGui.Text("Triggernometry的部分编辑还没写");
        ImGui.Text("Triggernometry的绝大部分Form或Control因兼容性被移除，可能误伤配置弹出框，一般报错中可以看出来，可以提Issue。");
        ImGui.Text("Triggernometry的脚本执行引用库如下，若超出引用库不可编译：");
        ImGui.Text("    Path.Combine(pluginPathRoot,'Triggernometry.dll')");
        ImGui.Text("    Path.Combine(pluginPathRoot,'AdvancedCombatTracker.dll')");
        ImGui.Text("    Path.Combine(pluginPathRoot,'PostNamazu.dll')");
        ImGui.Text("    Path.Combine(dalamudPathRoot,'Dalamud.dll')");
        ImGui.Text("    Path.Combine(dalamudPathRoot,'Dalamud.Bindings.ImGui.dll')");
        ImGui.Text("    Path.Combine(dalamudPathRoot,'InteropGenerator.Runtime.dll')");
        ImGui.Text("    Path.Combine(dalamudPathRoot,'ImGuiScene.dll')");
        ImGui.Text("    Path.Combine(dalamudPathRoot,'Lumina.dll')");
        ImGui.Text("    Path.Combine(dalamudPathRoot,'Lumina.Excel.dll')");
        ImGui.Text("    Path.Combine(dalamudPathRoot,'FFXIVClientStructs.dll')");
        ImGui.Text("    Path.Combine(dalamudPathRoot,'Newtonsoft.Json.dll')");
        ImGui.Text("    System.Windows.Forms.Form, System.Windows.Forms");
        foreach (var asm in CSharpScriptCompiler.SystemReferenes)
            ImGui.Text("    " + asm);
        ImGui.Text("=====常见问题=====");
        ImGui.Text("问题太多了。如果出现bug，试着关开一下插件，说不定就自己会好了。");
    }

    internal static void DrawTestSettings()
    {
        using var tab = ImRaii.TabItem("测试");
        if (!tab) return;
        if (ImGui.Button("打开插件目录"))
            Start(Plugin.PluginInterface.AssemblyLocation.Directory!.FullName);
        ImGui.SameLine();
        if (ImGui.Button("打开配置目录"))
            Start(Plugin.PluginInterface.ConfigDirectory.FullName);
        ImGui.SameLine();
        if (ImGui.Button("打开Log目录"))
            Start(Plugin.Configuration.LogFilePath);
        ImGui.Separator();
        ImGui.InputText("## 测试TTS", ref TestTts);
        ImGui.SameLine();
        if (ImGui.Button("测试TTS")) Advanced_Combat_Tracker.ActGlobals.oFormActMain.TTS(TestTts);
        ImGui.Separator();
        ImGui.InputText("触发器Id", ref TestTriggerId);
        ImGui.SameLine();
        if (ImGui.Button("验证触发器"))
        {
            Sb = $"总量: {RealPlugin.plug.Triggers.Count}";
            foreach (var t in RealPlugin.plug.Triggers)
                if (t.Id.ToString() == TestTriggerId)
                    Sb += "存活于Triggers.";
            foreach (var t in RealPlugin.plug.ActiveTextTriggers)
                if (t.Id.ToString() == TestTriggerId)
                    Sb += "存活于ActiveTextTriggers.";
            foreach (var t in RealPlugin.plug.ActiveACTTriggers)
                if (t.Id.ToString() == TestTriggerId)
                    Sb += "存活于ActiveACTTriggers.";
            foreach (var t in RealPlugin.plug.ActiveEndpointTriggers)
                if (t.Id.ToString() == TestTriggerId)
                    Sb += "存活于ActiveEndpointTriggers.";
            foreach (var t in RealPlugin.plug.ActiveFFXIVNetworkTriggers)
                if (t.Id.ToString() == TestTriggerId)
                    Sb += "存活于ActiveFFXIVNetworkTriggers.";
        }
        ImGui.Separator();
        ImGui.SetNextItemWidth(-1);
        ImGui.InputTextMultiline("代码", ref TestCode, 1145141, new Vector2(400, 300));
        if (ImGui.Button("编译代码"))
            Sb = CSharpScriptCompiler.CompileScript(TestCode, [])
                     ? "成功"
                     : "失败，详情见/xllog";
        ImGui.Separator();
        ImGui.SetNextItemWidth(-1);
        ImGui.SameLine();
        ImGui.InputText("表达式", ref TestExpression);
        if (ImGui.Button("评估表达式"))
            Sb = TestContext.ExpandVariables(null, null, false, TestCode);
        ImGui.Separator();
        ImGui.Text(Sb);
        ImGui.Separator();
        ImGui.Text("常用表达式");
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
        ImGui.Text("...蓝笔了不写了");
    }
}
