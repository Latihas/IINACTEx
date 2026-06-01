using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Runtime.Loader;
using System.Threading;
using System.Threading.Tasks;
using Advanced_Combat_Tracker;
using Dalamud.Bindings.ImGui;
using Dalamud.Common;
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
using Triggernometry.PluginBridges.BridgeNamazu;
using Triggernometry.PScript;
using TriggernometryProxy;
using static Advanced_Combat_Tracker.ActGlobals;
using static IINACT.Latihas.LWindow;
using static IINACT.Windows.MainWindow;
using static Utils.Interfaces;

namespace IINACT;

[SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Local")]
[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed class Plugin : I_IINACTEx_Plugin, IDalamudPlugin {
	public readonly Version Version;
	[PluginService] protected override IDalamudPluginInterface pluginInterface { get; set; }
	internal static IDalamudPluginInterface PluginInterface => Instance.pluginInterface;
	[PluginService] protected override ICommandManager commandManager { get; set; }
	internal static ICommandManager CommandManager => Instance.commandManager;
	[PluginService] protected override IClientState clientState { get; set; }
	internal static IClientState ClientState => Instance.clientState;
	[PluginService] protected override IDataManager dataManager { get; set; }
	internal static IDataManager DataManager => Instance.dataManager;
	[PluginService] protected override IChatGui chatGui { get; set; }
	internal static IChatGui ChatGui => Instance.chatGui;
	[PluginService] protected override IFramework framework { get; set; }
	internal static IFramework Framework => Instance.framework;
	[PluginService] protected override ICondition condition { get; set; }
	internal static ICondition Condition => Instance.condition;
	[PluginService] protected override IGameInteropProvider gameInteropProvider { get; set; }
	internal static IGameInteropProvider GameInteropProvider => Instance.gameInteropProvider;
	[PluginService] protected override ISigScanner sigScanner { get; set; }
	internal static ISigScanner SigScanner => Instance.sigScanner;
	[PluginService] protected override INotificationManager notificationManager { get; set; }
	internal static INotificationManager NotificationManager => Instance.notificationManager;
	[PluginService] protected override IPluginLog log { get; set; }
	internal static IPluginLog Log => Instance.log;
	[PluginService] protected override ITargetManager targetManager { get; set; }
	internal static ITargetManager TargetManager => Instance.targetManager;
	[PluginService] protected override IObjectTable objectTable { get; set; }
	internal static IObjectTable ObjectTable => Instance.objectTable;
	[PluginService] protected override IGameGui gameGui { get; set; }
	internal static IGameGui GameGui => Instance.gameGui;
	[PluginService] protected override ITextureProvider textureProvider { get; set; }
	internal static ITextureProvider TextureProvider => Instance.textureProvider;

	public readonly WindowSystem WindowSystem = new("IINACT");

	public static Configuration Configuration { get; private set; }
	[SuppressMessage("Performance", "CA1822")]
	[SuppressMessage("ReSharper", "UnusedMember.Global")]
	public Configuration ConfigurationInstance => Configuration;

	internal static TextToSpeechProvider TextToSpeechProvider { get; private set; }
	internal static MainWindow MainWindow = null!;
	internal static FileDialogManager FileDialogManager { get; private set; }
	private ZoneDownHookManager ZoneDownHookManager { get; set; }
	private IpcProviders IpcProviders { get; }

	public FfxivActPluginWrapper FfxivActPluginWrapper { get; set; }
	public PluginMain OverlayPlugin { get; set; }
	private ServerController? WebSocketServer { get; set; }
	internal string OverlayPluginStatus => OverlayPlugin.Status;
	public readonly ProxyPlugin TriggernometryProxyPlugin;
	public readonly PostNamazu.PostNamazu PostNamazuPlugin;

	// private PluginLogTraceListener PluginLogTraceListener { get; }
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
	private static DateTime lastLogTick = DateTime.Now;
	private readonly DateTime startLogTick = DateTime.Now;
	internal static EdgeTTSWindow EdgeTTSWindow = null!;
	public static Plugin Instance;
	private const int LatestConfigVersion = 3;
	private readonly FetchDependencies.FetchDependencies _fetchDependencies;
	// ReSharper disable once MemberCanBePrivate.Global
	public readonly TinyIoCContainer Container;
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
	public string cactbotDir => Path.Combine(Instance.PluginConfigDirectory, "cactbot");


	public static void UnzipWithoutPassword(string zipFilePath, string extractDir, bool overwrite = false) {
		try {
			if (Directory.Exists(extractDir))
				if (overwrite) Directory.Delete(extractDir, true);
				else return;
			ZipFile.ExtractToDirectory(zipFilePath, extractDir);
			File.Delete(zipFilePath);
		} catch (Exception ex) {
			Log.Warning($"解压失败：{ex.Message}");
		}
	}

	internal static void LogTick(string s) {
		var sec = (DateTime.Now - lastLogTick).TotalSeconds;
		var str = $"[StartTick] {s}({sec}s)";
		if (sec < 0.5) Log.Info(str);
		else Log.Warning(str);
		lastLogTick = DateTime.Now;
	}

	public Plugin() {
		Instance = this;
		LogTick("Start Initializing");
		Version = Version.Parse(Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>()!.InformationalVersion.Split('+')[0]);
		Init();
		oFormActMain = new FormActMain(this, Log, Framework, ObjectTable);
		Configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
		HttpClient = new HttpClient();
		if (Configuration.Version != LatestConfigVersion)
			Directory.GetFiles(PluginAssemblyDirectory, "FFXIV_ACT_Plugin*.dll").ToList().ForEach(File.Delete);
		_fetchDependencies = new FetchDependencies.FetchDependencies(Version, PluginAssemblyDirectory, DataManager.Language.ToString() == "ChineseSimplified", 5, HttpClient, Log);
		_fetchDependencies.GetFfxivPluginIfNullOrUpdate(Configuration.FFXIV_ACT_Plugin_CN_Update);
		Configuration.FFXIV_ACT_Plugin_CN_Update = false;
		LogTick("Dependencies Fetched");
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
			} catch (Exception ex) {
				Log.Error($"opcodestxt Replace Failed: {ex}");
				OpcodeManager.Instance.SetRegion(region);
			}
		} else
			OpcodeManager.Instance.SetRegion(region);
		FileDialogManager = new FileDialogManager();
		ActLocalization.Init();
		ActLocalization.AddPrebuild();
		oFormActMain.LogFilePath = Configuration.LogFilePath;
		TextToSpeechProvider = new TextToSpeechProvider();
		var info = PluginInterface.GetType().Assembly.GetType("Dalamud.Service`1", true)!.MakeGenericType(PluginInterface.GetType().Assembly.GetType("Dalamud.Dalamud", true)!).GetMethod("Get")!.Invoke(null, BindingFlags.Default, null, [], null)!;
		DalamudStartInfo = (DalamudStartInfo)info.GetType().GetProperty("StartInfo", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(info)!;
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
		// WindowSystem.AddWindow(ActStatics = new ACTStatics());
		IpcProviders = new IpcProviders(PluginInterface);
		Container = new TinyIoCContainer();
		var logger = new Logger(Log);
		Container.Register(logger);
		Container.Register<ILogger>(logger);
		Container.Register(HttpClient);
		Container.Register(FileDialogManager);
		Container.Register(PluginInterface);
		Container.Register(OverlayPlugin = new PluginMain(PluginAssemblyDirectory, logger, Container));
		OverlayPlugin.PreInitPlugin(PluginConfigDirectory);
		oFormActMain.OverlayPluginContainer = Container;
		opcodesjsoncReplaced = opcodesjsoncCanReplace;
		oFormActMain.TriggernometryPlugin = TriggernometryProxyPlugin = new ProxyPlugin();
		oFormActMain.PostNamazuPlugin = PostNamazuPlugin = new PostNamazu.PostNamazu();

		var extraOpcodes = opcodesjsoncCanReplace ? File.ReadAllText(opcodesjsoncPath) : null;
		FormActMain.AddDefaultPlugins(FfxivActPluginWrapper = new FfxivActPluginWrapper(), new PluginLoader(OverlayPlugin), TriggernometryProxyPlugin, PostNamazuPlugin);
		LogTick("FfxivActPlugin Inited");
		OverlayPlugin.InitPlugin(LogTick, extraOpcodes);
		LogTick("OverlayPlugin Initialized");
		var taskTrn = Task.Run(() => {
			TriggernometryProxyPlugin.InitPlugin(this, PluginInterface, Log, ClientState, Framework, GameInteropProvider, ObjectTable, GameGui, SigScanner,
				LatestConfigVersion);
		});
		if (!Configuration.AsyncOnInit) taskTrn.Wait();
		if (opcodesjsoncReplaced) Log.Warning("opcodesjsonc Replaced");
		var registry = Container.Resolve<Registry>();
		MainWindow.OverlayPresets = registry.OverlayPresets;
		MainWindow.Server = WebSocketServer = Container.Resolve<ServerController>();
		IpcProviders.Server = WebSocketServer;
		IpcProviders.OverlayIpcHandler = Container.Resolve<IpcHandlerController>();
		MainWindow.OverlayPluginConfig = Container.Resolve<IPluginConfig>();
		OverlayWindow.Init(WebSocketServer);
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
		Framework.Update += CheckCnUpdate;
		ClientState.Logout += OnLogOut;
		ZoneDownHookManager = new ZoneDownHookManager();
		foreach (var rt in Directory.GetFiles(PluginActScriptDirectory, "*.dll", SearchOption.TopDirectoryOnly).Select(Path.GetFileName).Cast<string>())
			if (Configuration.ActScriptsEnabled.Contains(rt))
				LoadIActPluginV1(rt, preserveEnableState: true);
		PostNamazuPlugin.InitPlugin(PluginInterface, Log, SigScanner);
		LogTick("Waiting Triggernometry");
		CancellationTokenSource postCts = new();
		var token = postCts.Token;
		if (Configuration.AsyncOnInit) taskTrn.Wait();
		LogTick("Triggernometry & PostNamazu & Callback Initialized");
		var sourceDir = Path.Combine(Instance.PluginAssemblyDirectory, "scripts");
		var targetDir = Instance.PluginActScriptDirectory;
		Directory.CreateDirectory(targetDir);
		CopyDirectoryContents(sourceDir, targetDir, true);
		foreach (var rt in Directory.GetFiles(PluginActScriptDirectory, "*.cs", SearchOption.TopDirectoryOnly).Select(Path.GetFileName).Cast<string>())
			if (Configuration.ActScriptsEnabled.Contains(rt))
				LoadPScript(rt, true);
		var taskPP = Task.Run(() => {
			LogTick("Asyc Post Start");
			BridgeNamazu.InitializeModules();
			LogTick("InitializeModules");
			BridgeNamazu.RegisterAnnotatedMethods();
			FormActMain.PluginInitialized = true;
			LogTick("Asyc Post Process Done");
		}, token);
		if (!Configuration.AsyncOnInit) taskPP.Wait();
		if (Configuration.LoadSilverDasherOnInit) EnableSilverDasher();
		if (Directory.Exists(Path.Combine(PluginConfigDirectory, "cactbot"))) RefreshBw();
		if (Configuration.ShowWindowOnInit) MainWindow.Toggle();
		if (Configuration.ShowOverlayOnInit) OverlayWindow.Toggle();
		if (Configuration.TtsOnInit) oFormActMain.TTS("插件加载完成");
		Configuration.Version = LatestConfigVersion;
		Configuration.Save();
		Log.Info($"[StartTick] IINACTEx Inited. Total {(DateTime.Now - startLogTick).TotalSeconds}s");
	}

	private static void OnLogOut(int type, int code) {
		if (DisableSilverDasher())
			Task.Run(async () => {
				await Task.Delay(5000);
				EnableSilverDasher();
			});
	}

	internal static DateTime lastCnUpdateCheck = DateTime.Now.AddMinutes(-8);

	private void CheckCnUpdate(IFramework _) {
		if (!((DateTime.Now - lastCnUpdateCheck).TotalMinutes > 10)) return;
		Configuration.FFXIV_ACT_Plugin_CN_Update = _fetchDependencies.CheckCnUpdate();
		Configuration.Save();
		lastCnUpdateCheck = DateTime.Now;
	}

	public static void InitIActPluginV1(ActPluginData plugin, bool preserveEnableState = false) {
		try {
			Log.Info($"正在加载IActPluginV1 {plugin.pluginFileName}");
			oFormActMain.ActPlugins.Add(plugin);
			plugin.PluginForm?.Show();
			plugin.pluginObj.InitPlugin(plugin.tpPluginSpace, plugin.lblPluginStatus);
			if (!preserveEnableState) {
				Configuration.ActScriptsEnabled.Add(plugin.pluginFileName);
				Configuration.Save();
			}
		} catch (Exception e) {
			Log.Error(e.ToString());
		}
	}

	public static void DeInitIActPluginV1(ActPluginData plugin, bool preserveEnableState = false) {
		try {
			Log.Info($"正在卸载IActPluginV1 {plugin.pluginFileName}");
			if (!preserveEnableState) {
				Configuration.ActScriptsEnabled.Remove(plugin.pluginFileName);
				Configuration.Save();
			}
			plugin.pluginObj.DeInitPlugin();
			if (plugin.PluginForm != null) {
				plugin.PluginForm.canClose = true;
				plugin.PluginForm.Close();
				plugin.PluginForm.Dispose();
			}
		} catch (Exception e) {
			Log.Error(e.ToString());
		}
		oFormActMain.ActPlugins.Remove(plugin);
	}

	public static void LoadIActPluginV1(string name, string? dllPath = null, bool preserveEnableState = false) {
		try {
			Assembly asm;
			using (var memoryStream = new MemoryStream(File.ReadAllBytes(dllPath ?? Path.Combine(Instance.PluginActScriptDirectory, name)))) {
				asm = AssemblyLoadContext.GetLoadContext(Assembly.GetExecutingAssembly())!.LoadFromStream(memoryStream);
			}
			Log.Info($"Loading IActPluginV1 {asm.FullName}");
			var scriptTypes = asm.GetTypes()
				.Where(type => type is { IsAbstract: false, IsInterface: false }
				               && typeof(IActPluginV1).IsAssignableFrom(type))
				.ToList();
			var isIScriptBase = scriptTypes.Any(type => typeof(IScriptBase).IsAssignableFrom(type));
			foreach (var type in scriptTypes)
				if (Activator.CreateInstance(type) is IActPluginV1 scriptInstance)
					InitIActPluginV1(new ActPluginData(name, scriptInstance, isIScriptBase), preserveEnableState);
		} catch (Exception ex) {
			Log.Warning($"ActScript {name} 载入失败: {ex}");
		}
	}

	public static void LoadPScript(string name, bool preserveEnableState = false) {
		try {
			var rs = File.ReadAllText(Path.Combine(Instance.PluginActScriptDirectory, name));
			if (CSharpScriptCompiler.CompileScript(rs, false))
				LoadIActPluginV1(name, Path.Combine(Instance.PluginConfigDirectory, "Scripts", Path.GetFileName(CSharpScriptCompiler.GetScriptDllPath(rs))), preserveEnableState);
		} catch (Exception ex) {
			Log.Warning($"ActScript {name} 载入失败: {ex}");
		}
	}

	public const ImGuiTableFlags ImGuiTableFlag = ImGuiTableFlags.Borders | ImGuiTableFlags.Resizable | ImGuiTableFlags.RowBg;

	public const BindingFlags AllFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;

	public void Dispose() {
		Configuration.Save();
		TextToSpeechProvider.Dispose();
		PluginInterface.UiBuilder.Draw -= DrawUI;
		PluginInterface.UiBuilder.OpenConfigUi -= DrawConfigUI;
		PluginInterface.UiBuilder.OpenMainUi -= DrawConfigUI;
		ClientState.EnterPvP -= EnterPvP;
		ClientState.LeavePvP -= LeavePvP;
		Framework.Update -= CheckCnUpdate;
		ClientState.Logout -= OnLogOut;
		IpcProviders.Dispose();
		ZoneDownHookManager.Dispose();
		// Trace.Listeners.Remove(PluginLogTraceListener);
		WindowSystem.RemoveAllWindows();
		OverlayWindow.Dispose();
		SilverDasherPlugin?.DeInitPlugin();
		CommandManager.RemoveHandler(MainWindowCommandName);
		CommandManager.RemoveHandler(EndEncCommandName);
		CommandManager.RemoveHandler(OverlayCommandName);
		oFormActMain.ActPlugins.RemoveAt(0);
		while (oFormActMain.ActPlugins.Count > 0)
			DeInitIActPluginV1(oFormActMain.ActPlugins.Last(), true);
		FfxivActPluginWrapper.Dispose();
		ActGlobals.Dispose();
		// MainWindow. TpMain?.Close();
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
			oFormActMain.EndCombat(false);
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