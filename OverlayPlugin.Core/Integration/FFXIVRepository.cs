using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Advanced_Combat_Tracker;
using FFXIV_ACT_Plugin.Common;
using FFXIV_ACT_Plugin.Common.Models;
using FFXIV_ACT_Plugin.Logfile;
using Machina.FFXIV;
using Machina.FFXIV.Headers.Opcodes;
using Machina.Infrastructure;

namespace RainbowMage.OverlayPlugin;

/* Taken from FFIXV_ACT_Plugin.Logfile. Copy&pasted to avoid issues if the FFXIV plugin ever changes this enum. */
public enum LogMessageType {
	LogLine,
	ChangeZone,
	ChangePrimaryPlayer,
	AddCombatant,
	RemoveCombatant,
	AddBuff,
	RemoveBuff,
	FlyingText,
	OutgoingAbility,
	IncomingAbility = 10,
	PartyList,
	PlayerStats,
	CombatantHP,
	ParsedPartyMember,
	NetworkStartsCasting = 20,
	NetworkAbility,
	NetworkAOEAbility,
	NetworkCancelAbility,
	NetworkDoT,
	NetworkDeath,
	NetworkBuff,
	NetworkTargetIcon,
	NetworkTargetMarker = 29,
	NetworkBuffRemove,
	NetworkGauge,
	NetworkWorld,
	Network6D,
	NetworkNameToggle,
	NetworkTether,
	NetworkLimitBreak,
	NetworkEffectResult,
	NetworkStatusList,
	NetworkUpdateHp,
	ChangeMap,
	Settings = 249,
	Process,
	Debug,
	PacketDump,
	Version,
	Error,
	Timer,

	// OverlayPlugin lines
	RegisterLogLine = 256,
	MapEffect,
	FateDirector,
	CEDirector,
	InCombat
}

[SuppressMessage("ReSharper", "UnusedMember.Global")]
public class FFXIVRepository {
	private IDataRepository? repository;
	private IDataSubscription? subscription;

	internal static FFXIV_ACT_Plugin.FFXIV_ACT_Plugin GetPluginData() => ActGlobals.oFormActMain.FfxivPlugin;

	private IDataRepository GetRepository() => repository ??= GetPluginData().DataRepository;

	private IDataSubscription GetSubscription() => subscription ??= GetPluginData().DataSubscription;

	[MethodImpl(MethodImplOptions.NoInlining)]
	private Process GetCurrentFFXIVProcessImpl() => GetRepository().GetCurrentFFXIVProcess();

	[Obsolete("Subscribe to the ProcessChanged event instead (See RegisterProcessChangedHandler())")]
	[MethodImpl(MethodImplOptions.NoInlining)]
	public Process GetCurrentFFXIVProcess() => GetCurrentFFXIVProcessImpl();

	[MethodImpl(MethodImplOptions.NoInlining)]
	private uint? GetCurrentTerritoryIDImpl() => GetRepository().GetCurrentTerritoryID();

	[MethodImpl(MethodImplOptions.NoInlining)]
	public uint? GetCurrentTerritoryID() => GetCurrentTerritoryIDImpl();

	[MethodImpl(MethodImplOptions.NoInlining)]
	public bool IsFFXIVPluginPresent() => true;


	public Version? GetOverlayPluginVersion() => Assembly.GetExecutingAssembly().GetName().Version;

	public Version? GetPluginVersion() => typeof(FFXIV_ACT_Plugin.FFXIV_ACT_Plugin).Assembly.GetName().Version;

	public string GetPluginPath() => typeof(IDataRepository).Assembly.Location;

	[MethodImpl(MethodImplOptions.NoInlining)]
	private string GetGameVersionImpl() => GetRepository().GetGameVersion();

	[MethodImpl(MethodImplOptions.NoInlining)]
	public string GetGameVersion() => GetGameVersionImpl();

	[MethodImpl(MethodImplOptions.NoInlining)]
	public uint GetPlayerIDImpl() => GetRepository().GetCurrentPlayerID();

	[MethodImpl(MethodImplOptions.NoInlining)]
	public uint GetPlayerID() => GetPlayerIDImpl();

	public string? GetPlayerNameImpl() {
		var repo = GetRepository();
		var playerId = repo.GetCurrentPlayerID();
		var playerInfo = repo.GetCombatantList().FirstOrDefault(x => x.ID == playerId);
		return playerInfo?.Name;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	public IDictionary<uint, string> GetResourceDictionary(ResourceType resourceType) =>
		GetResourceDictionaryImpl(resourceType);

	public IDictionary<uint, string> GetResourceDictionaryImpl(ResourceType resourceType) =>
		GetRepository().GetResourceDictionary(resourceType);

	[MethodImpl(MethodImplOptions.NoInlining)]
	public string? GetPlayerName() => GetPlayerNameImpl();

	public ReadOnlyCollection<Combatant> GetCombatants() => GetRepository().GetCombatantList();

	[MethodImpl(MethodImplOptions.NoInlining)]
	public Language GetLanguage() => GetRepository().GetSelectedLanguageID();

	public string GetLocaleString() => GetLanguage() switch {
		Language.English => "en",
		Language.French => "fr",
		Language.German => "de",
		Language.Japanese => "ja",
		Language.Chinese => "cn",
		Language.Korean => "ko",
		_ => null
	};

	public static Dictionary<GameRegion, Dictionary<string, ushort>> GetMachinaOpcodes() => OpcodeManager.Instance._opcodes;

	public GameRegion GetMachinaRegion() => OpcodeManager.Instance.GameRegion;

	public DateTime EpochToDateTime(long epoch) => ConversionUtility.EpochToDateTime(epoch).ToLocalTime();

	/**
	 * * Convert a coordinate expressed as a uint16 to a float.
	 * *
	 * * See https://github.com/ravahn/FFXIV_ACT_Plugin/issues/298, though this has been
	 * * updated to be more accurate.
	 */
	public static float ConvertUInt16Coordinate(ushort value) =>
		// This is the exact same formula the game client uses
		(float)(value * 3.0518043 * 0.0099999998 - 1000.0);

	/**
	 * * Convert a packet heading to an in-game headiung.
	 * *
	 * * When a heading is sent in certain packets, the heading is expressed as a uint16, where
	 * * 0=north and each increment is 1/65536 of a turn in the CCW direction.
	 * *
	 * * See https://github.com/ravahn/FFXIV_ACT_Plugin/issues/298, though this has been
	 * * updated to be more accurate.
	 */
	public static double ConvertHeading(ushort heading) =>
		// This is the exact same formula the game client uses
		heading * 0.009587526 * 0.0099999998 - Math.PI;

	private ILogOutput? _logOutput;

	[MethodImpl(MethodImplOptions.NoInlining)]
	public bool WriteLogLineImpl(uint ID, DateTime timestamp, string line) {
		_logOutput ??= (ILogOutput)GetPluginData()._iocContainer.GetService(typeof(ILogOutput));
		_logOutput?.WriteLine((FFXIV_ACT_Plugin.Logfile.LogMessageType)(int)ID, timestamp, line);
		return true;
	}

	// LogLineDelegate(uint EventType, uint Seconds, string logline);
	public void RegisterLogLineHandler(Action<uint, uint, string> handler) {
		GetSubscription().LogLine += new LogLineDelegate(handler);
	}

	// NetworkReceivedDelegate(string connection, long epoch, byte[] message)
	public void RegisterNetworkParser(Action<string, long, byte[]> handler) {
		GetSubscription().NetworkReceived += new NetworkReceivedDelegate(handler);
	}

	// PartyListChangedDelegate(ReadOnlyCollection<uint> partyList, int partySize)
	//
	// Details: partySize may differ from partyList.Count.
	// In non-cross world parties, players who are not in the same
	// zone count in the partySize but do not appear in the partyList.
	// In cross world parties, nobody will appear in the partyList.
	// Alliance data members show up in partyList but not in partySize.
	public void RegisterPartyChangeDelegate(Action<ReadOnlyCollection<uint>, int> handler) {
		GetSubscription().PartyListChanged += new PartyListChangedDelegate(handler);
	}

	// ProcessChangedDelegate(Process process)
	public void RegisterProcessChangedHandler(Action<Process> handler) {
		GetSubscription().ProcessChanged += new ProcessChangedDelegate(handler);
		handler(Process.GetCurrentProcess());
	}

	public void RegisterZoneChangeDelegate(Action<uint, string> handler) {
		GetSubscription().ZoneChanged += new ZoneChangedDelegate(handler);
	}

	public DateTime GetServerTimestamp() => GetRepository()?.GetServerTimestamp() ?? DateTime.Now;
}