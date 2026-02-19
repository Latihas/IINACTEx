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

    public bool SimulateIndividualDoTCrits { get; set; }

    public bool ShowRealDoTTicks { get; set; }

    public bool ShowDebug { get; set; }

    [JsonProperty("useEdgeTTS")] public bool UseEdgeTts { get; set; } = true;
    [JsonProperty("useLatihasTTS")] public bool UseLatihasTts { get; set; } = true;
    public bool ShowWindowOnInit { get; set; } = true;
    public bool ShowOverlayOnInit { get; set; } = true;
    public bool TtsOnInit { get; set; } = true;
    public bool LoadSilverDasherOnInit { get; set; }
    public float TtsInterval { get; set; } = 1.5f;


    public string LogFilePath
    {
        get => Directory.Exists(logFilePath) ? logFilePath : DefaultLogFilePath;
        set => logFilePath = value;
    }

    public bool WriteLogFile
    {
        get => ActGlobals.oFormActMain.WriteLogFile;
        set => ActGlobals.oFormActMain.WriteLogFile = value;
    }

    public bool WriteActLogFile
    {
        get => ActGlobals.oFormActMain.WriteActLogFile;
        set => ActGlobals.oFormActMain.WriteActLogFile = value;
    }

    public bool WriteTrnLogFile
    {
        get => ActGlobals.oFormActMain.WriteTrnLogFile;
        set => ActGlobals.oFormActMain.WriteTrnLogFile = value;
    }

    public bool DisableWritingPvpLogFile
    {
        get => ActGlobals.oFormActMain.DisableWritingPvpLogFile;
        set => ActGlobals.oFormActMain.DisableWritingPvpLogFile = value;
    }
    public List<string> ActScriptsEnabled { get; set; } = [];
    public int Version { get; set; } = 1;

    public string? SelectedOverlay { get; set; }

    public void Save() {
        Plugin.PluginInterface.SavePluginConfig(this);
    }
}