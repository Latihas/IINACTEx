using System.IO;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Runtime.Loader;
using System.Speech.Synthesis;
using Advanced_Combat_Tracker;
using Dalamud.Plugin.Services;
using Dalamud.Utility;
using IINACT.TextToSpeech;
using NAudio.Wave;
using static IINACT.Plugin;

namespace IINACT;

internal class TextToSpeechProvider : IDisposable {
    private readonly object speechLock = new();
    private readonly HttpClient client = new();
    private SpeechSynthesizer? speechSynthesizer;
    private readonly EdgeTTSManager? edgeTTSManager;
    private readonly dynamic? LatihasTts;
    private bool useEdgeTTS;
    private bool useLatihasTTS;

    public TextToSpeechProvider() {
        try {
            var assetsdir = Path.Combine(Instance.PluginConfigDirectory, "TtsAssets");
            Assembly latihasTtsAssembly;
            using (var memoryStream = new MemoryStream(File.ReadAllBytes(Path.Combine(assetsdir, "System.Numerics.Tensors.dll")))) {
                AssemblyLoadContext.GetLoadContext(Assembly.GetExecutingAssembly())!.LoadFromStream(memoryStream);
            }
            using (var memoryStream = new MemoryStream(File.ReadAllBytes(Path.Combine(assetsdir, "Microsoft.ML.OnnxRuntime.dll")))) {
                AssemblyLoadContext.GetLoadContext(Assembly.GetExecutingAssembly())!.LoadFromStream(memoryStream);
            }
            using (var memoryStream = new MemoryStream(File.ReadAllBytes(Path.Combine(assetsdir, "LatihasTTS.dll")))) {
                latihasTtsAssembly = AssemblyLoadContext.GetLoadContext(Assembly.GetExecutingAssembly())!.LoadFromStream(memoryStream);
            }
            LatihasTts = Activator.CreateInstance(latihasTtsAssembly.GetType("LatihasTTS.LatihasTts")!)!;
            LatihasTts.Init(assetsdir, Path.Combine(Instance.PluginConfigDirectory, "tmp"), Log);
            if (!LatihasTts.CheckAssets()) Log.Warning("LatihasTts Assets Lost");
        }
        catch (Exception ex) {
            Log.Warning(ex, "Failed to initialize LatihasTTS engine");
        }
        try {
            edgeTTSManager = new EdgeTTSManager(Log, Instance.PluginConfigDirectory);
        }
        catch (Exception ex) {
            Log.Warning(ex, "Failed to initialize EdgeTTS engine");
        }
        ActGlobals.oFormActMain.TextToSpeech += Speak;
        SetUseEdgeTTS(Plugin.Configuration.UseEdgeTts);
        SetUseLatihasTTS(Plugin.Configuration.UseLatihasTts);
    }


    public void SetUseEdgeTTS(bool useEdgeTTS) {
        if (useEdgeTTS) Plugin.Configuration.UseLatihasTts = useLatihasTTS = false;
        Plugin.Configuration.UseEdgeTts = this.useEdgeTTS = useEdgeTTS;
        Plugin.Configuration.Save();
    }

    public void SetUseLatihasTTS(bool useLatihasTTS) {
        if (useLatihasTTS) {
            if (LatihasTts == null || !LatihasTts!.CheckAssets()) {
                Plugin.Configuration.UseLatihasTts = this.useLatihasTTS = false;
                Plugin.Configuration.Save();
                return;
            }
            Plugin.Configuration.UseEdgeTts = useEdgeTTS = false;
        }
        Plugin.Configuration.UseLatihasTts = this.useLatihasTTS = useLatihasTTS;
        Plugin.Configuration.Save();
    }

    private static readonly Dictionary<string, DateTime> time = new();

    public void Speak(string message) {
        if (string.IsNullOrEmpty(message)) return;
        lock (time) {
            if (time.TryGetValue(message, out var value) && (DateTime.Now - value).TotalSeconds < Plugin.Configuration.TtsInterval) return;
            time[message] = DateTime.Now;
        }
        if (useEdgeTTS && edgeTTSManager != null) {
            try {
                Task.Run(() => edgeTTSManager.Speak(message));
            }
            catch (Exception ex) {
                Log.Error(ex, $"EdgeTTS failed to play back {message}");
            }
            return;
        }
        if (useLatihasTTS && LatihasTts != null) {
            try {
                LatihasTts!.Speak(message);
            }
            catch (Exception ex) {
                Log.Error(ex, $"LatihasTTS failed to play back {message}");
            }
            return;
        }

        Task.Run(() => {
            if (speechSynthesizer == null && !Util.IsWine()) {
                try {
                    speechSynthesizer = new SpeechSynthesizer();
                    speechSynthesizer?.SetOutputToDefaultAudioDevice();
                }
                catch (Exception ex) {
                    Log.Warning(ex, "Failed to initialize SAPI TTS engine");
                    speechSynthesizer = null;
                }
            }
            try {
                if (speechSynthesizer == null)
                    SpeakGoogle(message);
                else
                    SpeakSapi(message);
            }
            catch (Exception ex) {
                Log.Error(ex, $"TTS failed to play back {message}");
            }
        });
    }

    public EdgeTTSManager? GetEdgeTTSManager() => edgeTTSManager;

    private void SpeakGoogle(string message) {
        var query = WebUtility.UrlEncode(message);
        const string lang = "en";
        var url = $"https://translate.google.com/translate_tts?ie=UTF-8&client=tw-ob&tl={lang}&q={query}";
        var mp3Data = client.GetByteArrayAsync(url).Result;

        using var stream = new MemoryStream(mp3Data);
        using var reader = new Mp3FileReader(stream);
        using var waveOut = new WaveOutEvent();
        waveOut.Init(reader);
        var waitHandle = new ManualResetEventSlim(false);

        lock (speechLock) {
            waveOut.Play();
            waveOut.PlaybackStopped += (_, _) => waitHandle.Set();
            waitHandle.Wait();
        }
    }

    private void SpeakSapi(string message) {
        lock (speechLock)
            speechSynthesizer?.Speak(message);
    }

    public void Dispose() => LatihasTts?.Dispose();
}