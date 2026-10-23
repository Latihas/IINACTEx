using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Net;
using System.Reflection;
using System.Runtime.Loader;
using System.Speech.Synthesis;
using System.Threading.Channels;
using System.Threading.Tasks;
using Advanced_Combat_Tracker;
using Dalamud.Utility;
using IINACT.TextToSpeech;
using NAudio.Wave;
using static IINACT.Plugin;

namespace IINACT;

internal class TextToSpeechProvider : IDisposable {
	private readonly Channel<string> speechChannel =
		Channel.CreateUnbounded<string>(new UnboundedChannelOptions {
			SingleReader = true,
			SingleWriter = false
		});
	private SpeechSynthesizer? speechSynthesizer;
	private WaveOut? googleWaveOut;
	private int googleWaveOutDevice = -1;
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
		} catch (Exception ex) {
			Log.Info(ex, "Failed to initialize LatihasTTS engine");
		}
		try {
			edgeTTSManager = new EdgeTTSManager(Log, Instance.PluginConfigDirectory);
		} catch (Exception ex) {
			Log.Warning(ex, "Failed to initialize EdgeTTS engine");
		}
		ActGlobals.oFormActMain.TextToSpeech += Speak;
		SetUseEdgeTTS(Instance.Configuration.UseEdgeTts);
		SetUseLatihasTTS(Instance.Configuration.UseLatihasTts);
		_ = ConsumeSpeechAsync();
	}


	public void SetUseEdgeTTS(bool useEdgeTTS_) {
		if (useEdgeTTS_) Instance.Configuration.UseLatihasTts = useLatihasTTS = false;
		Instance.Configuration.UseEdgeTts = useEdgeTTS = useEdgeTTS_;
		Instance.Configuration.Save();
	}

	public void SetUseLatihasTTS(bool useLatihasTTS_) {
		if (useLatihasTTS_) {
			if (LatihasTts == null || !LatihasTts!.CheckAssets()) {
				Instance.Configuration.UseLatihasTts = useLatihasTTS = false;
				Instance.Configuration.Save();
				return;
			}
			Instance.Configuration.UseEdgeTts = useEdgeTTS = false;
		}
		Instance.Configuration.UseLatihasTts = useLatihasTTS = useLatihasTTS_;
		Instance.Configuration.Save();
	}

	private static readonly Dictionary<string, DateTime> time = new();

	[SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
	public void Speak(string message) {
		if (string.IsNullOrEmpty(message)) return;
		lock (time) {
			if (time.TryGetValue(message, out var value) && (DateTime.Now - value).TotalSeconds < Instance.Configuration.TtsInterval) return;
			time[message] = DateTime.Now;
		}
		_ = speechChannel.Writer.TryWrite(message);
	}

	private async Task ConsumeSpeechAsync() {
		try {
			if (speechSynthesizer == null && !Util.IsWine()) {
				try {
					speechSynthesizer = new SpeechSynthesizer();
					speechSynthesizer?.SetOutputToDefaultAudioDevice();
				} catch (Exception ex) {
					Log.Warning(ex, "Failed to initialize SAPI TTS engine");
					speechSynthesizer = null;
				}
			}

			await foreach (var message in speechChannel.Reader.ReadAllAsync().ConfigureAwait(false)) {
				try {
					if (useEdgeTTS && edgeTTSManager != null) {
						await edgeTTSManager.Speak(message).ConfigureAwait(false);
					} else if (useLatihasTTS && LatihasTts != null) {
						await Task.Run(() => { LatihasTts!.Speak(message); }).ConfigureAwait(false);
					} else if (speechSynthesizer == null || Instance.Configuration.ForceGoogleTts) {
						await SpeakGoogle(message).ConfigureAwait(false);
					} else {
						await Task.Run(() => SpeakSapi(message)).ConfigureAwait(false);
					}
				} catch (Exception ex) {
					Log.Error(ex, $"TTS failed to play back {message}");
				}
			}
		} catch (Exception ex) {
			Log.Error(ex, "TTS playback queue failed");
		}
	}

	public EdgeTTSManager? GetEdgeTTSManager() => edgeTTSManager;

	private async Task SpeakGoogle(string message) {
		var query = WebUtility.UrlEncode(message);
		var lang = Instance.Configuration.GoogleTtsLanguage;
		if (string.IsNullOrWhiteSpace(lang)) lang = "zh_cn";
		var url = $"https://translate.google.com/translate_tts?ie=UTF-8&client=tw-ob&tl={lang}&q={query}";
		var mp3Data = await Instance.HttpClient.GetByteArrayAsync(url).ConfigureAwait(false);

		using var stream = new MemoryStream(mp3Data);
		await using var reader = new Mp3FileReader(stream);
		var device = Instance.Configuration.TtsPlaybackDevice;
		if (googleWaveOut == null || googleWaveOutDevice != device) {
			googleWaveOut?.Dispose();
			googleWaveOut = new WaveOut();
			googleWaveOutDevice = device;
		}
		googleWaveOut.DeviceNumber = device;
		googleWaveOut.Init(reader);
		var stopped = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		googleWaveOut.PlaybackStopped += OnPlaybackStopped;
		try {
			googleWaveOut.Play();
			await stopped.Task.ConfigureAwait(false);
		} finally {
			googleWaveOut.PlaybackStopped -= OnPlaybackStopped;
		}

		void OnPlaybackStopped(object? sender, StoppedEventArgs e) {
			stopped.TrySetResult();
		}
	}

	private void SpeakSapi(string message) {
		speechSynthesizer?.Speak(message);
	}

	public void Dispose() {
		ActGlobals.oFormActMain.TextToSpeech -= Speak;
		speechChannel.Writer.TryComplete();
		googleWaveOut?.Dispose();
		LatihasTts?.Dispose();
	}
}