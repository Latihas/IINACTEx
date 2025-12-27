using System.Speech.Synthesis;
using Dalamud.Plugin.Services;
using IINACT.TextToSpeech;
using System.Net;
using NAudio.Wave;
using System.IO;
using System.Net.Http;
using static IINACT.Plugin;

namespace IINACT;

internal class TextToSpeechProvider
{
    private readonly object speechLock = new();
    private readonly HttpClient client = new();
    private SpeechSynthesizer? speechSynthesizer;
    private readonly EdgeTTSManager? edgeTTSManager;
    private bool useEdgeTTS;
    private bool useLatihasTTS;
    private readonly IPluginLog _log;

    public TextToSpeechProvider(IPluginLog log, string configPath)
    {
        _log = log;
        try
        {
            edgeTTSManager = new EdgeTTSManager(log, configPath);
        }
        catch (Exception ex)
        {
            _log.Warning(ex, "Failed to initialize EdgeTTS engine");
        }
        Advanced_Combat_Tracker.ActGlobals.oFormActMain.TextToSpeech += Speak;
    }

    public void SetUseEdgeTTS(bool useEdgeTTS)
    {
        if (useEdgeTTS) Plugin.Configuration.UseLatihasTts = useLatihasTTS = false;
        Plugin.Configuration.UseEdgeTTS = this.useEdgeTTS = useEdgeTTS;
        Plugin.Configuration.Save();
    }

    public void SetUseLatihasTTS(bool useLatihasTTS)
    {
        if (useLatihasTTS)
        {
            if (LatihasTts == null || !LatihasTts!.CheckAssets())
            {
                Plugin.Configuration.UseLatihasTts = this.useLatihasTTS = false;
                Plugin.Configuration.Save();
                return;
            }
            Plugin.Configuration.UseEdgeTTS = useEdgeTTS = false;
        }
        Plugin.Configuration.UseLatihasTts = this.useLatihasTTS = useLatihasTTS;
        Plugin.Configuration.Save();
    }

    public void Speak(string message)
    {
        if (string.IsNullOrEmpty(message)) return;

        if (useEdgeTTS && edgeTTSManager != null)
        {
            try
            {
                Task.Run(() => edgeTTSManager.Speak(message));
            }
            catch (Exception ex)
            {
                _log.Error(ex, $"EdgeTTS failed to play back {message}");
            }
            return;
        }
        if (useLatihasTTS)
        {
            try
            {
                LatihasTts.Speak(message);
            }
            catch (Exception ex)
            {
                _log.Error(ex, $"EdgeTTS failed to play back {message}");
            }
            return;
        }

        Task.Run(() =>
        {
            if (speechSynthesizer == null && !Dalamud.Utility.Util.IsWine())
            {
                try
                {
                    speechSynthesizer = new SpeechSynthesizer();
                    speechSynthesizer?.SetOutputToDefaultAudioDevice();
                }
                catch (Exception ex)
                {
                    _log.Warning(ex, "Failed to initialize SAPI TTS engine");
                    speechSynthesizer = null;
                }
            }
            try
            {
                if (speechSynthesizer == null)
                    SpeakGoogle(message);
                else
                    SpeakSapi(message);
            }
            catch (Exception ex)
            {
                _log.Error(ex, $"TTS failed to play back {message}");
            }
        });
    }

    public EdgeTTSManager? GetEdgeTTSManager() => edgeTTSManager;

    private void SpeakGoogle(string message)
    {
        var query = WebUtility.UrlEncode(message);
        const string lang = "en";
        var url = $"https://translate.google.com/translate_tts?ie=UTF-8&client=tw-ob&tl={lang}&q={query}";
        var mp3Data = client.GetByteArrayAsync(url).Result;

        using var stream = new MemoryStream(mp3Data);
        using var reader = new Mp3FileReader(stream);
        using var waveOut = new WaveOutEvent();
        waveOut.Init(reader);
        var waitHandle = new ManualResetEventSlim(false);

        lock (speechLock)
        {
            waveOut.Play();
            waveOut.PlaybackStopped += (s, e) => waitHandle.Set();
            waitHandle.Wait();
        }
    }

    private void SpeakSapi(string message)
    {
        lock (speechLock)
            speechSynthesizer?.Speak(message);
    }
}
