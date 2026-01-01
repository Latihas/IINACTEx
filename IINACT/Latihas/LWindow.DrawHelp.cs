using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility.Raii;
using Triggernometry;
using Triggernometry.Core;
using Triggernometry.Expressions.Tests;


namespace IINACT.Latihas;

public static partial class LWindow
{
    private static string TestTriggerId = "", TestCode = "", TestExpression = "", TestTts = "";
    private static readonly Context TestContext = new(null);

    internal static void DrawHelpSettings()
    {
        using var tab = ImRaii.TabItem("帮助");
        if (!tab) return;
        ImGui.PushStyleColor(ImGuiCol.Text, Color.LRed);
        ImGui.Text("重要提醒！！！请一定要先看完介绍再使用，本插件仍然不是很稳定，有炸游戏风险");
        ImGui.Text("重要提醒！！！请一定要先看完介绍再使用，本插件仍然不是很稳定，有炸游戏风险");
        ImGui.Text("重要提醒！！！请一定要先看完介绍再使用，本插件仍然不是很稳定，有炸游戏风险");
        ImGui.Text("其实最会炸游戏的是VfxModule，设置-Trn设置-启用ModuleBase-取消VfxModule勾选即可禁用");
        ImGui.PopStyleColor(1);
        if (ImGui.CollapsingHeader("更新日志", ImGuiTreeNodeFlags.DefaultOpen))
        {
            ImGui.Text("多了一个已知问题bug。");
        }
        var s = ImGui.CollapsingHeader("已知问题(没定位到问题所在，可以提Pr之类的协助我修复。)", ImGuiTreeNodeFlags.DefaultOpen);
        if (s)
        {
            ImGui.Text("插件加载的第一次有时会加载失败，类似于下面，涉及解析插件的初始化，重新加载即可。彻底解决需要重启XIVLauncher。");
            ImGui.Text("这个问题有时候在更新的时候(也会卸载)触发，且再次载入大概率会炸游戏，请注意。");
            ImGui.Indent();
            if (s && ImGui.CollapsingHeader("展开堆栈信息"))
            {
                ImGui.Text("11:28:52.526 | 警告 | [IINACTEx] DalamudStartInfo Inited");
                ImGui.Text("11:28:52.527 | 错误 | [LOCALPLUGIN] 加载 \"IINACTEx\" 时出错，绑定并调用插件构造函数失败");
                ImGui.Text("	System.AggregateException: Failed to create IINACT.Plugin (ctor invocation) (Exception has been thrown by the target of an invocation.)");
                ImGui.Text("	 ---> System.Reflection.TargetInvocationException: Exception has been thrown by the target of an invocation.");
                ImGui.Text("	 ---> System.TypeInitializationException: The type initializer for '<Module>' threw an exception.");
                ImGui.Text("	 ---> System.NullReferenceException: Object reference not set to an instance of an object.");
                ImGui.Text("	   at .cctor()");
                ImGui.Text("	   --- End of inner exception stack trace ---");
                ImGui.Text("	   at IINACT.FfxivActPluginWrapper..ctor()");
                ImGui.Text("	   at IINACT.FfxivActPluginWrapper..ctor()");
                ImGui.Text("	   at IINACT.Plugin..ctor()");
                ImGui.Text("	   at System.RuntimeMethodHandle.InvokeMethod(ObjectHandleOnStack target, Void** arguments, ObjectHandleOnStack sig, BOOL isConstructor, ObjectHandleOnStack result)");
                ImGui.Text("	   at System.Reflection.MethodBaseInvoker.InvokeConstructorWithoutAlloc(Object obj, Boolean wrapInTargetInvocationException)");
                ImGui.Text("	   --- End of inner exception stack trace ---");
                ImGui.Text("	   at System.Reflection.MethodBaseInvoker.InvokeConstructorWithoutAlloc(Object obj, Boolean wrapInTargetInvocationException)");
                ImGui.Text("	   at System.Reflection.MethodBase.Invoke(Object obj, Object[] parameters)");
                ImGui.Text("	   at Dalamud.IoC.Internal.ServiceContainer.<>c__DisplayClass10_0.<CreateAsync>b__0() in /_/Dalamud/IoC/Internal/ServiceContainer.cs:line 120");
                ImGui.Text("	   at System.Threading.Tasks.Task`1.InnerInvoke()");
                ImGui.Text("	   at System.Threading.ExecutionContext.RunInternal(ExecutionContext executionContext, ContextCallback callback, Object state)");
                ImGui.Text("	--- End of stack trace from previous location ---");
                ImGui.Text("	   at System.Threading.ExecutionContext.RunInternal(ExecutionContext executionContext, ContextCallback callback, Object state)");
                ImGui.Text("	   at System.Threading.Tasks.Task.ExecuteWithThreadLocal(Task& currentTaskSlot, Thread threadPoolThread)");
                ImGui.Text("	--- End of stack trace from previous location ---");
                ImGui.Text("	   at Dalamud.IoC.Internal.ServiceContainer.CreateAsync(Type objectType, ObjectInstanceVisibility allowedVisibility, Object[] scopedObjects, IServiceScope scope) in /_/Dalamud/IoC/Internal/ServiceContainer.cs:line 119");
                ImGui.Text("	   --- End of inner exception stack trace ---");
                ImGui.Text("	   at Dalamud.IoC.Internal.ServiceContainer.CreateAsync(Type objectType, ObjectInstanceVisibility allowedVisibility, Object[] scopedObjects, IServiceScope scope) in /_/Dalamud/IoC/Internal/ServiceContainer.cs:line 129");
                ImGui.Text("	   at Dalamud.Plugin.Internal.Types.LocalPlugin.<>c__DisplayClass78_0.<<CreatePluginInstance>g__Create|0>d.MoveNext() in /_/Dalamud/Plugin/Internal/Types/LocalPlugin.cs:line 601");
                ImGui.Text("	--- End of stack trace from previous location ---");
                ImGui.Text(
                    "	   at Dalamud.Plugin.Internal.Types.LocalPlugin.CreatePluginInstance(LocalPluginManifest manifest, IServiceScope scope, Type type, DalamudPluginInterface dalamudInterface) in /_/Dalamud/Plugin/Internal/Types/LocalPlugin.cs:line 599");
                ImGui.Text("	   at Dalamud.Plugin.Internal.Types.LocalPlugin.LoadAsync(PluginLoadReason reason, Boolean reloading) in /_/Dalamud/Plugin/Internal/Types/LocalPlugin.cs:line 399");
            }
            ImGui.Unindent();
        }
        if (ImGui.CollapsingHeader("TODO", ImGuiTreeNodeFlags.DefaultOpen))
        {
            ImGui.Text("触发器生成器");
            ImGui.Text("触发器搜索器");
            ImGui.Text("完善脚本");
            ImGui.Text("VfxModule修复");
        }
        if (ImGui.CollapsingHeader("项目介绍"))
        {
            ImGui.Text("修改IINACT的初衷旨在尽可能满足日常对ACT的基本需求，替代ACT，假装自己是西瓜玩。");
            ImGui.Text("本项目仍然处于野蛮开发期，代码管理极其混乱，暗藏神秘bug，仅作开发测试使用。");
            ImGui.Text("该插件会在一个类ACT的环境下运行FFXIV_ACT_Plugin与大量修改的Overlay Plugin以适配现代.NET。与此同时，也添加了大量修改的Triggernometry与Postnamazu。包括类ACT、Triggernometry、Postnamazu在内，这些并非完整的代码移植，并且仍在开发完善中，可能缺少部分原版的函数，开发时请注意。");
            ImGui.Text("本分支基于国服IINACT开发，如需移植到国际服理论上仅需修改IINACT官库与CN库的区别即可(可能有部分翻译由于开发时未注意多语言需要适配一下)。相关参考文献如下：");
            ImGui.Text("    IINACT官方: https://github.com/marzent/IINACT");
            ImGui.Text("    IINACT CN: https://github.com/MeowZWR/IINACT");
            ImGui.Text("    Machina: https://github.com/MeowZWR/machina");
            ImGui.Text("    Edge TTS: https://github.com/AtmoOmen/EdgeTTS");
            ImGui.Text("    Triggernometry: https://github.com/MnFeN/Triggernometry");
            ImGui.Text("    PostNamazu: https://github.com/Natsukage/PostNamazu");
            ImGui.Text("=====悬浮窗=====");
            ImGui.Text("开发者偏好使用Browsingway，但是IINACT原版的URL生成器指向的在线页面可能不是最新版，建议指向本地ACT的目录");
            ImGui.Text("比如开发者的ACT装在D盘，那么URL就类似于file:///D:/ACT.DieMoe/Plugins/ACT.OverlayPlugin/cactbot/ui/raidboss/raidboss.html?timeline=1&alerts=1");
            ImGui.Text("有时候bw会不显示东西（如时间轴），需要手动在bw里面刷新一下");
            ImGui.Text("更多详情请见·运行状态·栏");
            ImGui.Text("=====TTS/Cactbot=====");
            ImGui.Text("IINACT CN默认使用了EdgeTTS，如果你EdgeTTS工作正常可以跳过这部分。由于开发者比较喜欢用LatihasTTS(纯本地模型推理)所以也做了接口。由于文件过大放不上Github所以需要联系开发者获取。(其实也没那么必须，只是开发者用着舒服)");
            ImGui.Text("cactbot资源可在·运行状态·栏找到链接并下载。");
            ImGui.Text("仅需将相关文件放入插件安装目录下即可使用，加号代表添加的文件，像这样:");
            ImGui.Text("Cactbot:");
            ImGui.Text("$(pluginConfigs/IINACTEx)");
            ImGui.Text("    Scripts/");
            ImGui.Text("    TriggernometryRepoBackups/");
            ImGui.Text("    PostNamazu.config.xml");
            ImGui.Text("    Triggernometry.config.xml");
            ImGui.Text("    + cactbot.zip");
            ImGui.Text("    ...");
            ImGui.Text("LatihasTTS:");
            ImGui.Text("$(installedPlugins/IINACTEx)");
            ImGui.Text("    Advanced Combat Tracker.dll");
            ImGui.Text("    Triggernometry.dll");
            ImGui.Text("    + TtsAssets/");
            ImGui.Text("        + pinyin.txt");
            ImGui.Text("        + symbol.txt");
            ImGui.Text("        + vocab.txt");
            ImGui.Text("        + a.ort");
            ImGui.Text("        + v.ort");
            ImGui.Text("        + ...");
            ImGui.Text("    ...");
            ImGui.Text("目前支持cactbot.zip一键下载安装，比较依赖网络环境。手动的话资源放在插件Config目录，cactbot可以选择手动解压，也可以在插件下次加载时自动解压。");
        }
        if (ImGui.CollapsingHeader("已知限制"))
        {
            ImGui.Text("Act原版插件支持非常有限(复杂的几乎都不支持)");
            ImGui.Text("!!! Triggernometry有时会因为宝宝椅的鲇鱼精扩展功能炸游戏/显示异常/...。开发者用Penumbra可以恢复部分图形问题");
            ImGui.SameLine();
            if (ImGui.Button("/penumbra redraw")) RealPlugin.Instance.InvokeNamedCallback("command", "/penumbra redraw");
            ImGui.Text("触发器有时候声音比较小，减小游戏音量以调整。");
            ImGui.Text("Triggernometry的配置文件完全兼容ACT版本，可以直接把act的配置文件复制到插件配置目录下");
            ImGui.Text("Triggernometry的触发器导入窗口不支持导入文件，且有长度限制，过大的触发器建议右键从剪切板导入。");
            ImGui.Text("Triggernometry保存配置逻辑与原版一致，即每5分钟或是卸载时，所以游戏崩溃可能会丢失配置。请导入或修改过任何触发器/配置/...后点击保存配置按钮手动保存。");
            ImGui.Text("Triggernometry的部分高级内存操作与回调不可用");
            ImGui.Text("Triggernometry的重复触发器导入重命名可能有问题，请尽量不要二次导入相同的触发器");
            ImGui.Text("Triggernometry的部分编辑还没写");
            ImGui.Text("Triggernometry的绝大部分Form或Control因兼容性被移除，可能误伤配置弹出框，一般报错中可以看出来。");
            ImGui.Text("Triggernometry的脚本执行引用库如下，若超出引用库不可编译：");
            ImGui.Text("    //SystemReferenes");
            foreach (var asm in CSharpScriptCompiler.SystemReferenes) ImGui.Text("    " + asm);
            ImGui.Text("    //SystemTypeReferenes");
            foreach (var asm in CSharpScriptCompiler.SystemTypeReferenes) ImGui.Text("    " + asm);
            ImGui.Text("    //PluginDirReferenes");
            foreach (var asm in CSharpScriptCompiler.PluginDirReferenes) ImGui.Text("    " + asm);
            ImGui.Text("    //DalamudDirReferenes");
            foreach (var asm in CSharpScriptCompiler.DalamudDirReferenes) ImGui.Text("    " + asm);
        }
        if (ImGui.CollapsingHeader("常见问题"))
        {
            ImGui.Text("问题太多了。如果出现bug，试着关开一下插件，说不定就自己会好了。");
            ImGui.Text("当然也可能是我懒得写了，你也可以帮助我完善这一部分。");
            ImGui.Text("其他问题可在Github仓库提issue解决。");
        }
    }

    private static readonly BasicTest Test1 = new();
    private static readonly FunctionTest Test2 = new();

    internal static void DrawTestSettings()
    {
        using var tab = ImRaii.TabItem("测试");
        if (!tab) return;
        if (ImGui.Button("打开插件目录"))
            Start(Plugin.Instance.PluginAssemblyDirectory);
        ImGui.SameLine();
        if (ImGui.Button("打开配置目录"))
            Start(Plugin.Instance.PluginConfigDirectory);
        ImGui.SameLine();
        if (ImGui.Button("打开Log目录"))
            Start(Plugin.Configuration.LogFilePath);
        if (ImGui.Button("打开卫月Log"))
            RealPlugin._instance.InvokeNamedCallback("command", "/xllog");
        ImGui.SameLine();
        if (ImGui.Button("刷新Bw悬浮窗"))
            Plugin.Instance.RefreshBw();
        ImGui.SameLine();
        if (ImGui.Button("打开bw设置"))
            RealPlugin._instance.InvokeNamedCallback("command", "/bw config");
        if (ImGui.Button("打开伤害统计悬浮窗"))
            Plugin.Instance.OverlayWindow.IsOpen = true;
        ImGui.Separator();
        ImGui.InputText("## 测试TTS", ref TestTts);
        ImGui.SameLine();
        if (ImGui.Button("测试TTS")) Advanced_Combat_Tracker.ActGlobals.oFormActMain.TTS(TestTts);
    }
}
