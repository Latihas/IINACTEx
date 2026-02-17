using System.Collections.Generic;
using System.Linq;
using SilverDasher.ACT.Doppelgangers;
using SilverDasher.ACT.Models;

namespace SilverDasher.ACT.Storages;

internal class SpFateStorage : BaseStorage
{
	internal HashSet<int> Spfates = new HashSet<int>();

	internal List<ItemGroup> FateGroupsTree = new List<ItemGroup>();

	internal HashSet<ItemGroup> FateGroupsAll = new HashSet<ItemGroup>();

	internal Dictionary<string, ItemGroup> FateGroupById = new Dictionary<string, ItemGroup>();

	internal override string ResourceFileName => "spfates.json";

	internal SpFateStorage(Keeper kp)
		: base(kp)
	{
	}

	internal override void Load()
	{
		Spfates.Clear();
		FateGroupsAll.Clear();
		LoadData<List<ItemGroup>>(ResourceFileName, out FateGroupsTree);
		FateGroupsAll.UnionWith(FateGroupsTree);
		foreach (ItemGroup item2 in FateGroupsTree)
		{
			FateGroupsAll.UnionWith(item2.TraverseSubGroups());
			Spfates.UnionWith(item2.TraverseGetItems());
		}
		foreach (int spfate in Spfates)
		{
			if (Keeper.Fates.TryGet(spfate, out var item))
			{
				item.Special = true;
			}
		}
		FateGroupById = FateGroupsAll.ToDictionary((ItemGroup group) => group.Group);
	}
}
