using System;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Threading;
using Advanced_Combat_Tracker;
using Dalamud.Game.Chat;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.Text;
using Dalamud.Plugin.Services;
using FFXIV_ACT_Plugin;
using FFXIV_ACT_Plugin.Common;
using FFXIV_ACT_Plugin.Config;
using FFXIV_ACT_Plugin.Logfile;
using FFXIV_ACT_Plugin.Memory;
using FFXIV_ACT_Plugin.Memory.MemoryProcessors;
using FFXIV_ACT_Plugin.Memory.MemoryReader;
using FFXIV_ACT_Plugin.Memory.Models.Global;
using FFXIV_ACT_Plugin.Parse;
using FFXIV_ACT_Plugin.Resource;
using IINACT.Network;
using Microsoft.MinIoC;
using ACTWrapper = FFXIV_ACT_Plugin.Common.ACTWrapper;
using Region = FFXIV_ACT_Plugin.Config.Region;

namespace IINACT;

public partial class FfxivActPluginWrapper : IDisposable {
	public readonly FFXIV_ACT_Plugin.FFXIV_ACT_Plugin ffxivActPlugin;
	private ISettingsMediator settingsMediator = null!;
	private readonly ParseMediator parseMediator;

	private readonly ServerTimeProcessor serverTimeProcessor;
	private readonly MobArrayProcessor mobArrayProcessor;
	private readonly IZoneMapProcessor zoneMapProcessor;
	private readonly CombatantManager combatantManager;
	private readonly IPlayerProcessor playerProcessor;
	private readonly IPartyProcessor partyProcessor;

	private readonly int mobArraySize;
	private readonly int combatantSize;
	private readonly nint mobData;
	private readonly nint[] mobDataOffsets;
	private readonly SemaphoreSlim refreshSemaphore = new(0);
	private byte mobDataAge = byte.MaxValue;

	private readonly ILogOutput logOutput;
	private readonly ILogFormat logFormat;
	private readonly IProcessManager processManager;

	public DataCollectionSettingsEventArgs DataCollectionSettings = null!;
	public ParseSettings ParseSettings = null!;
	public readonly IDataRepository Repository;
	public readonly IDataSubscription Subscription;

	public unsafe FfxivActPluginWrapper() {
		ffxivActPlugin = new FFXIV_ACT_Plugin.FFXIV_ACT_Plugin();
		Plugin.Log.Information($"Initializing FFXIV_ACT_Plugin version {typeof(FFXIV_ACT_Plugin.FFXIV_ACT_Plugin).Assembly.GetName().Version}");
		ffxivActPlugin.ConfigureIOC();
		var iocContainer1 = ffxivActPlugin._iocContainer;
		Plugin.LogTick("FFXIV_ACT_Plugin IOC Configured");
		iocContainer1.Resolve<ResourceManager>().LoadResources();
		Plugin.LogTick("FFXIV_ACT_Plugin Resources Loaded");
		Subscription = iocContainer1.Resolve<DataSubscription>();
		ffxivActPlugin.SetProperty("DataSubscription", Subscription);
		parseMediator = iocContainer1.Resolve<ParseMediator>();

		ffxivActPlugin._dataCollection = iocContainer1.Resolve<DataCollection>();

		logOutput = ffxivActPlugin._dataCollection._logOutput;
		logFormat = ffxivActPlugin._dataCollection._logFormat;

		var scanMemory = (ScanMemory)ffxivActPlugin._dataCollection._scanMemory;

		processManager = scanMemory._processManager;
		serverTimeProcessor = (ServerTimeProcessor)scanMemory._serverTimeProcessor;
		mobArrayProcessor = (MobArrayProcessor)scanMemory._mobArrayProcessor;
		zoneMapProcessor = scanMemory._zoneProcessor;
		combatantManager = (CombatantManager)scanMemory._combatantManager;
		playerProcessor = scanMemory._playerProcessor;
		partyProcessor = scanMemory._partyProcessor;
		SetupActWrapper();

		SetupDataSubscription();

		SetupSettingsMediator();
		Repository = iocContainer1.Resolve<IDataRepository>();
		ffxivActPlugin.SetProperty("DataRepository", Repository);

		ffxivActPlugin._dataCollection.StartMemory();

		Plugin.ChatGui.ChatMessage += OnChatMessage;
		ActGlobals.oFormActMain.BeforeLogLineRead += OFormActMain_BeforeLogLineRead;
		serverTimeProcessor.ServerTime = DateTime.Now;

		Plugin.Framework.Update += ScanMemory;

		mobArraySize = mobArrayProcessor._internalMmobArray.Length;
		var combatantProcessor = (CombatantProcessor)combatantManager._combatantProcessor;
		var combatantBufferSize1 = ((ReadCombatant)combatantProcessor._readCombatant)._buffer.Length;
		combatantSize = sizeof(CombatantStruct);
		mobData = Marshal.AllocHGlobal(mobArraySize * combatantSize + (combatantBufferSize1 - combatantSize));
		mobDataOffsets = new nint[mobArraySize];
		for (var i = 0; i < mobArraySize; i++)
			mobDataOffsets[i] = mobData + i * combatantSize;

		Plugin.Framework.Update += MobDataRefresh;
	}


	private Language ClientLanguage =>
		Plugin.DataManager.Language switch {
			Dalamud.Game.ClientLanguage.Japanese => Language.Japanese,
			Dalamud.Game.ClientLanguage.English => Language.English,
			Dalamud.Game.ClientLanguage.German => Language.German,
			Dalamud.Game.ClientLanguage.French => Language.French,
			_ => Plugin.DataManager.Language.ToString() == "ChineseSimplified" ? Language.Chinese : Language.English //非CN SDK无ChineseSimplified
		};

	public void Dispose() {
		Plugin.Framework.Update -= ScanMemory;
		Plugin.Framework.Update -= MobDataRefresh;
		Plugin.ChatGui.ChatMessage -= OnChatMessage;
		ActGlobals.oFormActMain.BeforeLogLineRead -= OFormActMain_BeforeLogLineRead;
		ffxivActPlugin.DeInitPlugin();
		ffxivActPlugin.Dispose();
		Marshal.FreeHGlobal(mobData);
	}

	private void SetupSettingsMediator() {
		settingsMediator = ffxivActPlugin._dataCollection._settingsMediator;

		DataCollectionSettings = new DataCollectionSettingsEventArgs {
			LogFileFolder = ActGlobals.oFormActMain.LogFilePath,
			RegionID = Region.Global,
			ProcessID = Environment.ProcessId
		};
		settingsMediator.DataCollectionSettings = DataCollectionSettings;

		ParseSettings = new ParseSettings {
			DisableDamageShield = Plugin.Configuration.DisableDamageShield,
			DisableCombinePets = Plugin.Configuration.DisableCombinePets,
			LanguageID = ClientLanguage,
			ParseFilter = (ParseFilterMode)Plugin.Configuration.ParseFilterMode,
			SimulateIndividualDoTCrits = Plugin.Configuration.SimulateIndividualDoTCrits,
			ShowRealDoTTicks = Plugin.Configuration.ShowRealDoTTicks,
			ShowDebug = Plugin.Configuration.ShowDebug,
			EnableBenchmarks = false
		};
		settingsMediator.ParseSettings = ParseSettings;

		settingsMediator.ProcessException = OnProcessException;

		var line = logFormat.FormatParseSettings(ParseSettings.DisableDamageShield, ParseSettings.DisableCombinePets,
			ParseSettings.LanguageID, ParseSettings.ParseFilter,
			ParseSettings.SimulateIndividualDoTCrits,
			ParseSettings.ShowRealDoTTicks);
		logOutput.WriteLine(LogMessageType.Settings, DateTime.MinValue, line);

		var line2 = logFormat.FormatMemorySettings(DataCollectionSettings.ProcessID,
			DataCollectionSettings.LogFileFolder,
			DataCollectionSettings.LogAllNetworkData,
			DataCollectionSettings.DisableCombatLog,
			DataCollectionSettings.RegionID);
		logOutput.WriteLine(LogMessageType.Settings, DateTime.MinValue, line2);

		logOutput.CallMethod("ConfigureLogFile", null);
		ActGlobals.oFormActMain.GetDateTimeFromLog = parseMediator.ParseLogDateTime;

		if (!processManager.Verify())
			throw new InvalidOperationException("Game offsets could not be found");
	}

	private void OnChatMessage(IHandleableChatMessage message) {
		var evenType = (uint)message.LogKind;
		var player = message.Sender.TextValue;
		var text = message.Message.TextValue.Replace('\r', ' ').Replace('\n', ' ').Replace('|', '❘');
		if (message.LogKind == XivChatType.SystemMessage)
			evenType = evenType | (uint)message.TargetKind << 7 | (uint)message.SourceKind << 11;
		var line = logFormat.FormatChatMessage(evenType, player, text);
		logOutput.WriteLine(LogMessageType.ChatLog, GameServerTime.CurrentServerTime, line);
	}

	private void SetupActWrapper() {
		var actWrapper = logOutput.GetField<ACTWrapper>("_actWrapper");

		actWrapper.TimeStampLen = DateTime.Now.ToString("HH:mm:ss.fff", CultureInfo.InvariantCulture).Length + 3;
		actWrapper.LogPathHasCharName = false;
		actWrapper.OverrideMainFormVisible = true;

		ACT_UIMods.UpdateACTTables(false);

		ActGlobals.oFormActMain.FfxivPlugin = ffxivActPlugin;
	}

	private void OFormActMain_BeforeLogLineRead(bool isImport, LogLineEventArgs logInfo) {
		(logInfo.logLine, logInfo.detectedType) =
			parseMediator.BeforeLogLineRead(isImport, logInfo.detectedTime, logInfo.logLine);
	}

	private void SetupDataSubscription() {
		ffxivActPlugin.DataSubscription.ZoneChanged += OnZoneChanged;
	}

	private static void OnZoneChanged(uint zoneId, string zoneName) {
		ActGlobals.oFormActMain.ChangeZone(zoneName);
	}

	private static void OnProcessException(DateTime timestamp, string text) {
		Plugin.Log.Debug($"[FFXIV_ACT_Plugin] {text}");
	}

	[SuppressGCTransition]
	[LibraryImport("SafeMemoryReader.dll")]
	private static partial int ReadMemory(nint dest, nint src, int size);

	private unsafe void MobDataRefresh(IFramework _) {
		if (settingsMediator.DataCollectionSettings == null)
			return;

		if (mobDataAge < 3 || !Plugin.Condition[ConditionFlag.BoundByDuty56] && mobDataAge < 10) {
			mobDataAge++;
			if (mobArrayProcessor.PrimaryPlayerPointer == nint.Zero)
				return;

			refreshSemaphore.Release();
			return;
		}

		mobDataAge = 0;
		var mobArrayAddress = (ulong*)mobArrayProcessor._readMobArray.Read64();
		var mobArray = mobArrayProcessor._internalMmobArray;

		if (*mobArrayAddress == 0) {
			Array.Clear(mobArray);
			return;
		}

		ReadMemory(mobDataOffsets[0], (nint)(void*)*mobArrayAddress, combatantSize);
		mobArray[0] = mobDataOffsets[0];

		for (var i = 1; i < mobArraySize; i++) {
			if (*(mobArrayAddress + i) == 0) {
				mobArray[i] = nint.Zero;
				continue;
			}
			ReadMemory(mobDataOffsets[i], (nint)(void*)*(mobArrayAddress + i), combatantSize);
			mobArray[i] = mobDataOffsets[i];
		}

		refreshSemaphore.Release();
	}

	private void ScanMemory(IFramework framework) {
		try {
			serverTimeProcessor.ServerTime = GameServerTime.CurrentServerTime;

			var zoneId = zoneMapProcessor.ZoneID;
			zoneMapProcessor.Refresh();
			if (zoneMapProcessor.ZoneID == 0)
				return;

			if (zoneId != zoneMapProcessor.ZoneID)
				combatantManager.Rescan();
			else
				combatantManager.Refresh();

			playerProcessor.Refresh();
			partyProcessor.Refresh();
		} catch (Exception ex) when (ex is ThreadAbortException or OperationCanceledException or ObjectDisposedException) {
			return;
		} catch (Exception ex) {
			Plugin.Log.Error(ex, "[FFXIV_ACT_Plugin] ScanMemory failure");
		}
	}
}