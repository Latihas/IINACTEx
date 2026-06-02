using Newtonsoft.Json;

namespace SilverDasher.ACT.Models;

public class LocalizedEntry {
	[JsonProperty("chs")] public string Chinese;
	[JsonProperty("en")] public string English;
	[JsonProperty("ja")] public string Japanese;
	[JsonProperty("de")] public string German;
	[JsonProperty("fr")] public string French;

	public override string ToString() => Chinese;
}