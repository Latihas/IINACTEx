using System.Collections.Generic;
using SilverDasher.ACT.Doppelgangers;
using SilverDasher.ACT.Enums;
using SilverDasher.ACT.Models;

namespace SilverDasher.ACT.Storages;

internal class MobStorage : BaseStorage<HuntMob>
{
	internal Dictionary<int, HuntMob> HuntByBnpcNameID = new Dictionary<int, HuntMob>();

	internal Dictionary<(int, Rank), List<int>> HuntsByPatchRank = new Dictionary<(int, Rank), List<int>>();

	internal override string ResourceFileName => "hunts.json";

	internal MobStorage(Keeper kp)
		: base(kp)
	{
	}

	internal override HuntMob Get(int id)
	{
		return HuntByBnpcNameID[id];
	}

	internal override IEnumerable<int> Keys()
	{
		return HuntByBnpcNameID.Keys;
	}

	internal List<int> GetHuntsByPatchRank(int patch, Rank rank)
	{
		HuntsByPatchRank.TryGetValue((patch, rank), out var value);
		return value;
	}

	internal override bool Contains(int id)
	{
		return HuntByBnpcNameID.ContainsKey(id);
	}

	internal List<string> Query(int patch = 0, Rank rank = Rank.Unknown, int map = 0)
	{
		List<string> list = new List<string>();
		foreach (int key in HuntByBnpcNameID.Keys)
		{
			HuntMob huntMob = HuntByBnpcNameID[key];
			bool flag = true;
			if (patch != 0)
			{
				flag = huntMob.Patch == patch;
			}
			if (rank != Rank.Unknown)
			{
				flag = huntMob.Rank == rank;
			}
			if (map != 0)
			{
				flag = huntMob.TerritoryID == map;
			}
			if (flag)
			{
				list.Add(key.ToString());
			}
		}
		return list;
	}

	internal HuntState GetState(int health)
	{
		if (health == 100)
		{
			return HuntState.Healthy;
		}
		if (health > 95)
		{
			return HuntState.Taunted;
		}
		if (health > 0)
		{
			return HuntState.Dying;
		}
		return HuntState.Died;
	}

	internal string GetStateName(HuntState state)
	{
		return state switch
		{
			HuntState.Healthy => "健康", 
			HuntState.Taunted => "已开怪", 
			HuntState.Dying => "被暴打中", 
			HuntState.Died => "挂了", 
			HuntState.Unknown => "", 
			_ => "不见了", 
		};
	}

	internal override void Load()
	{
		LoadData<Dictionary<int, HuntMob>>(ResourceFileName, out HuntByBnpcNameID);
		foreach (int key in HuntByBnpcNameID.Keys)
		{
			HuntMob huntMob = HuntByBnpcNameID[key];
			huntMob.Id = key;
			huntMob.Storage = this;
			if (!HuntsByPatchRank.TryGetValue((huntMob.Patch, huntMob.Rank), out var value))
			{
				value = new List<int>();
				HuntsByPatchRank[(huntMob.Patch, huntMob.Rank)] = value;
			}
			value.Add(key);
		}
	}
}
