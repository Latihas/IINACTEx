using Newtonsoft.Json;

namespace SilverDasher.ACT.Models;

internal class Coordinate {
	[JsonProperty("x")] internal int x;

	[JsonProperty("y")] internal int y;

	[JsonIgnore] internal string displayX => (x / 100f).ToString("0.##");

	[JsonIgnore] internal string displayY => (y / 100f).ToString("0.##");
}