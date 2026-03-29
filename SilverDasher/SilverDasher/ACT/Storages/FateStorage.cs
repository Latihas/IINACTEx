using System.Collections.Generic;
using SilverDasher.ACT.Doppelgangers;
using SilverDasher.ACT.Models;

namespace SilverDasher.ACT.Storages;

internal class FateStorage(Keeper kp) : BaseStorage<Fate>(kp) {
	internal Dictionary<int, Fate> FateByID = new();

	internal override string ResourceFileName => "fates.json";

	internal override Fate Get(int id) => FateByID[id];

	// internal IEnumerable<int> Keys() => FateByID.Keys;

	internal override bool Contains(int id) => FateByID.ContainsKey(id);

	internal List<string> Query(int patch = 0, int map = 0) {
		List<string> list = [];
		foreach (var key in FateByID.Keys) {
			var fate = FateByID[key];
			var flag = true;
			if (patch != 0) flag = fate.Patch == patch;
			if (map != 0) flag = fate.TerritoryID == map;
			if (flag) list.Add(key.ToString());
		}
		return list;
	}

	// internal bool IsSpecial(int id) {
	//     if (TryGet(id, out _)) {
	//         return Keeper.SpFates.Spfates.Contains(id);
	//     }
	//     return false;
	// }

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