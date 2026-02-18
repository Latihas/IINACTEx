using System.Collections.Generic;
using SilverDasher.ACT.Doppelgangers;
using SilverDasher.ACT.Models;

namespace SilverDasher.ACT.Storages;

internal class PatchStorage : BaseStorage<Patch>
{
	private List<Patch> _patches;

	public Dictionary<int, Patch> PatchByCode = new();

	internal override string ResourceFileName => "patches.json";

	internal PatchStorage(Keeper kp)
		: base(kp)
	{
	}

	internal override IEnumerable<int> Keys()
	{
		return PatchByCode.Keys;
	}

	internal override Patch Get(int k)
	{
		return PatchByCode[k];
	}

	internal override bool Contains(int id)
	{
		return PatchByCode.ContainsKey(id);
	}

	internal override void Load()
	{
		LoadData<List<Patch>>(ResourceFileName, out _patches);
		foreach (Patch patch in _patches)
		{
			PatchByCode[patch.Code] = patch;
		}
	}
}
