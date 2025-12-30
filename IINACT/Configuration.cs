using System.IO;
using Dalamud.Configuration;
using Dalamud.Plugin;
using Newtonsoft.Json;

namespace IINACT;

[Serializable]
public class Configuration : IPluginConfiguration
{
    [JsonIgnore]
    public string DefaultLogFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "IINACT");
    private string? logFilePath;

    [JsonIgnore]
    private IDalamudPluginInterface? PluginInterface { get; set; }

    public int ParseFilterMode { get; set; }

    public bool DisableDamageShield { get; set; }

    public bool DisableCombinePets { get; set; }

    public bool DisablePvp { get; set; }
    
    public bool SimulateIndividualDoTCrits { get; set; }

    public bool ShowRealDoTTicks { get; set; }

    public bool ShowDebug { get; set; }

    [JsonProperty("useEdgeTTS")]
    public bool UseEdgeTTS { get; set; } = true;
    [JsonProperty("useLatihasTTS")]
    public bool UseLatihasTts { get; set; } = true;
    public bool ShowWindowOnInit { get; set; } = true;
    public bool ShowOverlayOnInit { get; set; } = true;
    public bool TtsOnInit { get; set; } = true;

    public string LogFilePath
    {
        get => Directory.Exists(logFilePath) ? logFilePath : DefaultLogFilePath;
        set => logFilePath = value;
    }

    public bool WriteLogFile
    {
        get => Advanced_Combat_Tracker.ActGlobals.oFormActMain.WriteLogFile;
        set => Advanced_Combat_Tracker.ActGlobals.oFormActMain.WriteLogFile = value;
    }
    
	public bool WriteActLogFile
	{
		get => Advanced_Combat_Tracker.ActGlobals.oFormActMain.WriteActLogFile;
		set => Advanced_Combat_Tracker.ActGlobals.oFormActMain.WriteActLogFile = value;
	}
    
    public bool WriteTrnLogFile
    {
        get => Advanced_Combat_Tracker.ActGlobals.oFormActMain.WriteTrnLogFile;
        set => Advanced_Combat_Tracker.ActGlobals.oFormActMain.WriteTrnLogFile = value;
    }

    public bool DisableWritingPvpLogFile
    {
        get => Advanced_Combat_Tracker.ActGlobals.oFormActMain.DisableWritingPvpLogFile;
        set => Advanced_Combat_Tracker.ActGlobals.oFormActMain.DisableWritingPvpLogFile = value;
    }
    public List<string> ActScriptsEnabled { get; set; } = [];
    public int Version { get; set; } = 1;
    
    public string? SelectedOverlay { get; set; }

    public void Initialize(IDalamudPluginInterface pluginInterface)
    {
        PluginInterface = pluginInterface;
    }

    public void Save()
    {
        PluginInterface?.SavePluginConfig(this);
    }
    
}
