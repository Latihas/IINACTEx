using System.IO;
using EdgeTTS.Models;
using Newtonsoft.Json;

namespace IINACT.TextToSpeech;

public class EdgeTTSConfig {
	[JsonProperty("deviceId")] public int DeviceId { get; set; }

	[JsonProperty("speed")] public int Speed { get; set; } = 100;

	[JsonProperty("pitch")] public int Pitch { get; set; } = 100;

	[JsonProperty("volume")] public int Volume { get; set; } = 100;

	[JsonProperty("voice")] public string Voice { get; set; } = "zh-CN-XiaoxiaoNeural";

	[JsonProperty("customCachePath")] public string? CustomCachePath { get; set; }

	[JsonProperty("textReplacements")] public Dictionary<string, string> TextReplacements { get; set; } = new() {
		["欧米茄"] = "欧米加",
		["歐米茄"] = "歐米加"
	};

	[JsonProperty("testText")] public string TestText { get; set; } = "异国的诗人提出疑问。如果欧米茄作为兵器不断地获得力量并且一直战斗下去的话，它真的能够找到渴求的答案吗？";

	public static EdgeTTSConfig Load(string configPath) {
		if (!File.Exists(configPath))
			return new EdgeTTSConfig();

		try {
			var json = File.ReadAllText(configPath);
			return JsonConvert.DeserializeObject<EdgeTTSConfig>(json) ?? new EdgeTTSConfig();
		} catch {
			return new EdgeTTSConfig();
		}
	}

	public void Save(string configPath) {
		var directory = Path.GetDirectoryName(configPath);
		if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
			Directory.CreateDirectory(directory);

		var json = JsonConvert.SerializeObject(this, Formatting.Indented);
		File.WriteAllText(configPath, json);
	}

	public EdgeTTSSettings ToEdgeTTSSettings() =>
		new() {
			DeviceID = DeviceId,
			Speed = Speed,
			Pitch = Pitch,
			Volume = Volume,
			Voice = Voice,
			PhonemeReplacements = TextReplacements
		};
}