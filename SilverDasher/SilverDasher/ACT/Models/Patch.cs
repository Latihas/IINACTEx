using Newtonsoft.Json;

namespace SilverDasher.ACT.Models;

public class Patch
{
	[JsonProperty("code")]
	public int Code;

	[JsonProperty("name")]
	public LocalizedEntry Name;
}
