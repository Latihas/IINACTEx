using System;
using System.Collections.Generic;
using System.IO;
using Advanced_Combat_Tracker;
using Dalamud.Configuration;
using Newtonsoft.Json;

namespace IINACT;

[Serializable]
public class Configuration : IPluginConfiguration {
	[JsonIgnore] public string DefaultLogFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "IINACT");
	private string? logFilePath;

	public int ParseFilterMode { get; set; }

	public bool DisableDamageShield { get; set; }

	public bool DisableCombinePets { get; set; }

	public bool DisablePvp { get; set; }

    public bool LogChatMessages { get; set; } = true;
    
	public bool SimulateIndividualDoTCrits { get; set; }

	public bool ShowRealDoTTicks { get; set; }

	public bool ShowDebug { get; set; }

	[JsonProperty("useEdgeTTS")] public bool UseEdgeTts { get; set; } = true;
	[JsonProperty("useLatihasTTS")] public bool UseLatihasTts { get; set; } = true;
	public bool ShowWindowOnInit { get; set; } = true;
	public bool ShowOverlayOnInit { get; set; } = true;
	public bool TtsOnInit { get; set; } = true;
	public bool LoadSilverDasherOnInit { get; set; }
	public bool AsyncOnInit { get; set; } = true;
	public float TtsInterval { get; set; } = 1.5f;
	public float ACTUpdateInterval { get; set; } = 0.5f;
	public bool UseActPic { get; set; } = true;
	public int endEncounterOutOfCombatDelayMs { get; set; } = 5000;


	public string LogFilePath {
		get => Directory.Exists(logFilePath) ? logFilePath : DefaultLogFilePath;
		set => logFilePath = value;
	}

	public bool WriteLogFile {
		get => ActGlobals.oFormActMain.WriteLogFile;
		set => ActGlobals.oFormActMain.WriteLogFile = value;
	}

	public bool WriteActLogFile {
		get => ActGlobals.oFormActMain.WriteActLogFile;
		set => ActGlobals.oFormActMain.WriteActLogFile = value;
	}

	public bool WriteTrnLogFile {
		get => ActGlobals.oFormActMain.WriteTrnLogFile;
		set => ActGlobals.oFormActMain.WriteTrnLogFile = value;
	}
	public bool DisableWritingPvpLogFile {
		get => ActGlobals.oFormActMain.DisableWritingPvpLogFile;
		set => ActGlobals.oFormActMain.DisableWritingPvpLogFile = value;
	}
	public string PlayerCharacterName
	{
		get => ActGlobals.charName;
		set => ActGlobals.charName = string.IsNullOrEmpty(value) ? "YOU" : value;
	}
	public List<string> ActScriptsEnabled { get; set; } = [];
    public int Version { get; set; } = 1;
    public bool InitFatalError { get; set; } 
	public string? SelectedOverlay { get; set; }
	public bool FFXIV_ACT_Plugin_CN_Update { get; set; }
    public string GoogleTtsLanguage { get; set; } = "en";
    public bool ForceGoogleTts { get; set; }
	public void Save() => Plugin.PluginInterface.SavePluginConfig(this);

    public int TtsPlaybackDevice { get; set; } = -1;
}