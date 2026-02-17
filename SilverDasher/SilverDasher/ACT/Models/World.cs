using System;
using Newtonsoft.Json;
using SilverDasher.ACT.Enums;

namespace SilverDasher.ACT.Models;

public class World
{
	[JsonIgnore]
	public int Id;

	[JsonProperty("name")]
	public string Name;

	[JsonProperty("name_label")]
	public string Label;

	[JsonProperty("dc")]
	public string DataCenter;

	[JsonProperty("dc_label")]
	public string DataCenterLabel;

	[JsonIgnore]
	public Region Region
	{
		get
		{
			switch (DataCenterLabel)
			{
			case "Mana":
			case "Elemental":
			case "Gaia":
			case "Light":
			case "Crystal":
			case "Chaos":
			case "Primal":
			case "Aether":
			case "Materia":
				return Region.Global;
			case "LuXingNiao":
			case "MaoXiaoPang":
			case "MoGuLi":
			case "DouDouChai":
				return Region.China;
			default:
				throw new NotImplementedException("Unable to find Region for DataCenter " + DataCenter + ", " + DataCenterLabel + ". ");
			}
		}
	}

	public override string ToString()
	{
		return Name;
	}
}
