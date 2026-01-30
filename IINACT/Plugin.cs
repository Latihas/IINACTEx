using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Reflection;
using System.Runtime.Loader;
using Advanced_Combat_Tracker;
using Dalamud.Game.Command;
using Dalamud.Interface.ImGuiFileDialog;
using Dalamud.Interface.Windowing;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using IINACT.Latihas.Overlay;
using IINACT.Network;
using IINACT.TextToSpeech;
using IINACT.Windows;
using Machina.FFXIV;
using Machina.FFXIV.Headers.Opcodes;
using RainbowMage.OverlayPlugin;
using RainbowMage.OverlayPlugin.Handlers.Ipc;
using RainbowMage.OverlayPlugin.WebSocket;
using Triggernometry;
using Triggernometry.Core;
using Triggernometry.PluginBridges.BridgeNamazu;
using Triggernometry.PScript;
using TriggernometryProxy;
using static IINACT.Latihas.LWindow;

namespace IINACT;

// ReSharper disable once ClassNeverInstantiated.Global
[SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Local")]
public sealed class Plugin : IDalamudPlugin {
    public string Name => "IINACTEx";
    public Version Version { get; }

    private const string MainWindowCommandName = "/iinact";
    private const string EndEncCommandName = "/endenc";
    internal const string OverlayCommandName = "/iinactoverlay";
    public readonly WindowSystem WindowSystem = new("IINACT");
    [PluginService] public static IDalamudPluginInterface PluginInterface { get; private set; }
    [PluginService] public static ICommandManager CommandManager { get; private set; }
    [PluginService] public static IClientState ClientState { get; private set; }
    [PluginService] public static IDataManager DataManager { get; private set; }
    [PluginService] public static IChatGui ChatGui { get; private set; }
    [PluginService] public static IFramework Framework { get; private set; }
    [PluginService] public static ICondition Condition { get; private set; }
    [PluginService] public static IGameInteropProvider GameInteropProvider { get; private set; }
    [PluginService] public static ISigScanner SigScanner { get; private set; }
    [PluginService] public static INotificationManager NotificationManager { get; private set; }
    [PluginService] public static IPluginLog Log { get; private set; }
    [PluginService] public static ITargetManager TargetManager { get; private set; }
    [PluginService] public static IObjectTable ObjectTable { get; private set; }
    [PluginService] public static IGameGui GameGui { get; private set; }
    public static Configuration Configuration { get; private set; }
    internal static TextToSpeechProvider TextToSpeechProvider { get; private set; }
    private static MainWindow MainWindow = null!;
    internal static FileDialogManager FileDialogManager { get; private set; }
    private ZoneDownHookManager ZoneDownHookManager { get; }
    private IpcProviders IpcProviders { get; }

    public FfxivActPluginWrapper FfxivActPluginWrapper { get; set; }
    public PluginMain OverlayPlugin { get; set; }
    private ServerController? WebSocketServer { get; set; }
    internal string OverlayPluginStatus => OverlayPlugin.Status;
    public readonly ProxyPlugin TriggernometryProxyPlugin;
    public readonly PostNamazu.PostNamazu PostNamazuPlugin;
    public readonly dynamic? DalamudStartInfo;
    private PluginLogTraceListener PluginLogTraceListener { get; }
    private HttpClient HttpClient { get; }
    public readonly TriggerWindow TriggerWindow;
    public readonly FolderWindow FolderWindow;
    public readonly ActionWindow ActionWindow;
    public readonly ExportWindow ExportWindow;
    public readonly ImportWindow ImportWindow;
    public readonly RepoWindow RepoWindow;
    public readonly TriggernometryLogView TriggernometryLogView;
    public readonly ACTLogView ACTLogView;
    public readonly OverlayWindow OverlayWindow;

    internal static EdgeTTSWindow EdgeTTSWindow = null!;
    public static Plugin Instance;

    public string PluginAssemblyDirectory => PluginInterface.AssemblyLocation.Directory!.ToString();
    public string PluginConfigDirectory => PluginInterface.ConfigDirectory.ToString();
    public string PluginActScriptDirectory => Path.Combine(PluginConfigDirectory, "ActScript");
    public readonly (string, ushort, ushort)[] opcodestxtDiff = [];
    public readonly bool opcodestxtReplaced;
    public bool opcodestxtCanReplace => File.Exists(opcodestxtPath);
    public string opcodestxtPath => Path.Combine(PluginAssemblyDirectory, "opcodes.txt");
    public readonly bool opcodesjsoncReplaced;
    public bool opcodesjsoncCanReplace => File.Exists(opcodesjsoncPath);
    public string opcodesjsoncPath => Path.Combine(PluginAssemblyDirectory, "opcodes.jsonc");

    public static void UnzipWithoutPassword(string zipFilePath, string extractDir, bool overwrite = false) {
        try {
            if (Directory.Exists(extractDir))
                if (overwrite) Directory.Delete(extractDir, true);
                else return;
            ZipFile.ExtractToDirectory(zipFilePath, extractDir);
            File.Delete(zipFilePath);
        }
        catch (Exception ex) {
            Log.Warning($"解压失败：{ex.Message}");
        }
    }

    public Plugin() {
        Log.Warning("IINACTEx Start Init...");
        Instance = this;
        if (!Directory.Exists(PluginActScriptDirectory)) Directory.CreateDirectory(PluginActScriptDirectory);
        var region = DataManager.Language.ToString() == "ChineseSimplified" ? GameRegion.Chinese : GameRegion.Global;
        if (opcodestxtCanReplace) {
            try {
                var d1 = OpcodeManager.Instance._opcodes[region].ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
                var d2 = OpcodeManager.ConvertOpCode(File.ReadAllText(opcodestxtPath));
                OpcodeManager.Instance.SetRegion(region, d2);
                opcodestxtDiff = d2
                    .Where(kvp => !d1.ContainsKey(kvp.Key) || !Equals(d1[kvp.Key], kvp.Value))
                    .Select(kvp => (kvp.Key, d1.GetValueOrDefault(kvp.Key, (ushort)0), kvp.Value))
                    .ToArray();
                opcodestxtReplaced = true;
                Log.Warning("opcodestxt Replaced");
            }
            catch (Exception ex) {
                Log.Error($"opcodestxt Replace Failed: {ex}");
                OpcodeManager.Instance.SetRegion(region);
            }
        }
        else
            OpcodeManager.Instance.SetRegion(region);
        Version = Assembly.GetExecutingAssembly().GetName().Version!;
        FileDialogManager = new FileDialogManager();
        HttpClient = new HttpClient();
        var fetchDeps =
            new FetchDependencies.FetchDependencies(Version, PluginAssemblyDirectory,
                DataManager.Language.ToString() == "ChineseSimplified", HttpClient);
        fetchDeps.GetFfxivPlugin();
        Log.Warning("Depedencies Fetched");
        PluginLogTraceListener = new PluginLogTraceListener();
        Trace.Listeners.Add(PluginLogTraceListener);
        ActGlobals.Init();
        ActGlobals.oFormActMain = new FormActMain(Log);
        ActGlobals.oFormActMain.DalamudPlugin = this;
        Configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
        TextToSpeechProvider = new TextToSpeechProvider();
        Log.Warning("TTS Inited");
        ActGlobals.oFormActMain.LogFilePath = Configuration.LogFilePath;
        WindowSystem.AddWindow(MainWindow = new MainWindow());
        WindowSystem.AddWindow(OverlayWindow = new OverlayWindow());
        WindowSystem.AddWindow(EdgeTTSWindow = new EdgeTTSWindow(TextToSpeechProvider.GetEdgeTTSManager()!));
        WindowSystem.AddWindow(TriggerWindow = new TriggerWindow());
        WindowSystem.AddWindow(FolderWindow = new FolderWindow());
        WindowSystem.AddWindow(ActionWindow = new ActionWindow());
        WindowSystem.AddWindow(ExportWindow = new ExportWindow());
        WindowSystem.AddWindow(ImportWindow = new ImportWindow());
        WindowSystem.AddWindow(RepoWindow = new RepoWindow());
        WindowSystem.AddWindow(TriggernometryLogView = new TriggernometryLogView());
        WindowSystem.AddWindow(ACTLogView = new ACTLogView());
        Log.Warning("Windows Inited");
        IpcProviders = new IpcProviders(PluginInterface);
        var info = PluginInterface.GetType().Assembly.GetType("Dalamud.Service`1", true)!.MakeGenericType(PluginInterface.GetType().Assembly.GetType("Dalamud.Dalamud", true)!).GetMethod("Get")!
            .Invoke(null, BindingFlags.Default, null, [], null);
        DalamudStartInfo = info!.GetType().GetField("StartInfo", AllFlags)?.GetValue(info)
                           ?? info.GetType().GetProperty("StartInfo", AllFlags)?.GetValue(info);
        Log.Info(DalamudStartInfo?.ToString());
        Log.Warning("DalamudStartInfo Inited");
        FfxivActPluginWrapper = new FfxivActPluginWrapper();
        Log.Warning("FfxivActPlugin Inited");
        var container = new TinyIoCContainer();
        var logger = new Logger(Log);
        container.Register(logger);
        container.Register<ILogger>(logger);
        container.Register(HttpClient);
        container.Register(FileDialogManager);
        container.Register(PluginInterface);
        OverlayPlugin = new PluginMain(PluginAssemblyDirectory, logger, container);
        container.Register(OverlayPlugin);
        ActGlobals.oFormActMain.OverlayPluginContainer = container;
        opcodesjsoncReplaced = opcodesjsoncCanReplace;
        OverlayPlugin.InitPlugin(PluginConfigDirectory, opcodesjsoncCanReplace ? File.ReadAllText(opcodesjsoncPath) : null);
        if (opcodesjsoncReplaced) Log.Warning("opcodesjsonc Replaced");
        var registry = container.Resolve<Registry>();
        MainWindow.OverlayPresets = registry.OverlayPresets;
        MainWindow.Server = WebSocketServer = container.Resolve<ServerController>();
        IpcProviders.Server = WebSocketServer;
        IpcProviders.OverlayIpcHandler = container.Resolve<IpcHandlerController>();
        MainWindow.OverlayPluginConfig = container.Resolve<IPluginConfig>();
        OverlayWindow.Init(WebSocketServer);
        Log.Warning("OverlayPlugin Inited");
        ActGlobals.oFormActMain.TriggernometryPlugin = TriggernometryProxyPlugin = new ProxyPlugin();
        TriggernometryProxyPlugin.InitPlugin(this, PluginInterface, Log, ClientState, Framework, GameInteropProvider, ObjectTable, GameGui, SigScanner);
        RealPlugin.Instance.InitAura();
        ActGlobals.oFormActMain.PostNamazuPlugin = PostNamazuPlugin = new PostNamazu.PostNamazu();
        PostNamazuPlugin.InitPlugin(PluginInterface, Log, SigScanner);
        BridgeNamazu.InitializeModules();
        BridgeNamazu.RegisterAnnotatedMethods();
        Log.Warning("Triggernometry & PostNamazu Inited");
        CommandManager.AddHandler(MainWindowCommandName, new CommandInfo(OnCommand) {
            HelpMessage = "显示IINACT主窗口"
        });
        CommandManager.AddHandler(EndEncCommandName, new CommandInfo(OnCommand) {
            HelpMessage = "终止IINACT正在处理的战斗"
        });
        CommandManager.AddHandler(OverlayCommandName, new CommandInfo(OnCommand) {
            HelpMessage = "打开统计悬浮窗"
        });
        PluginInterface.UiBuilder.Draw += DrawUI;
        PluginInterface.UiBuilder.OpenConfigUi += DrawConfigUI;
        PluginInterface.UiBuilder.OpenMainUi += DrawConfigUI;
        if (ClientState.IsPvP)
            EnterPvP();
        else
            LeavePvP();
        ClientState.EnterPvP += EnterPvP;
        ClientState.LeavePvP += LeavePvP;
        ZoneDownHookManager = new ZoneDownHookManager();
        ActGlobals.oFormActMain.ActPlugins.Add(new ActPluginData("_FFXIV_ACT_Plugin", ActGlobals.oFormActMain.FfxivPlugin, false));
        ActGlobals.oFormActMain.ActPlugins.Add(new ActPluginData("_OverlayPlugin", new PluginLoader(OverlayPlugin), false));
        ActGlobals.oFormActMain.ActPlugins.Add(new ActPluginData("_Triggernometry", TriggernometryProxyPlugin, false));
        ActGlobals.oFormActMain.ActPlugins.Add(new ActPluginData("_PostNamazu", PostNamazuPlugin, false));
        foreach (var rt in Directory.GetFiles(PluginActScriptDirectory, "*.cs", SearchOption.TopDirectoryOnly).Select(Path.GetFileName).Cast<string>())
            if (Configuration.ActScriptsEnabled.Contains(rt))
                LoadPScript(rt, preserveEnableState: true);
        foreach (var rt in Directory.GetFiles(PluginActScriptDirectory, "*.dll", SearchOption.TopDirectoryOnly).Select(Path.GetFileName).Cast<string>())
            if (Configuration.ActScriptsEnabled.Contains(rt))
                LoadIActPluginV1(rt, preserveEnableState: true);
        Log.Warning("ACT Plugin Inited");
        if (Directory.Exists(Path.Combine(PluginConfigDirectory, "cactbot"))) RefreshBw();
        if (Configuration.ShowWindowOnInit) MainWindow.Toggle();
        if (Configuration.ShowOverlayOnInit) OverlayWindow.Toggle();
        if (Configuration.TtsOnInit) ActGlobals.oFormActMain.TTS("插件加载完成");
        Log.Warning("IINACTEx Inited");
    }

    public static void InitIActPluginV1(ActPluginData plugin, bool preserveEnableState = false) {
        try {
            Log.Warning($"正在加载IActPluginV1 {plugin.pluginFileName}");
            ActGlobals.oFormActMain.ActPlugins.Add(plugin);
            plugin.pluginObj.InitPlugin(plugin.tpPluginSpace, plugin.lblPluginStatus);
            if (!preserveEnableState) {
                Configuration.ActScriptsEnabled.Add(plugin.pluginFileName);
                Configuration.Save();
            }
        }
        catch (Exception e) {
            Log.Error(e.ToString());
        }
    }

    public static void DeInitIActPluginV1(ActPluginData plugin, bool preserveEnableState = false) {
        try {
            Log.Warning($"正在卸载IActPluginV1 {plugin.pluginFileName}");
            if (!preserveEnableState) {
                Configuration.ActScriptsEnabled.Remove(plugin.pluginFileName);
                Configuration.Save();
            }
            plugin.pluginObj.DeInitPlugin();
        }
        catch (Exception e) {
            Log.Error(e.ToString());
        }
        ActGlobals.oFormActMain.ActPlugins.Remove(plugin);
    }

    public static void LoadIActPluginV1(string name, string? dllPath = null, bool preserveEnableState = false) {
        try {
            Assembly asm;
            using (var memoryStream = new MemoryStream(File.ReadAllBytes(dllPath ?? Path.Combine(Instance.PluginActScriptDirectory, name)))) {
                asm = AssemblyLoadContext.GetLoadContext(Assembly.GetExecutingAssembly())!.LoadFromStream(memoryStream);
            }
            Log.Warning($"Loading IActPluginV1 {asm.FullName}");
            var scriptTypes = asm.GetTypes()
                .Where(type => type is { IsAbstract: false, IsInterface: false }
                               && typeof(IActPluginV1).IsAssignableFrom(type))
                .ToList();
            var isIScriptBase = scriptTypes.Any(type => typeof(IScriptBase).IsAssignableFrom(type));
            foreach (var type in scriptTypes)
                if (Activator.CreateInstance(type) is IActPluginV1 scriptInstance)
                    InitIActPluginV1(new ActPluginData(name, scriptInstance, isIScriptBase), preserveEnableState);
        }
        catch (Exception ex) {
            Log.Warning($"ActScript {name} 载入失败: {ex}");
        }
    }

    public static void LoadPScript(string name, bool preserveEnableState = false) {
        try {
            var rs = File.ReadAllText(Path.Combine(Instance.PluginActScriptDirectory, name));
            if (CSharpScriptCompiler.CompileScript(rs, false))
                LoadIActPluginV1(name, Path.Combine(Instance.PluginConfigDirectory, "Scripts", Path.GetFileName(CSharpScriptCompiler.GetScriptDllPath(rs))), preserveEnableState);
        }
        catch (Exception ex) {
            Log.Warning($"ActScript {name} 载入失败: {ex}");
        }
    }

    public const BindingFlags AllFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;

    public void Dispose() {
        Configuration.Save();
        TextToSpeechProvider.Dispose();
        ClientState.EnterPvP -= EnterPvP;
        ClientState.LeavePvP -= LeavePvP;
        IpcProviders.Dispose();
        ZoneDownHookManager.Dispose();
        Trace.Listeners.Remove(PluginLogTraceListener);
        WindowSystem.RemoveAllWindows();
        OverlayWindow.Dispose();
        CommandManager.RemoveHandler(MainWindowCommandName);
        CommandManager.RemoveHandler(EndEncCommandName);
        CommandManager.RemoveHandler(OverlayCommandName);
       
        ActGlobals.oFormActMain.ActPlugins.RemoveAt(0);
        while (ActGlobals.oFormActMain.ActPlugins.Count > 0)
            DeInitIActPluginV1(ActGlobals.oFormActMain.ActPlugins.Last(), true);
        FfxivActPluginWrapper.Dispose();
        ActGlobals.Dispose();
    }

    internal void RefreshBw() {
        PostNamazuPlugin.DoAction("command", "/bw overlay 时间轴 reload");
        PostNamazuPlugin.DoAction("command", "/bw overlay 设置 reload");
    }

    private void OnCommand(string command, string args) {
        if (command == OverlayCommandName) {
            OverlayWindow.IsOpen = true;
            return;
        }
        if (command == EndEncCommandName) {
            ActGlobals.oFormActMain.EndCombat(false);
            return;
        }

        switch (args) {
            case "start": //deprecated
            case "ws start":
                WebSocketServer?.Start();
                break;
            case "stop": //deprecated
            case "ws stop":
                WebSocketServer?.Stop();
                break;
            case "log start":
                Configuration.WriteLogFile = true;
                Configuration.Save();
                break;
            case "log stop":
                Configuration.WriteLogFile = false;
                Configuration.Save();
                break;
            case "log pvp start":
                Configuration.DisablePvp = false;
                Configuration.Save();
                break;
            case "log pvp stop":
                Configuration.DisablePvp = true;
                Configuration.Save();
                break;
            default:
                MainWindow.IsOpen = true;
                break;
        }
    }

    private void DrawUI() {
        WindowSystem.Draw();
        FileDialogManager.Draw();
    }

    public void DrawConfigUI() {
        MainWindow.IsOpen = true;
    }

    private void EnterPvP() {
        if (Configuration is not { DisablePvp: true, DisableWritingPvpLogFile: false })
            return;

        Configuration.DisableWritingPvpLogFile = true;
    }

    private void LeavePvP() {
        Configuration.DisableWritingPvpLogFile = false;
    }

    internal static void OpenEdgeTTSWindow() {
        EdgeTTSWindow.Show();
    }
}