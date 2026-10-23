using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Advanced_Combat_Tracker;
using Newtonsoft.Json.Linq;
using RainbowMage.OverlayPlugin.WebSocket;

namespace RainbowMage.OverlayPlugin.EventSources;

internal class MiniParseEventSource : EventSourceBase {
	// private readonly List<string> importedLogs = [];

	private const string CombatDataEvent = "CombatData";
	private const string ImportedLogLinesEvent = "ImportedLogLines";
	private const string BroadcastMessageEvent = "BroadcastMessage";

	// Event Source

	private BuiltinEventConfig Config { get; set; }

	public MiniParseEventSource(TinyIoCContainer container) : base(container) {
		Name = "MiniParse";

		RegisterEventTypes([
			CombatDataEvent,
			ImportedLogLinesEvent,
			BroadcastMessageEvent
		]);

		RegisterEventHandler("saveData", msg => {
			if (Config?.OverlayData == null) return null;
			var key = msg["key"]?.ToString();
			var data = msg["data"]?.ToString();
			if (key == null || data == null) return null;
			Config.OverlayData[key] = data;
			return null;
		});

		RegisterEventHandler("loadData", msg => {
			if (Config?.OverlayData == null) return null;
			var key = msg["key"]?.ToString();
			if (key == null) return null;
			if (!Config.OverlayData.TryGetValue(key, out var value)) return null;
			return new JObject {
				["key"] = key,
				["data"] = value
			};
		});

		RegisterEventHandler("say", msg => {
			var text = msg["text"]?.ToString();
			if (text == null) return null;
			ActGlobals.oFormActMain.TTS(text);
			return null;
		});

		RegisterEventHandler("playSound", msg => {
			var file = msg["file"]?.ToString();
			if (file == null) return null;
			ActGlobals.oFormActMain.PlaySound(file);
			return null;
		});

		RegisterEventHandler("broadcast", msg => {
			if (!msg.ContainsKey("msg") || !msg.TryGetValue("source", out var value)) {
				Log(LogLevel.Error,
					"Called broadcast handler without specifying a source or message (\"source\" or \"msg\" property are missing).");
				return null;
			}

			if (value.Type != JTokenType.String) {
				Log(LogLevel.Error, "The source passed to the broadcast handler must be a string!");
				return null;
			}

			DispatchEvent(JObject.FromObject(new {
				type = BroadcastMessageEvent,
				source = msg["source"],
				msg = msg["msg"]
			}));

			return null;
		});

		RegisterEventHandler("openWebsiteWithWS", msg => {
			var result = new JObject();

			if (!msg.ContainsKey("url")) {
				Log(LogLevel.Error,
					"Called openWebsiteWithWS handler without specifying a URL (\"url\" property is missing).");
				result["$error"] =
					"Called openWebsiteWithWS handler without specifying a URL (\"url\" property is missing).";
				return result;
			}

			var wsServer = container.Resolve<ServerController>();

			if (!wsServer.Running) {
				result["$error"] = "WSServer is not running";
				return result;
			}

			try {
				var msgurl = msg["url"]?.ToString();
				if (string.IsNullOrEmpty(msgurl)) throw new Exception("url is empty.");
				var url = wsServer.GetModernUrl(msgurl);
				var proc = new Process();
				proc.StartInfo.Verb = "open";
				proc.StartInfo.FileName = url;
				proc.Start();
			} catch (Exception ex) {
				Log(LogLevel.Error, $"Failed to to open website: {ex}");
				result["$error"] = $"Failed to to open website: {ex}";
				return result;
			}

			result["success"] = true;
			return result;
		});

		// ActGlobals.oFormActMain.BeforeLogLineRead += (isImport, logInfo) => {
		// 	if (isImport) {
		// 		lock (importedLogs) {
		// 			importedLogs.Add(logInfo.originalLogLine);
		// 		}
		// 	}
		// };
	}

	public override void LoadConfig(IPluginConfig config) {
		Config = container.Resolve<BuiltinEventConfig>();

		Config.UpdateIntervalChanged += (_, _) => { Start(); };
	}

	public override void SaveConfig(IPluginConfig config) {
	}

	public override void Start() {
		timer?.Change(0, Config.UpdateInterval * 1000);
	}

	protected override void Update() {
		if (!CheckIsActReady()) return;
		if (!HasSubscriber(CombatDataEvent)) return;
		DispatchEvent(CreateCombatData());
	}

	private JObject CreateCombatData() {
		if (!CheckIsActReady()) return new JObject();
// #if DEBUG
// 		var stopwatch = new Stopwatch();
// 		stopwatch.Start();
// #endif
		var allies = ActGlobals.oFormActMain.ActiveZone?.ActiveEncounter.GetAllies();
		if (allies == null) return new JObject();
		var encounter = GetEncounterDictionary(allies);
		var combatant = GetCombatantList(allies);


		var obj = new JObject {
			["type"] = "CombatData",
			["Encounter"] = JObject.FromObject(encounter),
			["Combatant"] = new JObject()
		};

		if (Config.SortKey != "") {
			var factor = Config.SortDesc ? -1 : 1;
			var key = Config.SortKey;

			try {
				combatant.Sort((a, b) => {
					try {
						var aValue = float.Parse(a.Value[key]);
						var bValue = float.Parse(b.Value[key]);

						return factor * aValue.CompareTo(bValue);
					} catch (FormatException) {
						return 0;
					} catch (KeyNotFoundException) {
						return 0;
					}
				});
			} catch (Exception e) {
				Log(LogLevel.Error, Resources.ListSortFailed, key, e);
			}
		}

		foreach (var pair in combatant) {
			var value = new JObject();
			foreach (var pair2 in pair.Value) {
				value.Add(pair2.Key, Util.ReplaceNaNString(pair2.Value, "---"));
			}
			obj["Combatant"]![pair.Key.Name] = value;
		}

		obj["isActive"] = ActGlobals.oFormActMain.ActiveZone?.ActiveEncounter.Active == true ? "true" : "false";

// #if DEBUG
// 		stopwatch.Stop();
// 		Log(LogLevel.Trace, "CreateUpdateScript: {0} msec", stopwatch.Elapsed.TotalMilliseconds);
// #endif
		return obj;
	}

	private List<KeyValuePair<CombatantData, Dictionary<string, string>>> GetCombatantList(
		IEnumerable<CombatantData> allies) {
// #if TRACE
// 		var stopwatch = new Stopwatch();
// 		stopwatch.Start();
// #endif

		Dictionary<string, string> BuildCombatantDict(CombatantData ally) {
			var valueDict = new Dictionary<string, string>();
			foreach (var exportValuePair in CombatantData.ExportVariables) {
				try {
					// NAME タグには {NAME:8} のようにコロンで区切られたエクストラ情報が必要で、
					// プラグインの仕組み的に対応することができないので除外する
					if (exportValuePair.Key.StartsWith("NAME")) {
						continue;
					}

					// ACT_FFXIV_Plugin が提供する LastXXDPS は、
					// ally.Items[CombatantData.DamageTypeDataOutgoingDamage].Items に All キーが存在しない場合に、
					// プラグイン内で例外が発生してしまい、パフォーマンスが悪化するので代わりに空の文字列を挿入する
					if (exportValuePair.Key is "Last10DPS" or "Last30DPS" or "Last60DPS" or "Last180DPS") {
						if (!ally.Items[CombatantData.DamageTypeDataOutgoingDamage].Items.ContainsKey("All")) {
							valueDict.Add(exportValuePair.Key, "");
							continue;
						}
					}

					var value = exportValuePair.Value.GetExportString(ally, "");
					valueDict.Add(exportValuePair.Key, value);
				} catch (Exception e) {
					Log(LogLevel.Debug, "GetCombatantList: {0}: {1}: {2}", ally.Name, exportValuePair.Key, e);
				}
			}
			return valueDict;
		}

		var combatantList = allies.AsParallel()
			.Select(combatantData => new KeyValuePair<CombatantData, Dictionary<string, string>>(
				combatantData, BuildCombatantDict(combatantData)))
			.ToList();

// #if TRACE
// 		stopwatch.Stop();
// 		Log(LogLevel.Trace, "GetCombatantList: {0} msec", stopwatch.Elapsed.TotalMilliseconds);
// #endif

		return combatantList;
	}

	private Dictionary<string, string> GetEncounterDictionary(List<CombatantData> allies) {
// #if TRACE
// 		var stopwatch = new Stopwatch();
// 		stopwatch.Start();
// #endif

		var encounterDict = new Dictionary<string, string>();
		foreach (var exportValuePair in EncounterData.ExportVariables) {
			try {
// #if TRACE
// 				stopwatch.Reset();
// 				stopwatch.Start();
// #endif

				// ACT_FFXIV_Plugin が提供する LastXXDPS は、
				// ally.Items[CombatantData.DamageTypeDataOutgoingDamage].Items に All キーが存在しない場合に、
				// プラグイン内で例外が発生してしまい、パフォーマンスが悪化するので代わりに空の文字列を挿入する
				if (exportValuePair.Key is "Last10DPS" or "Last30DPS" or "Last60DPS" or "Last180DPS") {
					if (!allies.All(ally => ally.Items[CombatantData.DamageTypeDataOutgoingDamage].Items
						    .ContainsKey("All"))) {
						encounterDict.Add(exportValuePair.Key, "");
						continue;
					}
				}

				var value = exportValuePair.Value.GetExportString(
					ActGlobals.oFormActMain.ActiveZone?.ActiveEncounter,
					allies,
					"");
				encounterDict.Add(exportValuePair.Key, value);
			} catch (Exception e) {
				Log(LogLevel.Debug, "GetEncounterDictionary: {0}: {1}", exportValuePair.Key, e);
			}
		}

// #if TRACE
// 		stopwatch.Stop();
// 		Log(LogLevel.Trace, "GetEncounterDictionary: {0} msec", stopwatch.Elapsed.TotalMilliseconds);
// #endif

		return encounterDict;
	}

	private static bool CheckIsActReady() => ActGlobals.oFormActMain.ActiveZone?.ActiveEncounter != null;
}