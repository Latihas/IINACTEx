using System;
using Newtonsoft.Json;
using SilverDasher.ACT.Enums;

namespace SilverDasher.ACT.Models;

public class HuntMob : GameDynamicObject
{
	[JsonProperty("rank")]
	public string RankRaw;

	[JsonIgnore]
	public Rank Rank => (Rank)Enum.Parse(typeof(Rank), RankRaw);

	[JsonIgnore]
	public int Health
	{
		get
		{
			return Progress;
		}
		set
		{
			Progress = value;
		}
	}

	[JsonIgnore]
	internal HuntState State
	{
		get
		{
			if (Health == 100)
			{
				return HuntState.Healthy;
			}
			if (Health > 95)
			{
				return HuntState.Taunted;
			}
			if (Health > 0)
			{
				return HuntState.Dying;
			}
			return HuntState.Died;
		}
	}

	internal HuntMob()
	{
		TypeName = "hunt";
	}

	public HuntMob Clone()
	{
		return new HuntMob
		{
			Name = Name,
			Level = Level,
			Patch = Patch,
			RankRaw = RankRaw,
			TerritoryID = TerritoryID
		};
	}
}
