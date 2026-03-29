using System.Collections.Generic;
using SilverDasher.ACT.Doppelgangers;
using SilverDasher.ACT.Models;

namespace SilverDasher.ACT.Storages;

internal class PatchStorage(Keeper kp) : BaseStorage<Patch>(kp) {
	private List<Patch> _patches;

	public readonly Dictionary<int, Patch> PatchByCode = new();

	internal override string ResourceFileName => "patches.json";

	// internal IEnumerable<int> Keys() => PatchByCode.Keys;

	internal override Patch Get(int k) => PatchByCode[k];

	internal override bool Contains(int id) => PatchByCode.ContainsKey(id);

	internal override void Load() {
		LoadData(ResourceFileName, out _patches);
		foreach (var patch in _patches) PatchByCode[patch.Code] = patch;
	}
}