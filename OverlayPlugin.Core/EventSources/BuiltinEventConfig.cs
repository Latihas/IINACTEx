using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace RainbowMage.OverlayPlugin.EventSources;

[Serializable]
public class BuiltinEventConfig {
	public event EventHandler UpdateIntervalChanged;
	public event EventHandler EnmityIntervalChanged;
	public event EventHandler SortKeyChanged;
	public event EventHandler SortDescChanged;
	public event EventHandler UpdateDpsDuringImportChanged;
	public event EventHandler EndEncounterAfterWipeChanged;
	public event EventHandler EndEncounterOutOfCombatChanged;
	public event EventHandler LogLinesChanged;

	private int updateInterval = 1;

	public int UpdateInterval {
		get => updateInterval;
		set {
			if (updateInterval == value) return;
			updateInterval = value;
			UpdateIntervalChanged?.Invoke(this, EventArgs.Empty);
		}
	}

	private int enmityIntervalMs = 100;

	public int EnmityIntervalMs {
		get => enmityIntervalMs;
		set {
			if (enmityIntervalMs == value) return;
			enmityIntervalMs = value;
			EnmityIntervalChanged?.Invoke(this, EventArgs.Empty);
		}
	}

	private string sortKey = "encdps";

	public string SortKey {
		get => sortKey;
		set {
			if (sortKey == value) return;
			sortKey = value;
			SortKeyChanged?.Invoke(this, EventArgs.Empty);
		}
	}

	private bool sortDesc = true;

	public bool SortDesc {
		get => sortDesc;
		set {
			if (sortDesc == value) return;
			sortDesc = value;
			SortDescChanged?.Invoke(this, EventArgs.Empty);
		}
	}

	private bool updateDpsDuringImport;

	public bool UpdateDpsDuringImport {
		get => updateDpsDuringImport;
		set {
			if (updateDpsDuringImport == value) return;
			updateDpsDuringImport = value;
			UpdateDpsDuringImportChanged?.Invoke(this, EventArgs.Empty);
		}
	}

	private bool endEncounterAfterWipe = true;

	public bool EndEncounterAfterWipe {
		get => endEncounterAfterWipe;
		set {
			if (endEncounterAfterWipe == value) return;
			endEncounterAfterWipe = value;
			EndEncounterAfterWipeChanged?.Invoke(this, EventArgs.Empty);
		}
	}

	private bool endEncounterOutOfCombat = true;

	public bool EndEncounterOutOfCombat {
		get => endEncounterOutOfCombat;
		set {
			if (endEncounterOutOfCombat == value) return;
			endEncounterOutOfCombat = value;
			EndEncounterOutOfCombatChanged?.Invoke(this, EventArgs.Empty);
		}
	}

	private bool _logLines;

	public bool LogLines {
		get => _logLines;
		set {
			if (_logLines == value) return;
			_logLines = value;
			LogLinesChanged?.Invoke(this, EventArgs.Empty);
		}
	}

	// Data that overlays can save/load via event handlers.
	public Dictionary<string, JToken> OverlayData = new();

	public static BuiltinEventConfig LoadConfig(IPluginConfig config) {
		var result = new BuiltinEventConfig();

		if (!config.EventSourceConfigs.TryGetValue("MiniParse", out var obj)) return result;

		if (obj.TryGetValue("UpdateInterval", out var value)) {
			result.updateInterval = value.ToObject<int>();
		}

		if (obj.TryGetValue("EnmityIntervalMs", out value)) {
			result.enmityIntervalMs = value.ToObject<int>();
		}

		if (obj.TryGetValue("SortKey", out value)) {
			result.sortKey = value.ToString();
		}

		if (obj.TryGetValue("SortDesc", out value)) {
			result.sortDesc = value.ToObject<bool>();
		}

		if (obj.TryGetValue("UpdateDpsDuringImport", out value)) {
			result.updateDpsDuringImport = value.ToObject<bool>();
		}

		if (obj.TryGetValue("EndEncounterAfterWipe", out value)) {
			result.endEncounterAfterWipe = value.ToObject<bool>();
		}

		if (obj.TryGetValue("EndEncounterOutOfCombat", out value)) {
			result.endEncounterOutOfCombat = value.ToObject<bool>();
		}

		if (obj.TryGetValue("OverlayData", out value)) {
			result.OverlayData = value.ToObject<Dictionary<string, JToken>>();
		}

		if (obj.TryGetValue("LogLines", out value)) {
			result._logLines = value.ToObject<bool>();
		}

		return result;
	}

	public void SaveConfig(IPluginConfig Config) {
		var newObj = JObject.FromObject(this);
		if (Config.EventSourceConfigs.ContainsKey("MiniParse") &&
		    JToken.DeepEquals(Config.EventSourceConfigs["MiniParse"], newObj)) return;
		Config.EventSourceConfigs["MiniParse"] = newObj;
		Config.MarkDirty();
	}
}

public enum MiniParseSortType {
	None,
	StringAscending,
	StringDescending,
	NumericAscending,
	NumericDescending
}