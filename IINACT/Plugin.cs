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
using IINACT.TextToSpeech;
using IINACT.Network;
using IINACT.Windows;
using Machina.FFXIV;
using Machina.FFXIV.Headers.Opcodes;
using Triggernometry;
using TriggernometryProxy;

namespace IINACT;

// ReSharper disable once ClassNeverInstantiated.Global
[SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Local")]
public sealed class Plugin : IDalamudPlugin
{
    public string Name => "IINACT";
    public Version Version { get; }

    private const string MainWindowCommandName = "/iinact";
    private const string EndEncCommandName = "/endenc";
    public readonly WindowSystem WindowSystem = new("IINACT");
    [PluginService]
    public static IDalamudPluginInterface PluginInterface { get; private set; }
    [PluginService]
    internal static ICommandManager CommandManager { get; private set; }
    [PluginService]
    internal static IClientState ClientState { get; private set; }
    [PluginService]
    internal static IDataManager DataManager { get; private set; }
    [PluginService]
    internal static IChatGui ChatGui { get; private set; }
    [PluginService]
    internal static IFramework Framework { get; private set; }
    [PluginService]
    internal static ICondition Condition { get; private set; }
    [PluginService]
    internal static IGameInteropProvider GameInteropProvider { get; private set; }
    [PluginService]
    internal static ISigScanner SigScanner { get; private set; }
    [PluginService]
    internal static INotificationManager NotificationManager { get; private set; }
    [PluginService]
    public static IPluginLog Log { get; private set; }
    public Configuration GetConfiguration => Configuration;
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

    internal static EdgeTTSWindow EdgeTTSWindow = null!;
    public static Plugin Instance;
    public static dynamic? LatihasTts;

    public static void UnzipWithoutPassword(string zipFilePath, string extractDir, bool overwrite = false)
    {
        try
        {
            if (Directory.Exists(extractDir))
                if (overwrite) Directory.Delete(extractDir, recursive: true);
                else return;
            ZipFile.ExtractToDirectory(zipFilePath, extractDir);
        }
        catch (Exception ex)
        {
            Log.Error($"解压失败：{ex.Message}");
        }
    }

    public Plugin()
    {
        Instance = this;
        OpcodeManager.Instance.SetRegion(DataManager.Language.ToString() == "ChineseSimplified"
                                             ? GameRegion.Chinese
                                             : GameRegion.Global);
        var createZoneDownHookManager = Task.Run(()
                                                     => new ZoneDownHookManager(NotificationManager, GameInteropProvider));
        Version = Assembly.GetExecutingAssembly().GetName().Version!;
        FileDialogManager = new FileDialogManager();
        HttpClient = new HttpClient();
        var fetchDeps =
            new FetchDependencies.FetchDependencies(Version, PluginInterface.AssemblyLocation.Directory!.FullName,
                                                    DataManager.Language.ToString() == "ChineseSimplified", HttpClient);
        fetchDeps.GetFfxivPlugin();
        PluginLogTraceListener = new PluginLogTraceListener();
        Trace.Listeners.Add(PluginLogTraceListener);
        Advanced_Combat_Tracker.ActGlobals.Init();
        Advanced_Combat_Tracker.ActGlobals.oFormActMain = new Advanced_Combat_Tracker.FormActMain(Log);
        Configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
        Configuration.Initialize(PluginInterface);
        UnzipWithoutPassword(Path.Combine(PluginInterface.AssemblyLocation.Directory.ToString(), "cactbot.zip"),
                             Path.Combine(PluginInterface.AssemblyLocation.Directory.ToString(), "cactbot"));
        //TTS
        try
        {
            var text = PluginInterface.AssemblyLocation.Directory.ToString();
            var assetsdir = text + "/TtsAssets/";
            Assembly latihasTtsAssembly;
            using (var memoryStream = new MemoryStream(File.ReadAllBytes(assetsdir + "System.Numerics.Tensors.dll")))
            {
                AssemblyLoadContext.GetLoadContext(Assembly.GetExecutingAssembly())!.LoadFromStream(memoryStream);
            }
            using (var memoryStream = new MemoryStream(File.ReadAllBytes(assetsdir + "Microsoft.ML.OnnxRuntime.dll")))
            {
                AssemblyLoadContext.GetLoadContext(Assembly.GetExecutingAssembly())!.LoadFromStream(memoryStream);
            }
            using (var memoryStream = new MemoryStream(File.ReadAllBytes(assetsdir + "LatihasTTS.dll")))
            {
                latihasTtsAssembly = AssemblyLoadContext.GetLoadContext(Assembly.GetExecutingAssembly())!.LoadFromStream(memoryStream);
            }
            LatihasTts = Activator.CreateInstance(latihasTtsAssembly.GetType("LatihasTTS.LatihasTts")!)!;
            LatihasTts.Init(text, Log);
        }
        catch (Exception e)
        {
            Log.Warning(e.ToString());
        }
        TextToSpeechProvider = new TextToSpeechProvider(Log, PluginInterface.ConfigFile.FullName);
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
                                      .Invoke(null, BindingFlags.Default, null, Array.Empty<object>(), null);
            DalamudStartInfo = info!.GetType().GetField("StartInfo", AllFlags)?.GetValue(info)
                               ?? info.GetType().GetProperty("StartInfo", AllFlags)?.GetValue(info);
            Log.Info(DalamudStartInfo?.ToString());
        }
        catch (Exception e)
        {
            Log.Error($"{e.Message}\n{e.StackTrace ?? ""}");
        }
        Advanced_Combat_Tracker.ActGlobals.oFormActMain.TriggernometryPlugin = TriggernometryProxyPlugin = new ProxyPlugin();
        TriggernometryProxyPlugin.InitPlugin(this, PluginInterface, Log, ClientState, Framework);
        Advanced_Combat_Tracker.ActGlobals.oFormActMain.PostNamazuPlugin = PostNamazuPlugin = new PostNamazu.PostNamazu();
        PostNamazuPlugin.InitPlugin(PluginInterface, Log, SigScanner);
        IpcProviders = new IpcProviders(PluginInterface);
        LWindow.WindowPrefix = "IINACT ";
        WindowSystem.AddWindow(MainWindow = new MainWindow());
        WindowSystem.AddWindow(TriggerWindow = new LWindow.TriggerWindow());
        WindowSystem.AddWindow(FolderWindow = new LWindow.FolderWindow());
        WindowSystem.AddWindow(ActionWindow = new LWindow.ActionWindow());
        WindowSystem.AddWindow(ExportWindow = new LWindow.ExportWindow());
        WindowSystem.AddWindow(ImportWindow = new LWindow.ImportWindow());
        WindowSystem.AddWindow(RepoWindow = new LWindow.RepoWindow());

        CommandManager.AddHandler(MainWindowCommandName, new CommandInfo(OnCommand)
        {
            HelpMessage = "显示IINACT主窗口"
        });

        CommandManager.AddHandler(EndEncCommandName, new CommandInfo(OnCommand)
        {
            HelpMessage = "终止IINACT正在处理的战斗"
        });

        PluginInterface.UiBuilder.Draw += DrawUI;
        PluginInterface.UiBuilder.OpenConfigUi += DrawConfigUI;
        if (ClientState.IsPvP)
            EnterPvP();
        else
            LeavePvP();

        ClientState.EnterPvP += EnterPvP;
        ClientState.LeavePvP += LeavePvP;

        ZoneDownHookManager = createZoneDownHookManager.Result;
        Advanced_Combat_Tracker.ActGlobals.oFormActMain.TTS("插件加载完成");
        MainWindow.Toggle();
        RealPlugin.plug.InitAura(Framework);
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
        CommandManager.RemoveHandler(MainWindowCommandName);
        CommandManager.RemoveHandler(EndEncCommandName);
        PostNamazuPlugin.DeInitPlugin();
        TriggernometryProxyPlugin.DeInitPlugin();
        OverlayPlugin.DeInitPlugin();
        PostNamazuPlugin.DeInitPlugin();
        RealPlugin.plug.DeInitAura(Framework);
        Advanced_Combat_Tracker.ActGlobals.Dispose();
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
            PluginInterface.AssemblyLocation.Directory!.FullName, logger, container);
        container.Register(overlayPlugin);
        Advanced_Combat_Tracker.ActGlobals.oFormActMain.OverlayPluginContainer = container;

        Task.Run(() =>
        {
            overlayPlugin.InitPlugin(PluginInterface.ConfigDirectory.FullName);

            var registry = container.Resolve<RainbowMage.OverlayPlugin.Registry>();
            MainWindow.OverlayPresets = registry.OverlayTemplates;
            WebSocketServer = container.Resolve<RainbowMage.OverlayPlugin.WebSocket.ServerController>();
            MainWindow.Server = WebSocketServer;
            IpcProviders.Server = WebSocketServer;
            IpcProviders.OverlayIpcHandler = container.Resolve<RainbowMage.OverlayPlugin.Handlers.Ipc.IpcHandlerController>();
            MainWindow.OverlayPluginConfig = container.Resolve<RainbowMage.OverlayPlugin.IPluginConfig>();
            Triggernometry.PluginBridges.BridgeNamazu.BridgeNamazu.InitializeModules();
            Triggernometry.PluginBridges.BridgeNamazu.BridgeNamazu.RegisterAnnotatedMethods();
            PostNamazuPlugin.DoAction("command","/bw overlay 伤害统计 reload");
            PostNamazuPlugin.DoAction("command","/bw overlay 时间轴 reload");
            PostNamazuPlugin.DoAction("command","/bw overlay 设置 reload");
        });
        return overlayPlugin;
    }

    private void OnCommand(string command, string args)
    {
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
