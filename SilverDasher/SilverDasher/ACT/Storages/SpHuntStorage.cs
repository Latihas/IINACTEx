using System.Collections.Generic;
using System.Linq;
using SilverDasher.ACT.Doppelgangers;
using SilverDasher.ACT.Models;

namespace SilverDasher.ACT.Storages;

internal class SpHuntStorage : BaseStorage
{
	internal HashSet<int> SpHunts = new HashSet<int>();

	internal List<ItemGroup> HuntGroupsTree = new List<ItemGroup>();

	internal HashSet<ItemGroup> HuntGroupsAll = new HashSet<ItemGroup>();

	internal Dictionary<string, ItemGroup> HuntGroupById = new Dictionary<string, ItemGroup>();

	internal override string ResourceFileName => "sphunts.json";

	internal SpHuntStorage(Keeper kp)
		: base(kp)
	{
	}

	internal override void Load()
	{
		SpHunts.Clear();
		HuntGroupsAll.Clear();
		LoadData<List<ItemGroup>>(ResourceFileName, out HuntGroupsTree);
		HuntGroupsAll.UnionWith(HuntGroupsTree);
		foreach (ItemGroup item in HuntGroupsTree)
		{
			HuntGroupsAll.UnionWith(item.TraverseSubGroups());
			SpHunts.UnionWith(item.TraverseGetItems());
		}
		foreach (int spHunt in SpHunts)
		{
			Keeper.Mobs.Get(spHunt);
		}
		HuntGroupById = HuntGroupsAll.ToDictionary((ItemGroup group) => group.Group);
	}
}
