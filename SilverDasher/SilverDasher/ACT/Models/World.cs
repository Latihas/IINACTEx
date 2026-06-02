using Newtonsoft.Json;

namespace SilverDasher.ACT.Models;

public class World {
	[JsonIgnore] public int Id;
	[JsonProperty("name")] public string Name;
	[JsonProperty("name_label")] public string Label;
	[JsonProperty("dc")] public string DataCenter;
	[JsonProperty("dc_label")] public string DataCenterLabel;
	public override string ToString() => Name;
}