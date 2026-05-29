using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Dalamud.Plugin.Services;
using Microsoft.ML.OnnxRuntime;
using NAudio.Wave;
using static LatihasTTS.LatihasTts.TtsEngine;

namespace LatihasTTS;

[SuppressMessage("ReSharper", "UnusedType.Global")]
[SuppressMessage("Usage", "CA2211:非常量字段应当不可见")]
public partial class LatihasTts : IDisposable {
	private static string Tmpdir;
	private static string Assetsdir;
	private static string[] AssetsList;
	private static IPluginLog Log;
	private static IFramework Framework;
	private readonly List<IntPtr> onnxruntimedll = [];

	[DllImport("kernel32.dll", SetLastError = true)]
	private static extern bool FreeLibrary(IntPtr hModule);

	[DllImport("kernel32.dll", SetLastError = true)]
	private static extern IntPtr LoadLibrary(string lpFileName);

	public void Init(string assetsDir, string tmpDir, IPluginLog log, IFramework framework) {
		Log = log;
		Assetsdir = assetsDir;
		Tmpdir = tmpDir;
		Framework = framework;
		AssetsList = [
			Path.Combine(Assetsdir, "vocab.txt"),
			Path.Combine(Assetsdir, "pinyin.txt"),
			Path.Combine(Assetsdir, "symbol.txt"),
			Path.Combine(Assetsdir, "a.ort"),
			Path.Combine(Assetsdir, "v.ort"),
			Path.Combine(Assetsdir, "onnxruntime.dll"),
			Path.Combine(Assetsdir, "onnxruntime.lib"),
			Path.Combine(Assetsdir, "onnxruntime_providers_shared.dll"),
			Path.Combine(Assetsdir, "onnxruntime_providers_shared.lib"),
			Path.Combine(Assetsdir, "Microsoft.ML.OnnxRuntime.dll"),
			Path.Combine(Assetsdir, "System.Numerics.Tensors.dll")
		];
		onnxruntimedll.Add(LoadLibrary(Path.Combine(Assetsdir, "onnxruntime.dll")));
		// onnxruntimedll.Add(LoadLibrary(Path.Combine(Assetsdir, "onnxruntime_providers_shared.dll")));
	}

	private bool _playerInited;

	private void _Speak(object message) {
		try {
			if (!_playerInited) {
				Framework.Update += Player.ProcessSpeakQueue;
				_playerInited = true;
			}
			var smg = message.ToString();
			if (string.IsNullOrEmpty(smg)) return;
			smg = smg.Replace("AA", ",A,A")
				.Replace("aa", ",a,a")
				.Replace("AOE", "AAOOE")
				.Replace("aoe", "aaooe");
			foreach (var line in StrRegex().Split(smg)) {
				Player.Play(GetWav(line), line is "A" or "a");
			}
			Log.Info("TTS: " + message);
		} catch (Exception e) {
			Log.Error("TTS: " + message + e);
		}
	}

	[SuppressMessage("ReSharper", "UnusedMember.Global")]
	public void Speak(string message) {
		if (!CheckAssets()) return;
		_Speak(message);
	}

	[SuppressMessage("ReSharper", "MemberCanBeMadeStatic.Global")]
	[SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
	[SuppressMessage("Performance", "CA1822")]
	public bool CheckAssets() => AssetsList.All(File.Exists);

	public static class TtsEngine {
		internal static MemoryStream GetWav(string text) {
			if (string.IsNullOrEmpty(text)) return new MemoryStream();
			var tmpfp = Path.Combine(Tmpdir, text + ".wav");
			if (File.Exists(tmpfp)) {
				Log.Info("Cached: " + text);
				using var fileStream = new FileStream(tmpfp, FileMode.Open, FileAccess.Read);
				var mStream = new MemoryStream();
				fileStream.CopyTo(mStream);
				mStream.Position = 0;
				return mStream;
			}
			var a = GenTts.Forward(PaddleTextTokenizer.Encode(text));
			var ad = new byte[2 * a.Length];
			var iter = 0;
			foreach (var da in a) ad[iter++] = ad[iter++] = (byte)(da * 128 * 1.25);
			Task.Run(() => {
				try {
					if (!Directory.Exists(Tmpdir)) Directory.CreateDirectory(Tmpdir);
					using var fostream = new MemoryStream(ad);
					using var outputStream = new FileStream(tmpfp, FileMode.Create, FileAccess.Write);
					fostream.CopyTo(outputStream);
				} catch (Exception e) {
					Log.Error(e.ToString());
				}
			});
			return new MemoryStream(ad);
		}

		public static class Player {
			private static readonly Queue<WavInfo> streams = new();

			internal static void ProcessSpeakQueue(IFramework _) {
				try {
					if (streams.Count > 0) {
						var mStream = streams.Dequeue();
						PlayWav(mStream.Stream);
						mStream.Stream.Close();
						if (mStream.LongDelay) Thread.Sleep(75);
					} else Thread.Sleep(50);
				} catch (Exception ex) {
					Log.Warning($"Speak Err:{ex}");
				}
			}

			internal static void Play(MemoryStream stream, bool longDelay = false) {
				streams.Enqueue(new WavInfo(stream, longDelay));
			}

			private static void PlayWav(MemoryStream stream) {
				try {
					const int sr = 30000;
					var rawStream = new RawSourceWaveStream(stream, new WaveFormat(sr, 16, 1));
					var waveOut = new WaveOut();
					waveOut.Init(rawStream);
					waveOut.Play();
					while (waveOut.PlaybackState == PlaybackState.Playing) Thread.Sleep(250);
				} catch {
					// ignored
				}
			}

			private class WavInfo(MemoryStream memoryStream, bool b) {
				internal readonly bool LongDelay = b;
				internal readonly MemoryStream Stream = memoryStream;
			}
		}

		private static class PaddleTextTokenizer {
			private static readonly Dictionary<string, string> Vocab = new();
			private static readonly Dictionary<string, string> Pinyin = new();
			private static readonly Dictionary<string, long> Symbol = new();

			static PaddleTextTokenizer() {
				using (var sr = File.OpenText(Path.Combine(Assetsdir, "vocab.txt"))) {
					while (sr.ReadLine() is { } nextLine) {
						try {
							var array = nextLine.Split(':');
							Vocab[array[0]] = array[1];
						} catch (Exception) {
							// ignored
						}
					}
				}
				using (var sr = File.OpenText(Path.Combine(Assetsdir, "pinyin.txt"))) {
					while (sr.ReadLine() is { } nextLine) {
						try {
							var array = nextLine.Split(':');
							Pinyin[array[0]] = array[1];
						} catch (Exception) {
							// ignored
						}
					}
				}
				using (var sr = File.OpenText(Path.Combine(Assetsdir, "symbol.txt"))) {
					while (sr.ReadLine() is { } nextLine) {
						try {
							var array = nextLine.Split(' ');
							Symbol[array[0]] = long.Parse(array[1]);
						} catch (Exception) {
							// ignored
						}
					}
				}
			}

			public static long[] Encode(string text) {
				var list = new List<long>();
				foreach (var t in Py(text)) {
					if (Symbol.TryGetValue(t, out var value)) list.Add(value);
				}
				return list.ToArray();
			}

			private static List<string> Py(string text) {
				var list = new List<string>();
				for (int i = text.Length, start = 0; i > start; i--) {
					var ss = text.Substring(start, i - start);
					if (!Vocab.TryGetValue(ss, out var zhTone)) continue;
					start = i;
					i = text.Length + 1;
					if (zhTone is [<= 'Z' and >= 'A']) {
						if (!Pinyin.TryGetValue(zhTone, out var what)) {
							Log.Error($"{zhTone} is not pinyin.");
							continue;
						}
						list.Add(what);
					}
					foreach (var x in zhTone.Split(' ')) {
						var key = Regex.Replace(x, "\\d+$", "");
						if (!Pinyin.TryGetValue(key, out var what)) {
							Log.Error($"{key} is not pinyin.");
							continue;
						}
						what += x.Substring(x.Length - 1);
						list.Add(what);
					}
				}
				var res = new List<string>();
				for (var i = 0; i < list.Count; i++) {
					var ci = list[i];
					if (i != list.Count - 1) {
						var cj = list[i + 1];
						var ciLast = ci[^1];
						var cjLast = cj[^1];
						if (ciLast == '3' && cjLast == '3')
							ci = ci[..^1] + '2';
						if (ci == "b u4" && cjLast == '4') ci = "b u2";
						if (ci == "^ i1") {
							ci = cjLast switch {
								'1' or '3' => "^ i4",
								'4' => "^ i2",
								_ => ci
							};
						}
					}
					res.AddRange(ci.Split(' '));
				}
				return res;
			}
		}

		private static class GenTts {
			private static readonly InferenceSession SessionFastspeech, SessionVcoder;
			private static readonly RunOptions RunOptions = new();

			static GenTts() {
				var options = new SessionOptions();
				options.AddSessionConfigEntry("session.load_model_format", "ORT");
				SessionFastspeech = new InferenceSession(Path.Combine(Assetsdir, "a.ort"), options);
				SessionVcoder = new InferenceSession(Path.Combine(Assetsdir, "v.ort"), options);
			}

			public static float[] Forward(long[] ids) {
				if (ids.Length == 0) return [];
				using var inputOrtValue = OrtValue.CreateTensorValueFromMemory(ids, [
					ids.Length
				]);
				var inputs1 = new Dictionary<string, OrtValue> {
					{
						"text", inputOrtValue
					}
				};
				using var outputs1 = SessionFastspeech.Run(RunOptions, inputs1, SessionFastspeech.OutputNames);
				var inputs2 = new Dictionary<string, OrtValue> {
					{
						"logmel", outputs1.First()
					}
				};
				using var results = SessionVcoder.Run(RunOptions, inputs2, SessionVcoder.OutputNames);
				return results.First().GetTensorMutableDataAsSpan<float>().ToArray();
			}
		}
	}

	public void Dispose() {
		Framework.Update -= Player.ProcessSpeakQueue;
		foreach (var onnx in onnxruntimedll) {
			try {
				FreeLibrary(onnx);
			} catch {
				Log.Warning($"Probably Leaked Onnx Dll: {onnx}");
			}
		}
	}

	[GeneratedRegex(@"[^\u4e00-\u9fa5\w]+")]
	private static partial Regex StrRegex();
}