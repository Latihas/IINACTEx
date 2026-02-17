using System.Collections.Generic;
using Newtonsoft.Json;

namespace SilverDasher.ACT.Models;

public class ItemGroup
{
	[JsonProperty("group")]
	public string Group;

	[JsonProperty("name")]
	public string Name;

	[JsonProperty("items")]
	public List<int> Items = new List<int>();

	[JsonProperty("subGroups")]
	public List<ItemGroup> SubGroups = new List<ItemGroup>();

	internal HashSet<ItemGroup> TraverseSubGroups()
	{
		HashSet<ItemGroup> hashSet = new HashSet<ItemGroup>();
		if (SubGroups.Count > 0)
		{
			foreach (ItemGroup subGroup in SubGroups)
			{
				hashSet.Add(subGroup);
				hashSet.UnionWith(subGroup.TraverseSubGroups());
			}
			return hashSet;
		}
		return hashSet;
	}

	internal HashSet<int> TraverseGetItems()
	{
		HashSet<int> hashSet = new HashSet<int>();
		hashSet.UnionWith(Items);
		foreach (ItemGroup item in TraverseSubGroups())
		{
			hashSet.UnionWith(item.Items);
		}
		return hashSet;
	}
}
