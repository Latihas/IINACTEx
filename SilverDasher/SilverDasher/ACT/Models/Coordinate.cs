using Newtonsoft.Json;

namespace SilverDasher.ACT.Models;

internal class Coordinate
{
	[JsonProperty("x")]
	internal int x;

	[JsonProperty("y")]
	internal int y;

	[JsonIgnore]
	internal string displayX => ((float)x / 100f).ToString("0.##");

	[JsonIgnore]
	internal string displayY => ((float)y / 100f).ToString("0.##");
}
