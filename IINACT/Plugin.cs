using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Loader;
using Dalamud.Game;
using Dalamud.Game.Command;
using Dalamud.Interface.ImGuiFileDialog;
using Dalamud.Interface.Windowing;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using IINACT.Latihas;
using IINACT.Latihas.Overlay;
using IINACT.TextToSpeech;
using IINACT.Network;
using IINACT.Windows;
using Machina.FFXIV;
using Machina.FFXIV.Headers.Opcodes;
using Triggernometry;
using Triggernometry.Core;


namespace IINACT;

// ReSharper disable once ClassNeverInstantiated.Global
[SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Local")]
public sealed class Plugin : IDalamudPlugin
{
    public string Name => "IINACT";
    public Version Version { get; }

    private const string MainWindowCommandName = "/iinact";
    private const string EndEncCommandName = "/endenc";
    internal const string OverlayCommandName = "/iinactoverlay";
    public readonly WindowSystem WindowSystem = new("IINACT");
    [PluginService]
    public static IDalamudPluginInterface PluginInterface { get; private set; }
    [PluginService]
    public static ICommandManager CommandManager { get; private set; }
    [PluginService]
    public static IClientState ClientState { get; private set; }
    [PluginService]
    public static IDataManager DataManager { get; private set; }
    [PluginService]
    public static IChatGui ChatGui { get; private set; }
    [PluginService]
    public static IFramework Framework { get; private set; }
    [PluginService]
    public static ICondition Condition { get; private set; }
    [PluginService]
    public static IGameInteropProvider GameInteropProvider { get; private set; }
    [PluginService]
    public static ISigScanner SigScanner { get; private set; }
    [PluginService]
    public static INotificationManager NotificationManager { get; private set; }
    [PluginService]
    public static IPluginLog Log { get; private set; }
    [PluginService]
    public static IGameGui GameGui { get; private set; }
    [PluginService]
    public static ITargetManager TargetManager  { get; private set; }
    // public Configuration GetConfiguration => Configuration;
    internal static Configuration Configuration { get; private set; }
    internal static TextToSpeechProvider TextToSpeechProvider { get; private set; }
    private static MainWindow MainWindow = null!;
    internal static FileDialogManager FileDialogManager { get; private set; }
    private ZoneDownHookManager ZoneDownHookManager { get; }
    private IpcProviders IpcProviders { get; }

    private FfxivActPluginWrapper FfxivActPluginWrapper { get; }
    public RainbowMage.OverlayPlugin.PluginMain OverlayPlugin { get; set; }
    private RainbowMage.OverlayPlugin.WebSocket.ServerController? WebSocketServer { get; set; }
    internal string OverlayPluginStatus => OverlayPlugin.Status;
    public ProxyPlugin TriggernometryProxyPlugin;
    public PostNamazu.PostNamazu PostNamazuPlugin;
    public dynamic? DalamudStartInfo;
    private PluginLogTraceListener PluginLogTraceListener { get; }
    private HttpClient HttpClient { get; }

    public LWindow.TriggerWindow TriggerWindow = null!;
    public LWindow.FolderWindow FolderWindow = null!;
    public LWindow.ActionWindow ActionWindow = null!;
    public LWindow.ExportWindow ExportWindow = null!;
    public LWindow.ImportWindow ImportWindow = null!;
    public LWindow.RepoWindow RepoWindow = null!;
    public LWindow.TriggernometryLogView TriggernometryLogView = null!;
    public LWindow.ACTLogView ACTLogView = null!;
    public OverlayWindow OverlayWindow = null!;

    internal static EdgeTTSWindow EdgeTTSWindow = null!;
    public static Plugin Instance;
    public static dynamic? LatihasTts;
    public string PluginAssemblyDirectory => PluginInterface.AssemblyLocation.Directory!.ToString();
    public string PluginConfigDirectory => PluginInterface.ConfigDirectory.ToString();
    public string PluginPScriptDirectory =>Path.Combine(PluginConfigDirectory,"PScript");

    public static void UnzipWithoutPassword(string zipFilePath, string extractDir, bool overwrite = false)
    {
        try
        {
            if (Directory.Exists(extractDir))
                if (overwrite) Directory.Delete(extractDir, recursive: true);
                else return;
            ZipFile.ExtractToDirectory(zipFilePath, extractDir);
            File.Delete(zipFilePath);
        }
        catch (Exception ex)
        {
            Log.Warning($"解压失败：{ex.Message}");
        }
    }

    public Plugin()
    {
        Instance = this;
        if (!Directory.Exists(PluginPScriptDirectory)) Directory.CreateDirectory(PluginPScriptDirectory);
        OpcodeManager.Instance.SetRegion(DataManager.Language.ToString() == "ChineseSimplified"
                                             ? GameRegion.Chinese
                                             : GameRegion.Global);
        var createZoneDownHookManager = Task.Run(()
                                                     => new ZoneDownHookManager(NotificationManager, GameInteropProvider));
        Version = Assembly.GetExecutingAssembly().GetName().Version!;
        FileDialogManager = new FileDialogManager();
        HttpClient = new HttpClient();
        var fetchDeps =
            new FetchDependencies.FetchDependencies(Version, PluginAssemblyDirectory,
                                                    DataManager.Language.ToString() == "ChineseSimplified", HttpClient);
        fetchDeps.GetFfxivPlugin();
        PluginLogTraceListener = new PluginLogTraceListener();
        Trace.Listeners.Add(PluginLogTraceListener);
        Advanced_Combat_Tracker.ActGlobals.Init();
        Advanced_Combat_Tracker.ActGlobals.oFormActMain = new Advanced_Combat_Tracker.FormActMain(Log);
        Configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
        Configuration.Initialize(PluginInterface);
        //TTS
        try
        {
            var tmpdir = Path.Combine(PluginConfigDirectory, "tmp");
            var text = PluginConfigDirectory;
            var assetsdir = Path.Combine(text, "TtsAssets");
            Assembly latihasTtsAssembly;
            using (var memoryStream = new MemoryStream(File.ReadAllBytes(Path.Combine(assetsdir, "System.Numerics.Tensors.dll"))))
                AssemblyLoadContext.GetLoadContext(Assembly.GetExecutingAssembly())!.LoadFromStream(memoryStream);
            using (var memoryStream = new MemoryStream(File.ReadAllBytes(Path.Combine(assetsdir, "Microsoft.ML.OnnxRuntime.dll"))))
                AssemblyLoadContext.GetLoadContext(Assembly.GetExecutingAssembly())!.LoadFromStream(memoryStream);
            using (var memoryStream = new MemoryStream(File.ReadAllBytes(Path.Combine(assetsdir, "LatihasTTS.dll"))))
                latihasTtsAssembly = AssemblyLoadContext.GetLoadContext(Assembly.GetExecutingAssembly())!.LoadFromStream(memoryStream);
            LatihasTts = Activator.CreateInstance(latihasTtsAssembly.GetType("LatihasTTS.LatihasTts")!)!;
            LatihasTts.Init(assetsdir, tmpdir, Log);
            if (!LatihasTts.CheckAssets()) Log.Warning("LatihasTts Assets Lost");
        }
        catch (Exception ex)
        {
            Log.Warning(ex.ToString());
        }
        TextToSpeechProvider = new TextToSpeechProvider(Log,PluginConfigDirectory);
        TextToSpeechProvider.SetUseEdgeTTS(Configuration.UseEdgeTTS);
        TextToSpeechProvider.SetUseLatihasTTS(Configuration.UseLatihasTts);
        EdgeTTSWindow = new EdgeTTSWindow(TextToSpeechProvider.GetEdgeTTSManager()!);
        WindowSystem.AddWindow(EdgeTTSWindow);
        Advanced_Combat_Tracker.ActGlobals.oFormActMain.LogFilePath = Configuration.LogFilePath;
        FfxivActPluginWrapper = new FfxivActPluginWrapper(Configuration, DataManager.Language, ChatGui, Framework, Condition);
        OverlayPlugin = InitOverlayPluginTrn();
        //Ecommons
        try
        {
            var info = PluginInterface.GetType().Assembly.GetType("Dalamud.Service`1", true).MakeGenericType(PluginInterface.GetType().Assembly.GetType("Dalamud.Dalamud", true)).GetMethod("Get")
                                      .Invoke(null, BindingFlags.Default, null, [], null);
            DalamudStartInfo = info!.GetType().GetField("StartInfo", AllFlags)?.GetValue(info)
                               ?? info.GetType().GetProperty("StartInfo", AllFlags)?.GetValue(info);
            Log.Info(DalamudStartInfo?.ToString());
        }
        catch (Exception e)
        {
            Log.Error($"{e.Message}\n{e.StackTrace ?? ""}");
        }
        Advanced_Combat_Tracker.ActGlobals.oFormActMain.TriggernometryPlugin = TriggernometryProxyPlugin = new ProxyPlugin();
        TriggernometryProxyPlugin.InitPlugin(this, PluginInterface, Log, ClientState, Framework, GameInteropProvider);
        Advanced_Combat_Tracker.ActGlobals.oFormActMain.PostNamazuPlugin = PostNamazuPlugin = new PostNamazu.PostNamazu();
        PostNamazuPlugin.InitPlugin(PluginInterface, Log, SigScanner);
        IpcProviders = new IpcProviders(PluginInterface);
        LWindow.WindowPrefix = "IINACTEx ";
        WindowSystem.AddWindow(MainWindow = new MainWindow());
        WindowSystem.AddWindow(TriggerWindow = new LWindow.TriggerWindow());
        WindowSystem.AddWindow(FolderWindow = new LWindow.FolderWindow());
        WindowSystem.AddWindow(ActionWindow = new LWindow.ActionWindow());
        WindowSystem.AddWindow(ExportWindow = new LWindow.ExportWindow());
        WindowSystem.AddWindow(ImportWindow = new LWindow.ImportWindow());
        WindowSystem.AddWindow(RepoWindow = new LWindow.RepoWindow());
        WindowSystem.AddWindow(TriggernometryLogView = new LWindow.TriggernometryLogView());
        WindowSystem.AddWindow(ACTLogView = new LWindow.ACTLogView());
        WindowSystem.AddWindow(OverlayWindow = new OverlayWindow());

        CommandManager.AddHandler(MainWindowCommandName, new CommandInfo(OnCommand)
        {
            HelpMessage = "显示IINACT主窗口"
        });

        CommandManager.AddHandler(EndEncCommandName, new CommandInfo(OnCommand)
        {
            HelpMessage = "终止IINACT正在处理的战斗"
        });
        CommandManager.AddHandler(OverlayCommandName, new CommandInfo(OnCommand)
        {
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

        ZoneDownHookManager = createZoneDownHookManager.Result;
        Advanced_Combat_Tracker.ActGlobals.oFormActMain.TTS("插件加载完成");
        if (Configuration.ShowWindowOnInit) MainWindow.Toggle();
        if (Configuration.ShowOverlayOnInit) OverlayWindow.Toggle();
        RealPlugin.Instance.InitAura();
    }

    public const BindingFlags AllFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;

    public void Dispose()
    {
        LatihasTts?.Dispose();
        ClientState.EnterPvP -= EnterPvP;
        ClientState.LeavePvP -= LeavePvP;
        IpcProviders.Dispose();
        ZoneDownHookManager.Dispose();

        FfxivActPluginWrapper.Dispose();
        OverlayPlugin.DeInitPlugin();
        Trace.Listeners.Remove(PluginLogTraceListener);
        WindowSystem.RemoveAllWindows();
        MainWindow.Dispose();
        OverlayWindow.Dispose();
        CommandManager.RemoveHandler(MainWindowCommandName);
        CommandManager.RemoveHandler(EndEncCommandName);
        CommandManager.RemoveHandler(OverlayCommandName);
        RealPlugin.Instance.DeInitAura();
        PostNamazuPlugin.DeInitPlugin();
        TriggernometryProxyPlugin.DeInitPlugin();
        OverlayPlugin.DeInitPlugin();
        PostNamazuPlugin.DeInitPlugin();

        Advanced_Combat_Tracker.ActGlobals.Dispose();
    }

    internal void RefreshBw()
    {
        PostNamazuPlugin.DoAction("command", "/bw overlay 时间轴 reload");
        PostNamazuPlugin.DoAction("command", "/bw overlay 设置 reload");
    }

    private RainbowMage.OverlayPlugin.PluginMain InitOverlayPluginTrn()
    {
        var container = new RainbowMage.OverlayPlugin.TinyIoCContainer();

        var logger = new RainbowMage.OverlayPlugin.Logger(Log);
        container.Register(logger);
        container.Register<RainbowMage.OverlayPlugin.ILogger>(logger);

        container.Register(HttpClient);
        container.Register(FileDialogManager);
        container.Register(PluginInterface);

        var overlayPlugin = new RainbowMage.OverlayPlugin.PluginMain(
            PluginAssemblyDirectory, logger, container);
        container.Register(overlayPlugin);
        Advanced_Combat_Tracker.ActGlobals.oFormActMain.OverlayPluginContainer = container;

        Task.Run(() =>
        {
            overlayPlugin.InitPlugin(PluginConfigDirectory);

            var registry = container.Resolve<RainbowMage.OverlayPlugin.Registry>();
            MainWindow.OverlayPresets = registry.OverlayTemplates;
            WebSocketServer = container.Resolve<RainbowMage.OverlayPlugin.WebSocket.ServerController>();
            MainWindow.Server = WebSocketServer;
            OverlayWindow.Init(WebSocketServer);
            IpcProviders.Server = WebSocketServer;
            IpcProviders.OverlayIpcHandler = container.Resolve<RainbowMage.OverlayPlugin.Handlers.Ipc.IpcHandlerController>();
            MainWindow.OverlayPluginConfig = container.Resolve<RainbowMage.OverlayPlugin.IPluginConfig>();
            Triggernometry.PluginBridges.BridgeNamazu.BridgeNamazu.InitializeModules();
            Triggernometry.PluginBridges.BridgeNamazu.BridgeNamazu.RegisterAnnotatedMethods();
            if (Directory.Exists(Path.Combine(PluginAssemblyDirectory, "cactbot")) ||
                Directory.Exists(Path.Combine(PluginConfigDirectory, "cactbot")))
                RefreshBw();
        });
        return overlayPlugin;
    }

    private void OnCommand(string command, string args)
    {
        if (command == OverlayCommandName)
        {
            OverlayWindow.IsOpen = true;
            return;
        }
        if (command == EndEncCommandName)
        {
            Advanced_Combat_Tracker.ActGlobals.oFormActMain.EndCombat(false);
            return;
        }

        switch (args)
        {
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

    private void DrawUI()
    {
        WindowSystem.Draw();
        FileDialogManager.Draw();
    }

    public void DrawConfigUI()
    {
        MainWindow.IsOpen = true;
    }

    private void EnterPvP()
    {
        if (Configuration is not { DisablePvp: true, DisableWritingPvpLogFile: false })
            return;

        Configuration.DisableWritingPvpLogFile = true;
    }

    private void LeavePvP()
    {
        Configuration.DisableWritingPvpLogFile = false;
    }

    internal static void OpenEdgeTTSWindow()
    {
        EdgeTTSWindow?.Show();
    }
}
