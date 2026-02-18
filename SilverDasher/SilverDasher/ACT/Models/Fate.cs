using System.Collections.Generic;
using Newtonsoft.Json;

namespace SilverDasher.ACT.Models;

public class Fate : GameDynamicObject
{
	[JsonIgnore]
	public bool Special;

	[JsonIgnore]
	public int StartTime;

	[JsonIgnore]
	public int Duration;

	[JsonIgnore]
	public List<FateGroup> Groups = [];

	[JsonIgnore]
	internal HuntState State
	{
		get
		{
			if (Progress == 0)
			{
				return HuntState.Healthy;
			}
			if (Progress < 20)
			{
				return HuntState.Taunted;
			}
			if (Progress < 100)
			{
				return HuntState.Dying;
			}
			return HuntState.Died;
		}
	}

	internal Fate()
	{
		TypeName = "fate";
	}

	public Fate Clone()
	{
		return new Fate
		{
			Name = Name,
			Level = Level,
			Patch = Patch,
			TerritoryID = TerritoryID,
			Special = Special,
			Groups = Groups
		};
	}
}
