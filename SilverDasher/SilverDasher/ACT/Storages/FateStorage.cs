using System.Collections.Generic;
using SilverDasher.ACT.Doppelgangers;
using SilverDasher.ACT.Models;

namespace SilverDasher.ACT.Storages;

internal class FateStorage(Keeper kp) : BaseStorage<Fate>(kp) {
	internal Dictionary<int, Fate> FateByID = [];

	internal override string ResourceFileName => "fates.json";

	internal override Fate Get(int id) => FateByID[id];

	internal override bool Contains(int id) => FateByID.ContainsKey(id);

	internal static HuntState GetState(int progress) {
		return progress switch {
			0 => HuntState.Healthy,
			< 20 => HuntState.Taunted,
			< 100 => HuntState.Dying,
			_ => HuntState.Died
		};
	}

	internal override void Load() {
		LoadData(ResourceFileName, out FateByID);
		foreach (var key in FateByID.Keys) {
			var fate = FateByID[key];
			fate.Id = key;
			fate.Storage = this;
		}
	}
}