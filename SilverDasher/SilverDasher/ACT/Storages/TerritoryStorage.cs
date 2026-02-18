using System.Collections.Generic;
using SilverDasher.ACT.Doppelgangers;
using SilverDasher.ACT.Models;

namespace SilverDasher.ACT.Storages;

internal class TerritoryStorage : BaseStorage<Territory>
{
	internal static TerritoryStorage Instance;

	private Dictionary<int, Territory> TerritoryById = new();

	internal override string ResourceFileName => "territories.json";

	internal TerritoryStorage(Keeper kp)
		: base(kp)
	{
		Instance = this;
	}

	internal override bool Contains(int id)
	{
		return TerritoryById.ContainsKey(id);
	}

	internal bool Contains(uint id)
	{
		return TerritoryById.ContainsKey((int)id);
	}

	internal override Territory Get(int id)
	{
		return TerritoryById[id];
	}

	internal override IEnumerable<int> Keys()
	{
		return TerritoryById.Keys;
	}

	internal override void Load()
	{
		LoadData<Dictionary<int, Territory>>(ResourceFileName, out TerritoryById);
	}
}
