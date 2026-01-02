using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading;
using Dalamud.Plugin.Services;
using Microsoft.ML.OnnxRuntime;
using NAudio.Wave;

namespace LatihasTTS;

[SuppressMessage("ReSharper", "UnusedType.Global")]
[SuppressMessage("Usage", "CA2211:非常量字段应当不可见")]
public class LatihasTts : IDisposable {
    internal static string Tmpdir;
    public static string Assetsdir;
    private static string[] AssetsList;
    private static IPluginLog Log;
    internal static readonly bool[] Alive = [
        true
    ];
    private readonly List<IntPtr> onnxruntimedll = [];

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool FreeLibrary(IntPtr hModule);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr LoadLibrary(string lpFileName);

    public void Init(string assetsDir, string tmpDir, IPluginLog log) {
        Log = log;
        Assetsdir = assetsDir;
        Tmpdir = tmpDir;
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
        onnxruntimedll.Add(LoadLibrary(Path.Combine(Assetsdir, "onnxruntime_providers_shared.dll.dll")));
    }

    private static void _Speak(object message) {
        try {
            var player = new TtsEngine.Player();
            var smg = message.ToString();
            if (string.IsNullOrEmpty(smg)) return;
            smg = smg.Replace("AA", ",A,A")
                .Replace("aa", ",a,a")
                .Replace("AOE", "AAOOE")
                .Replace("aoe", "aaooe");
            foreach (var line in Regex.Split(smg, @"[^\u4e00-\u9fa5\w]+")) {
                if (!Alive[0]) return;
                player.Play(TtsEngine.GetWav(line), line is "A" or "a");
            }
            Log.Info("TTS: " + message);
        }
        catch (Exception e) {
            Log.Error("TTS: " + message + e);
        }
    }

    public void Speak(string message) {
        if (!CheckAssets()) return;
        try {
            new Thread(_Speak).Start(message);
        }
        catch {
            // ignored
        }
    }

    public bool CheckAssets() {
        foreach (var fp in AssetsList)
            if (!File.Exists(fp))
                return false;
        return true;
    }

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
            new Thread(() => {
                try {
                    if (!Directory.Exists(Tmpdir)) Directory.CreateDirectory(Tmpdir);
                    using var fostream = new MemoryStream(ad);
                    using var outputStream = new FileStream(tmpfp, FileMode.Create, FileAccess.Write);
                    fostream.CopyTo(outputStream);
                }
                catch (Exception e) {
                    Log.Error(e.ToString());
                }
            }).Start();
            return new MemoryStream(ad);
        }

        public class Player {
            private readonly Queue<WavInfo> streams = new();

            internal Player() {
                new Thread(() => {
                    while (Alive[0]) {
                        try {
                            if (streams.Count > 0) {
                                var mStream = streams.Dequeue();
                                PlayWav(mStream.Stream);
                                mStream.Stream.Close();
                                if (mStream.LongDelay) Thread.Sleep(75);
                            }
                            else Thread.Sleep(50);
                        }
                        catch {
                            // ignored
                        }
                    }
                }).Start();
            }

            internal void Play(MemoryStream stream, bool longDelay = false) {
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
                }
                catch {
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
                        }
                        catch (Exception) {
                            // ignored
                        }
                    }
                }
                using (var sr = File.OpenText(Path.Combine(Assetsdir, "pinyin.txt"))) {
                    while (sr.ReadLine() is { } nextLine) {
                        try {
                            var array = nextLine.Split(':');
                            Pinyin[array[0]] = array[1];
                        }
                        catch (Exception) {
                            // ignored
                        }
                    }
                }
                using (var sr = File.OpenText(Path.Combine(Assetsdir, "symbol.txt"))) {
                    while (sr.ReadLine() is { } nextLine) {
                        try {
                            var array = nextLine.Split(' ');
                            Symbol[array[0]] = long.Parse(array[1]);
                        }
                        catch (Exception) {
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
        Alive[0] = false;
        foreach (var onnx in onnxruntimedll) {
            try {
                FreeLibrary(onnx);
            }
            catch {
                Log.Warning($"Probably Leaked Onnx Dll: {onnx}");
            }
        }
    }
}