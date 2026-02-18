using System.Collections.Generic;
using SilverDasher.ACT.Doppelgangers;
using SilverDasher.ACT.Models;

namespace SilverDasher.ACT.Storages;

internal class FateStorage : BaseStorage<Fate>
{
	internal Dictionary<int, Fate> FateByID = new();

	internal override string ResourceFileName => "fates.json";

	internal FateStorage(Keeper kp)
		: base(kp)
	{
	}

	internal override Fate Get(int id)
	{
		return FateByID[id];
	}

	internal override IEnumerable<int> Keys()
	{
		return FateByID.Keys;
	}

	internal override bool Contains(int id)
	{
		return FateByID.ContainsKey(id);
	}

	internal List<string> Query(int patch = 0, int map = 0)
	{
		List<string> list = [];
		foreach (int key in FateByID.Keys)
		{
			Fate fate = FateByID[key];
			bool flag = true;
			if (patch != 0)
			{
				flag = fate.Patch == patch;
			}
			if (map != 0)
			{
				flag = fate.TerritoryID == map;
			}
			if (flag)
			{
				list.Add(key.ToString());
			}
		}
		return list;
	}

	internal bool IsSpecial(int id)
	{
		if (TryGet(id, out var _))
		{
			return Keeper.SpFates.Spfates.Contains(id);
		}
		return false;
	}

	internal HuntState GetState(int progress)
	{
		if (progress == 0)
		{
			return HuntState.Healthy;
		}
		if (progress < 20)
		{
			return HuntState.Taunted;
		}
		if (progress < 100)
		{
			return HuntState.Dying;
		}
		return HuntState.Died;
	}

	internal override void Load()
	{
		LoadData<Dictionary<int, Fate>>(ResourceFileName, out FateByID);
		foreach (int key in FateByID.Keys)
		{
			Fate fate = FateByID[key];
			fate.Id = key;
			fate.Storage = this;
		}
	}
}
