using Newtonsoft.Json;

namespace SilverDasher.ACT.Models;

public class Territory {
	[JsonProperty("name")] public string Name;

	[JsonProperty("region")] public string Region;

	[JsonProperty("content")] public int Content;

	[JsonProperty("dcmap")] public bool IsDataCenterMap;

	public override string ToString() => Name;
}