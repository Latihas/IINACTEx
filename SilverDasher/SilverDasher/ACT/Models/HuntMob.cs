using System;
using Newtonsoft.Json;
using SilverDasher.ACT.Enums;

namespace SilverDasher.ACT.Models;

public class HuntMob : GameDynamicObject {
	[JsonProperty("rank")] public string RankRaw;

	[JsonIgnore] public Rank Rank => (Rank)Enum.Parse(typeof(Rank), RankRaw);

	[JsonIgnore] public int Health {
		get => Progress;
		set => Progress = value;
	}

	[JsonIgnore] internal HuntState State {
		get {
			return Health switch {
				100 => HuntState.Healthy,
				> 95 => HuntState.Taunted,
				> 0 => HuntState.Dying,
				_ => HuntState.Died
			};
		}
	}

	internal HuntMob() {
		TypeName = "hunt";
	}

	public HuntMob Clone() =>
		new() {
			Name = Name,
			Level = Level,
			Patch = Patch,
			RankRaw = RankRaw,
			TerritoryID = TerritoryID
		};
}